using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Modules;
using Nexora.Domain.Modules;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Modules;

/// <summary>
/// SQL-backed module catalog and policy service. Preview tokens are signed,
/// short-lived capability descriptions; the commit always re-reads and locks
/// the module row, so a token can never bypass current policy or dependencies.
/// </summary>
public sealed class SqlModulePolicyService : IModulePolicyService
{
    private static readonly TimeSpan PreviewTtl = TimeSpan.FromMinutes(2);
    private readonly SqlConnectionFactory _connections;
    private readonly byte[] _previewSecret;
    private readonly SqlRequestReceiptStore _receipts;

    public SqlModulePolicyService(SqlConnectionFactory connections, string? previewSecret = null, string? idempotencySecret = null)
    {
        _connections = connections;
        _previewSecret = string.IsNullOrWhiteSpace(previewSecret)
            ? RandomNumberGenerator.GetBytes(32)
            : SHA256.HashData(Encoding.UTF8.GetBytes(previewSecret));
        _receipts = new SqlRequestReceiptStore(idempotencySecret ?? previewSecret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    public IdentityOperationResult<ModulePolicyPage> List(int? limit, IdentityPrincipal actor)
    {
        if (!IsSuperAdmin(actor))
        {
            return Denied<ModulePolicyPage>();
        }

        var take = Math.Clamp(limit ?? 25, 1, 100);
        using var connection = _connections.Create();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Limit) [Id], [Code], [Name], [State], [SystemEnabled],
                   [RegistrationEnabled], [PolicyRevision]
            FROM [platform].[Module]
            ORDER BY [Code];
            """;
        command.Parameters.Add(new SqlParameter("@Limit", System.Data.SqlDbType.Int) { Value = take });

        var modules = new List<ModulePolicyRecord>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var id = reader.GetGuid(0);
                var code = reader.GetString(1);
                var state = reader.GetString(3);
                modules.Add(new ModulePolicyRecord(
                    id,
                    code,
                    reader.GetString(2),
                    state,
                    reader.GetBoolean(4),
                    reader.GetBoolean(5),
                    reader.GetInt64(6),
                    ETag(reader.GetInt64(6)),
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    UnavailableReason(state)));
            }
        }

        // Dependency rows are read after the catalog reader is closed. This
        // keeps the query straightforward and avoids multiple active readers.
        var withDependencies = modules.Select(module => module with
        {
            RequiredDependencies = ReadDependencies(connection, module.Id, incoming: true),
            RequiredBy = ReadDependencies(connection, module.Id, incoming: false)
        }).ToArray();

        return IdentityOperationResult<ModulePolicyPage>.Success(new ModulePolicyPage(withDependencies, null));
    }

    public IdentityOperationResult<ModulePolicyPreview> Preview(Guid moduleId, ModulePolicyChange change, IdentityPrincipal actor)
    {
        if (!IsSuperAdmin(actor))
        {
            return Denied<ModulePolicyPreview>();
        }

        if (change.SystemEnabled is null && change.RegistrationEnabled is null)
        {
            return IdentityOperationResult<ModulePolicyPreview>.Failure(
                "ValidationFailed", 422, "At least one editable module policy field is required.");
        }

        using var connection = _connections.Create();
        connection.Open();
        var module = ReadModule(connection, moduleId, forUpdate: false);
        if (module is null)
        {
            return IdentityOperationResult<ModulePolicyPreview>.Failure("ResourceUnavailable", 404, "Module unavailable.");
        }

        var blockers = EvaluateBlockers(connection, module, change);
        var changes = BuildDiffs(module, change);
        var expiresAt = DateTimeOffset.UtcNow.Add(PreviewTtl);
        // Bind the short-lived capability to the SuperAdmin who requested it.
        // A valid preview must never be transferable to another principal.
        var token = SignPreview(new PreviewPayload(actor.UserId, module.Id, module.PolicyRevision, change.SystemEnabled, change.RegistrationEnabled, expiresAt));
        return IdentityOperationResult<ModulePolicyPreview>.Success(
            new ModulePolicyPreview(token, expiresAt, ETag(module.PolicyRevision), changes, blockers));
    }

    public IdentityOperationResult<ModulePolicyRecord> Commit(Guid moduleId, string? ifMatch, ModulePolicyCommit command, IdentityPrincipal actor, string? idempotencyKey = null, string? traceId = null)
    {
        if (!IsSuperAdmin(actor))
        {
            return Denied<ModulePolicyRecord>();
        }

        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return IdentityOperationResult<ModulePolicyRecord>.Failure("PreconditionRequired", 428, "If-Match is required.");
        }

        if (!TryReadPreview(command.PreviewToken, actor.UserId, out var preview) || preview.ModuleId != moduleId || preview.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return IdentityOperationResult<ModulePolicyRecord>.Failure("PreviewStale", 409, "Module policy preview is stale.");
        }

        using var connection = _connections.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
        var module = ReadModule(connection, transaction, moduleId, forUpdate: true);
        if (module is null)
        {
            transaction.Rollback();
            return IdentityOperationResult<ModulePolicyRecord>.Failure("ResourceUnavailable", 404, "Module unavailable.");
        }

        var receipt = _receipts.TryClaim(connection, transaction, actor.UserId,
            "modules.policy.update", idempotencyKey ?? string.Empty,
            $"module:{moduleId:N}|etag:{ifMatch}|change:{command.Change.SystemEnabled}|registration:{command.Change.RegistrationEnabled}|preview:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.PreviewToken ?? string.Empty)))}",
            DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && !receipt.IsClaimed)
        {
            transaction.Rollback();
            var code = receipt.IsConflict ? "IdempotencyConflict" : receipt.IsReplay ? "IdempotencyReplay" : "RequestInProgress";
            return IdentityOperationResult<ModulePolicyRecord>.Failure(code, 409,
                code == "IdempotencyConflict" ? "The same Idempotency-Key was already used with a different request." : "The request was already completed or is in progress.");
        }

        if (!string.Equals(ifMatch, ETag(module.PolicyRevision), StringComparison.Ordinal) ||
            preview.PolicyRevision != module.PolicyRevision ||
            preview.SystemEnabled != command.Change.SystemEnabled ||
            preview.RegistrationEnabled != command.Change.RegistrationEnabled)
        {
            transaction.Rollback();
            return IdentityOperationResult<ModulePolicyRecord>.Failure("RevisionConflict", 412, "Module policy revision changed.");
        }

        var blockers = EvaluateBlockers(connection, transaction, module, command.Change);
        if (blockers.Count > 0)
        {
            transaction.Rollback();
            var blocker = blockers[0];
            return IdentityOperationResult<ModulePolicyRecord>.Failure(blocker.Code, 409, blocker.Message);
        }

        var newSystemEnabled = command.Change.SystemEnabled ?? module.SystemEnabled;
        var newRegistrationEnabled = command.Change.RegistrationEnabled ?? module.RegistrationEnabled;
        using var update = connection.CreateCommand();
        update.Transaction = transaction;
        update.CommandText = """
            UPDATE [platform].[Module]
            SET [SystemEnabled] = @SystemEnabled,
                [RegistrationEnabled] = @RegistrationEnabled,
                [PolicyRevision] = [PolicyRevision] + 1,
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Id] = @Id AND [PolicyRevision] = @PolicyRevision;
            """;
        update.Parameters.Add(new SqlParameter("@SystemEnabled", System.Data.SqlDbType.Bit) { Value = newSystemEnabled });
        update.Parameters.Add(new SqlParameter("@RegistrationEnabled", System.Data.SqlDbType.Bit) { Value = newRegistrationEnabled });
        update.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = module.Id });
        update.Parameters.Add(new SqlParameter("@PolicyRevision", System.Data.SqlDbType.BigInt) { Value = module.PolicyRevision });
        if (update.ExecuteNonQuery() != 1)
        {
            transaction.Rollback();
            return IdentityOperationResult<ModulePolicyRecord>.Failure("RevisionConflict", 412, "Module policy revision changed.");
        }

        WriteAudit(connection, transaction, actor, module.Id, "modules.policy.update", "Succeeded", traceId);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _receipts.Complete(connection, transaction, receipt, "ModulePolicyUpdated");
        }
        transaction.Commit();

        return IdentityOperationResult<ModulePolicyRecord>.Success(module with
        {
            SystemEnabled = newSystemEnabled,
            RegistrationEnabled = newRegistrationEnabled,
            PolicyRevision = module.PolicyRevision + 1,
            ETag = ETag(module.PolicyRevision + 1)
        });
    }

    private static bool IsSuperAdmin(IdentityPrincipal actor) =>
        string.Equals(actor.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

    private static IdentityOperationResult<T> Denied<T>() =>
        IdentityOperationResult<T>.Failure("PermissionDenied", 403, "Permission denied.");

    private static string? UnavailableReason(string state) => state switch
    {
        "Paused" => "PO_PAUSED",
        "Disabled" => "SYSTEM_DISABLED",
        "Uninstalled" => "NOT_INSTALLED",
        "MigrationFailed" => "MIGRATION_FAILED",
        "Blocked" => "BLOCKED",
        _ => null
    };

    private static ModulePolicyRecord? ReadModule(SqlConnection connection, Guid id, bool forUpdate) =>
        ReadModule(connection, null, id, forUpdate);

    private static ModulePolicyRecord? ReadModule(SqlConnection connection, SqlTransaction? transaction, Guid id, bool forUpdate)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT [Id], [Code], [Name], [State], [SystemEnabled], [RegistrationEnabled], [PolicyRevision]
            FROM [platform].[Module] WITH ({(forUpdate ? "UPDLOCK, ROWLOCK" : "NOLOCK")})
            WHERE [Id] = @Id;
            """;
        command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = id });
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var revision = reader.GetInt64(6);
        return new ModulePolicyRecord(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetBoolean(4), reader.GetBoolean(5), revision, ETag(revision), Array.Empty<string>(), Array.Empty<string>(), UnavailableReason(reader.GetString(3)));
    }

    private static IReadOnlyList<string> ReadDependencies(SqlConnection connection, Guid moduleId, bool incoming)
    {
        using var command = connection.CreateCommand();
        command.CommandText = incoming
            ? "SELECT d2.[Code] FROM [platform].[ModuleDependency] d JOIN [platform].[Module] d2 ON d2.[Id] = d.[DependsOnModuleId] WHERE d.[ModuleId] = @Id ORDER BY d2.[Code];"
            : "SELECT d2.[Code] FROM [platform].[ModuleDependency] d JOIN [platform].[Module] d2 ON d2.[Id] = d.[ModuleId] WHERE d.[DependsOnModuleId] = @Id ORDER BY d2.[Code];";
        command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = moduleId });
        using var reader = command.ExecuteReader();
        var values = new List<string>();
        while (reader.Read()) values.Add(reader.GetString(0));
        return values;
    }

    private static IReadOnlyList<ModulePolicyBlocker> EvaluateBlockers(SqlConnection connection, ModulePolicyRecord module, ModulePolicyChange change) =>
        EvaluateBlockers(connection, null, module, change);

    private static IReadOnlyList<ModulePolicyBlocker> EvaluateBlockers(SqlConnection connection, SqlTransaction? transaction, ModulePolicyRecord module, ModulePolicyChange change)
    {
        var blockers = new List<ModulePolicyBlocker>();
        var systemEnabled = change.SystemEnabled ?? module.SystemEnabled;

        if (systemEnabled && !module.SystemEnabled)
        {
            foreach (var dependency in ReadDependencyStates(connection, transaction, module.Id, incoming: true))
            {
                var decision = ModulePolicy.CanEnable(module.Code, Enum.TryParse<ModuleRuntimeState>(module.State, out var state) ? state : ModuleRuntimeState.Blocked,
                    new[] { new ModuleDependencyStatus(dependency.Code, dependency.Enabled, Required: true) });
                if (!decision.Allowed)
                {
                    blockers.Add(new ModulePolicyBlocker(decision.Code, decision.Message, "systemEnabled"));
                }
            }
        }

        if (!systemEnabled && module.SystemEnabled)
        {
            var dependents = ReadDependencyStates(connection, transaction, module.Id, incoming: false)
                .Select(item => new ModuleDependencyStatus(item.Code, item.Enabled, Required: true));
            var decision = ModulePolicy.CanDisable(dependents);
            if (!decision.Allowed)
            {
                blockers.Add(new ModulePolicyBlocker(decision.Code, decision.Message, "systemEnabled"));
            }
        }

        if ((change.RegistrationEnabled ?? module.RegistrationEnabled) && !systemEnabled)
        {
            blockers.Add(new ModulePolicyBlocker("DependencyUnavailable", "Registration defaults cannot be enabled while the module is disabled.", "registrationEnabled"));
        }

        if (module.State is "Paused" or "Blocked" or "Uninstalled" or "MigrationFailed")
        {
            blockers.Add(new ModulePolicyBlocker("DecisionBlocked", "This module is not currently ready for enablement.", "systemEnabled"));
        }

        return blockers;
    }

    private static IReadOnlyList<(string Code, bool Enabled)> ReadDependencyStates(SqlConnection connection, SqlTransaction? transaction, Guid moduleId, bool incoming)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = incoming
            ? "SELECT d2.[Code], d2.[SystemEnabled] FROM [platform].[ModuleDependency] d JOIN [platform].[Module] d2 ON d2.[Id] = d.[DependsOnModuleId] WHERE d.[ModuleId] = @Id;"
            : "SELECT d2.[Code], d2.[SystemEnabled] FROM [platform].[ModuleDependency] d JOIN [platform].[Module] d2 ON d2.[Id] = d.[ModuleId] WHERE d.[DependsOnModuleId] = @Id;";
        command.Parameters.Add(new SqlParameter("@Id", System.Data.SqlDbType.UniqueIdentifier) { Value = moduleId });
        using var reader = command.ExecuteReader();
        var values = new List<(string, bool)>();
        while (reader.Read()) values.Add((reader.GetString(0), reader.GetBoolean(1)));
        return values;
    }

    private static IReadOnlyList<ModulePolicyDiff> BuildDiffs(ModulePolicyRecord module, ModulePolicyChange change)
    {
        var diffs = new List<ModulePolicyDiff>();
        if (change.SystemEnabled is { } systemEnabled && systemEnabled != module.SystemEnabled)
            diffs.Add(new ModulePolicyDiff("systemEnabled", module.SystemEnabled.ToString(), systemEnabled.ToString()));
        if (change.RegistrationEnabled is { } registrationEnabled && registrationEnabled != module.RegistrationEnabled)
            diffs.Add(new ModulePolicyDiff("registrationEnabled", module.RegistrationEnabled.ToString(), registrationEnabled.ToString()));
        return diffs;
    }

    private static void WriteAudit(SqlConnection connection, SqlTransaction transaction, IdentityPrincipal actor, Guid targetId, string action, string result, string? traceId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO [security].[AuditEvent] ([ActorUserId], [OwnerUserId], [ActionKey], [TargetType], [TargetId], [Result], [TraceId])
            VALUES (@Actor, @Owner, @Action, N'platform.Module', @Target, @Result, @TraceId);
            """;
        command.Parameters.Add(new SqlParameter("@Actor", System.Data.SqlDbType.UniqueIdentifier) { Value = actor.UserId });
        command.Parameters.Add(new SqlParameter("@Owner", System.Data.SqlDbType.UniqueIdentifier) { Value = actor.OwnerId });
        command.Parameters.Add(new SqlParameter("@Action", System.Data.SqlDbType.NVarChar, 160) { Value = action });
        command.Parameters.Add(new SqlParameter("@Target", System.Data.SqlDbType.UniqueIdentifier) { Value = targetId });
        command.Parameters.Add(new SqlParameter("@Result", System.Data.SqlDbType.VarChar, 32) { Value = result });
        command.Parameters.Add(new SqlParameter("@TraceId", System.Data.SqlDbType.NVarChar, 128) { Value = (object?)traceId ?? DBNull.Value });
        command.ExecuteNonQuery();
    }

    private string SignPreview(PreviewPayload payload)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        var body = Base64Url(json);
        using var hmac = new HMACSHA256(_previewSecret);
        return body + "." + Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }

    private bool TryReadPreview(string token, Guid actorUserId, out PreviewPayload payload)
    {
        payload = default!;
        var parts = token?.Split('.') ?? Array.Empty<string>();
        if (parts.Length != 2) return false;
        try
        {
            using var hmac = new HMACSHA256(_previewSecret);
            var body = Encoding.UTF8.GetBytes(parts[0]);
            var expected = hmac.ComputeHash(body);
            var actual = FromBase64Url(parts[1]);
            if (!CryptographicOperations.FixedTimeEquals(expected, actual)) return false;
            payload = JsonSerializer.Deserialize<PreviewPayload>(FromBase64Url(parts[0]))!;
            return payload is not null && payload.ActorUserId == actorUserId;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] FromBase64Url(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
    private static string ETag(long revision) => $"\"{Convert.ToBase64String(BitConverter.GetBytes(revision))}\"";

    private sealed record PreviewPayload(Guid ActorUserId, Guid ModuleId, long PolicyRevision, bool? SystemEnabled, bool? RegistrationEnabled, DateTimeOffset ExpiresAt);
}
