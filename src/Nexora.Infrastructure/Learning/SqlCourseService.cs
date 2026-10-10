using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Learning;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Learning;

/// <summary>Personal manual Course tracking. Milestones are owned child rows; no external delivery.</summary>
public sealed class SqlCourseService : ICourseService
{
    public static readonly string[] Actions = ["learning.course.read", "learning.course.create", "learning.course.update",
        "learning.course.progress", "learning.course.milestone", "learning.course.complete", "learning.course.abandon",
        "learning.course.archive", "learning.course.unarchive", "learning.course.trash", "learning.course.restore", "learning.course.purge"];
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private const string Columns = "Id,Title,Provider,Url,ProgressMode,ManualProgress,FirstProgressAt,Status,StartedOn,CompletedOn,Notes,CreatedAt,UpdatedAt,RowVersion";
    private const string ChildColumns = "Id,Title,Position,Completed,CompletedAt,RowVersion";
    public SqlCourseService(SqlConnectionFactory connections, string secret)
    { this.connections = connections; capabilities = new(connections); receipts = new(secret); }
    private bool Allowed(IdentityPrincipal a, string action) => capabilities.IsAllowed(a, "FX40", action);
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, string action) => capabilities.IsAllowed(c, tx, a, "FX40", action);
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable", 404, "The resource is unavailable.");
    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal a)
    {
        var result = Actions.ToDictionary(k => k, k => Allowed(a, k) && (k != "learning.course.update" || Allowed(a, "learning.course.read")));
        return result.Values.Any(v => v) ? IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result) : Denied<IReadOnlyDictionary<string, bool>>();
    }
    public IdentityOperationResult<CoursePage> List(IdentityPrincipal a, Guid? cursor, string? status, string? query)
    {
        if (!Allowed(a, "learning.course.read")) return Denied<CoursePage>();
        status ??= "Planned";
        if (status is not ("Planned" or "InProgress" or "Completed" or "Abandoned" or "Archived" or "Trash") || query is { Length: > 200 })
            return Fail<CoursePage>("ValidationFailed", 422, "Choose a valid course state and a query up to 200 characters.");
        using var c = connections.Create(); c.Open();
        using var cmd = Command(c, null, $"""
            WITH selected AS (SELECT {Columns} FROM [learning].[Course] WHERE OwnerId=@Owner AND Status=@Status AND (@Query IS NULL OR CHARINDEX(@Query,Title)>0))
            SELECT TOP(26) {Columns} FROM selected WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected b WHERE b.Id=@Cursor
                AND (selected.UpdatedAt<b.UpdatedAt OR (selected.UpdatedAt=b.UpdatedAt AND selected.Id<b.Id))) ORDER BY UpdatedAt DESC,Id DESC;
            """, a.OwnerId);
        Add(cmd, "@Status", SqlDbType.VarChar, status, 64); Add(cmd, "@Cursor", SqlDbType.UniqueIdentifier, cursor);
        Add(cmd, "@Query", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(query) ? null : query.Trim(), 200);
        var items = Rows(cmd); items = Counts(c, null, a.OwnerId, items);
        return IdentityOperationResult<CoursePage>.Success(new(items.Take(25).ToArray(), items.Count > 25 ? items[24].Id : null));
    }
    public IdentityOperationResult<Course> Get(IdentityPrincipal a, Guid id)
    {
        if (!Allowed(a, "learning.course.read")) return Denied<Course>();
        using var c = connections.Create(); c.Open(); var item = Read(c, null, a.OwnerId, id);
        return item is null ? Missing<Course>() : IdentityOperationResult<Course>.Success(item);
    }
    public IdentityOperationResult<CourseAcknowledgement> Create(IdentityPrincipal a, CourseMetadata body, string key, string? trace) =>
        Mutate(a, "learning.course.create", body, key, trace, (c, tx) =>
        {
            if (!Validate(body, out var url)) return Invalid();
            var id = Guid.NewGuid();
            using var registry = Command(c, tx, """
                INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt
                JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX40' AND rt.Code='Course' AND rt.ContractVersion='courses-v1';
                """, a.OwnerId);
            Add(registry, "@Id", SqlDbType.UniqueIdentifier, id); Add(registry, "@User", SqlDbType.UniqueIdentifier, a.UserId);
            if (registry.ExecuteNonQuery() != 1) return Fail<CourseAcknowledgement>("DependencyUnavailable", 409, "The course contract is unavailable.");
            using var cmd = Command(c, tx, """
                INSERT [learning].[Course](Id,OwnerId,Title,Provider,Url,ProgressMode,Status,StartedOn,Notes,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Title,@Provider,@Url,@Mode,'Planned',@Start,@Notes,SYSUTCDATETIME(),@User,@User);
                """, a.OwnerId);
            Fields(cmd, a, id, body, url); cmd.ExecuteNonQuery(); return Ack(c, tx, a, id, status: 201);
        });
    public IdentityOperationResult<CourseAcknowledgement> Update(IdentityPrincipal a, Guid id, string? etag, CourseMetadata body, string key, string? trace) =>
        Mutate(a, "learning.course.update", new { id, etag, body }, key, trace, (c, tx) =>
        {
            var item = Read(c, tx, a.OwnerId, id); var error = Editable(item, etag); if (error is not null) return error;
            if (!Validate(body, out var url) || (body.StartedOn is { } start && item!.CompletedOn is { } end && end < start)) return Invalid();
            if (item!.ProgressModeLocked && body.ProgressMode != item.ProgressMode)
                return Fail<CourseAcknowledgement>("ProgressModeLocked", 409, "Progress mode is fixed after the first recorded progress.");
            using var cmd = Command(c, tx, """
                UPDATE [learning].[Course] SET Title=@Title,Provider=@Provider,Url=@Url,ProgressMode=@Mode,StartedOn=@Start,Notes=@Notes,
                    UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """, a.OwnerId);
            Fields(cmd, a, id, body, url); cmd.ExecuteNonQuery(); UpdateResource(c, tx, a, id, "Active"); return Ack(c, tx, a, id);
        });
    public IdentityOperationResult<CourseAcknowledgement> Progress(IdentityPrincipal a, Guid id, string? etag, CourseProgress body, string key, string? trace) =>
        Mutate(a, "learning.course.progress", new { id, etag, body }, key, trace, (c, tx) =>
        {
            var item = Read(c, tx, a.OwnerId, id); var error = Editable(item, etag); if (error is not null) return error;
            decimal? fraction = null;
            if (item!.ProgressMode == "ManualPercent")
            { if (!ParseFraction(body.ManualProgress, out var value)) return Invalid(); fraction = value; }
            else if (body.ManualProgress is not null || !body.Start) return Invalid();
            if ((body.Start && (item.Status != "Planned" || body.StartedOn is null)) || (!body.Start && body.StartedOn is not null)) return Invalid();
            using var cmd = Command(c, tx, """
                UPDATE [learning].[Course] SET ManualProgress=@Progress,FirstProgressAt=COALESCE(FirstProgressAt,SYSUTCDATETIME()),
                    Status=CASE WHEN @Start=1 THEN 'InProgress' ELSE Status END,
                    StartedOn=CASE WHEN @Start=1 THEN @Date ELSE StartedOn END,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """, a.OwnerId);
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Start", SqlDbType.Bit, body.Start);
            Add(cmd, "@Date", SqlDbType.Date, body.StartedOn?.ToDateTime(TimeOnly.MinValue)); Add(cmd, "@User", SqlDbType.UniqueIdentifier, a.UserId);
            Decimal(cmd, "@Progress", fraction); cmd.ExecuteNonQuery(); UpdateResource(c, tx, a, id, "Active"); return Ack(c, tx, a, id);
        });
    public IdentityOperationResult<CoursePreview> Preview(IdentityPrincipal a, Guid id, string operation)
    {
        var action = Action(operation); if (action is null) return Fail<CoursePreview>("ValidationFailed", 422, "Choose a supported operation.");
        if (!Allowed(a, action)) return Denied<CoursePreview>();
        using var c = connections.Create(); c.Open(); var item = Read(c, null, a.OwnerId, id);
        if (item is null) return Missing<CoursePreview>();
        if (!Eligible(item.Status, operation)) return Fail<CoursePreview>("LifecycleLocked", 409, "The course state does not allow this operation.");
        return IdentityOperationResult<CoursePreview>.Success(new(id, item.ETag, operation, ReferenceCount(c, null, a.OwnerId, id)));
    }
    public IdentityOperationResult<CourseAcknowledgement> Transition(IdentityPrincipal a, Guid id, string? etag,
        string operation, CourseConfirmation body, string key, string? trace)
    {
        var action = Action(operation); if (action is null) return Invalid();
        return Mutate(a, action, new { id, etag, operation, body }, key, trace, (c, tx) =>
        {
            var item = Read(c, tx, a.OwnerId, id); if (item is null) return Missing<CourseAcknowledgement>();
            var error = Precondition(item.ETag, etag); if (error is not null) return error;
            if (!body.Confirm || (operation == "complete" ? body.CompletedOn is null || (item.StartedOn is { } s && body.CompletedOn < s) : body.CompletedOn is not null)) return Invalid();
            if (!Eligible(item.Status, operation)) return Fail<CourseAcknowledgement>("LifecycleLocked", 409, "The course state does not allow this operation.");
            var next = operation switch { "complete" => "Completed", "abandon" => "Abandoned", "archive" => "Archived",
                "unarchive" => Previous(c, tx, a.OwnerId, id, "PreArchiveState"), "trash" => "Trash",
                "restore" => Previous(c, tx, a.OwnerId, id, "PreTrashState"), "purge" => "Purged", _ => null };
            if (next is null) return Invalid();
            if (operation == "purge" && ReferenceCount(c, tx, a.OwnerId, id) != 0)
                return Fail<CourseAcknowledgement>("ResourcePinned", 409, "A retained reference prevents permanent deletion.");
            var fingerprint = operation == "trash" ? Fingerprint(c, tx, a.OwnerId, id) : null;
            if (operation is "restore" or "purge")
            {
                using var saved = Command(c, tx, "SELECT TrashMilestoneFingerprint FROM [learning].[Course] WHERE OwnerId=@Owner AND Id=@Id;", a.OwnerId);
                Add(saved, "@Id", SqlDbType.UniqueIdentifier, id);
                if (saved.ExecuteScalar() is not byte[] expected || !CryptographicOperations.FixedTimeEquals(expected, Fingerprint(c, tx, a.OwnerId, id)))
                    return Fail<CourseAcknowledgement>("LifecycleLocked", 409, "The owned milestone cohort changed.");
            }
            var batch = Guid.NewGuid();
            if (operation is "trash" or "restore" or "purge")
            {
                using var cohort = Command(c, tx, operation == "trash" ? """
                    INSERT [operations].[TrashBatch](Id,OwnerId,RootResourceId,DeletedAt,State,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                    VALUES(@Batch,@Owner,@Id,SYSUTCDATETIME(),'Trashed',SYSUTCDATETIME(),@User,@User);
                    INSERT [operations].[TrashMember](Id,OwnerId,BatchId,ResourceId,PreviousLifecycle,Depth,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                    VALUES(NEWID(),@Owner,@Batch,@Id,@Previous,0,SYSUTCDATETIME(),@User,@User); SELECT 1;
                    """ : """
                    DECLARE @CurrentBatch uniqueidentifier=(SELECT TrashBatchId FROM [learning].[Course] WHERE OwnerId=@Owner AND Id=@Id);
                    IF NOT EXISTS(SELECT 1 FROM [operations].[TrashBatch] b JOIN [operations].[TrashMember] m ON m.OwnerId=b.OwnerId AND m.BatchId=b.Id
                      WHERE b.OwnerId=@Owner AND b.Id=@CurrentBatch AND b.RootResourceId=@Id AND b.State='Trashed' AND m.ResourceId=@Id
                      AND m.PreviousLifecycle=@Previous AND m.Depth=0 AND m.ParentResourceId IS NULL AND m.PurgedAt IS NULL)
                      OR (SELECT COUNT(*) FROM [operations].[TrashMember] WHERE OwnerId=@Owner AND BatchId=@CurrentBatch)<>1 SELECT 0;
                    ELSE BEGIN
                      UPDATE [operations].[TrashBatch] SET State=CASE WHEN @Operation='restore' THEN 'Restored' ELSE 'Purged' END,
                        RestoredAt=CASE WHEN @Operation='restore' THEN SYSUTCDATETIME() ELSE NULL END,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                        WHERE OwnerId=@Owner AND Id=@CurrentBatch;
                      IF @Operation='purge' UPDATE [operations].[TrashMember] SET PurgedAt=SYSUTCDATETIME(),UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                        WHERE OwnerId=@Owner AND BatchId=@CurrentBatch AND ResourceId=@Id; SELECT 1;
                    END;
                    """, a.OwnerId);
                Add(cohort, "@Id", SqlDbType.UniqueIdentifier, id); Add(cohort, "@Batch", SqlDbType.UniqueIdentifier, batch); Add(cohort, "@User", SqlDbType.UniqueIdentifier, a.UserId);
                Add(cohort, "@Operation", SqlDbType.VarChar, operation, 64); Add(cohort, "@Previous", SqlDbType.VarChar, operation == "trash" ? item.Status : Previous(c, tx, a.OwnerId, id, "PreTrashState"), 64);
                if (Convert.ToInt32(cohort.ExecuteScalar()) != 1) return Fail<CourseAcknowledgement>("LifecycleLocked", 409, "The deletion cohort is unavailable.");
            }
            using var cmd = Command(c, tx, next == "Purged" ? """
                DELETE [learning].[CourseCompletionHistory] WHERE OwnerId=@Owner AND CourseId=@Id;
                DELETE [learning].[CourseMilestone] WHERE OwnerId=@Owner AND CourseId=@Id;
                DELETE [learning].[Course] WHERE OwnerId=@Owner AND Id=@Id;
                """ : """
                UPDATE [learning].[Course] SET
                  PreArchiveState=CASE WHEN @Operation='archive' THEN Status WHEN @Operation='unarchive' THEN NULL ELSE PreArchiveState END,
                  PreTrashState=CASE WHEN @Operation='trash' THEN Status WHEN @Operation='restore' THEN NULL ELSE PreTrashState END,
                  TrashBatchId=CASE WHEN @Operation='trash' THEN @Batch WHEN @Operation='restore' THEN NULL ELSE TrashBatchId END,
                  TrashMilestoneFingerprint=CASE WHEN @Operation='trash' THEN @Fingerprint WHEN @Operation='restore' THEN NULL ELSE TrashMilestoneFingerprint END,
                  CompletedOn=CASE WHEN @Operation='complete' THEN @Complete ELSE CompletedOn END,
                  Status=@Status,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                IF @Operation='complete' INSERT [learning].[CourseCompletionHistory](Id,OwnerId,CourseId,CompletedOn,ActorUserId)
                    VALUES(NEWID(),@Owner,@Id,@Complete,@User);
                """, a.OwnerId);
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Operation", SqlDbType.VarChar, operation, 64); Add(cmd, "@Status", SqlDbType.VarChar, next, 64);
            Add(cmd, "@User", SqlDbType.UniqueIdentifier, a.UserId); Add(cmd, "@Batch", SqlDbType.UniqueIdentifier, batch);
            Add(cmd, "@Fingerprint", SqlDbType.Binary, fingerprint, 32); Add(cmd, "@Complete", SqlDbType.Date, body.CompletedOn?.ToDateTime(TimeOnly.MinValue)); cmd.ExecuteNonQuery();
            UpdateResource(c, tx, a, id, next is "Archived" or "Trash" or "Purged" ? next : "Active");
            return next == "Purged" ? IdentityOperationResult<CourseAcknowledgement>.Success(new(id, null)) : Ack(c, tx, a, id);
        });
    }
    public IdentityOperationResult<CourseMilestonePage> Milestones(IdentityPrincipal a, Guid id, Guid? cursor)
    {
        if (!Allowed(a, "learning.course.read")) return Denied<CourseMilestonePage>();
        using var c = connections.Create(); c.Open(); var parent = Read(c, null, a.OwnerId, id); if (parent is null) return Missing<CourseMilestonePage>();
        using var cmd = Command(c, null, $"""
            WITH selected AS (SELECT {ChildColumns} FROM [learning].[CourseMilestone] WHERE OwnerId=@Owner AND CourseId=@Id)
            SELECT TOP(26) {ChildColumns} FROM selected WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected b WHERE b.Id=@Cursor
                AND (selected.Position>b.Position OR (selected.Position=b.Position AND selected.Id>b.Id))) ORDER BY Position,Id;
            """, a.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Cursor", SqlDbType.UniqueIdentifier, cursor); var rows = ChildRows(cmd);
        return IdentityOperationResult<CourseMilestonePage>.Success(new(rows.Take(25).ToArray(), rows.Count > 25 ? rows[24].Id : null, parent.ETag));
    }
    public IdentityOperationResult<CourseAcknowledgement> AddMilestone(IdentityPrincipal a, Guid id, string? etag, MilestoneDefinition body, string key, string? trace) =>
        Mutate(a, "learning.course.milestone", new { id, etag, body }, key, trace, (c, tx) =>
        {
            var parent = Read(c, tx, a.OwnerId, id); var error = Editable(parent, etag); if (error is not null) return error;
            if (!ValidTitle(body.Title)) return Invalid();
            using var position = Command(c, tx, "SELECT COALESCE(MAX(Position),-1) FROM [learning].[CourseMilestone] WHERE OwnerId=@Owner AND CourseId=@Id;", a.OwnerId);
            Add(position, "@Id", SqlDbType.UniqueIdentifier, id); var max = Convert.ToInt32(position.ExecuteScalar());
            if (max == int.MaxValue) return Fail<CourseAcknowledgement>("PositionUnavailable", 409, "The milestone ordering limit is reached.");
            var child = Guid.NewGuid(); using var cmd = Command(c, tx, """
                INSERT [learning].[CourseMilestone](Id,OwnerId,CourseId,Title,Position,Completed,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(@Child,@Owner,@Id,@Title,@Position,0,SYSUTCDATETIME(),@User,@User);
                """, a.OwnerId);
            ChildFields(cmd, a, id, child); Add(cmd, "@Title", SqlDbType.NVarChar, body.Title.Trim(), 200); Add(cmd, "@Position", SqlDbType.Int, max + 1); cmd.ExecuteNonQuery();
            Touch(c, tx, a, id, false); return Ack(c, tx, a, id, child);
        });
    public IdentityOperationResult<CourseAcknowledgement> ChangeMilestone(IdentityPrincipal a, Guid id, Guid child, string? etag, string? childETag,
        string operation, object body, string key, string? trace)
    {
        if (operation is not ("update" or "delete" or "completion" or "move")) return Invalid();
        var action = operation == "completion" ? "learning.course.progress" : "learning.course.milestone";
        return Mutate(a, action, new { id, child, etag, childETag, operation, body }, key, trace, (c, tx) =>
        {
            var parent = Read(c, tx, a.OwnerId, id); var error = Editable(parent, etag); if (error is not null) return error;
            var item = ReadChild(c, tx, a.OwnerId, id, child); if (item is null) return Missing<CourseAcknowledgement>();
            error = Precondition(item.ETag, childETag); if (error is not null) return error;
            if (operation == "completion" && parent!.ProgressMode != "Milestones") return Invalid();
            if (operation == "update")
            {
                if (body is not MilestoneDefinition definition || !ValidTitle(definition.Title)) return Invalid();
                using var cmd = Command(c, tx, "UPDATE [learning].[CourseMilestone] SET Title=@Title,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND CourseId=@Id AND Id=@Child;", a.OwnerId);
                ChildFields(cmd, a, id, child); Add(cmd, "@Title", SqlDbType.NVarChar, definition.Title.Trim(), 200); cmd.ExecuteNonQuery();
            }
            else if (operation == "delete")
            {
                using var cmd = Command(c, tx, "DELETE [learning].[CourseMilestone] WHERE OwnerId=@Owner AND CourseId=@Id AND Id=@Child;", a.OwnerId); ChildFields(cmd, a, id, child); cmd.ExecuteNonQuery();
            }
            else if (operation == "completion")
            {
                if (body is not MilestoneCompletion completion) return Invalid();
                using var cmd = Command(c, tx, """
                    UPDATE [learning].[CourseMilestone] SET Completed=@Completed,
                      CompletedAt=CASE WHEN @Completed=0 THEN NULL WHEN Completed=1 THEN CompletedAt ELSE SYSUTCDATETIME() END,
                      UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND CourseId=@Id AND Id=@Child;
                    """, a.OwnerId);
                ChildFields(cmd, a, id, child); Add(cmd, "@Completed", SqlDbType.Bit, completion.Completed); cmd.ExecuteNonQuery();
            }
            else
            {
                if (body is not MilestoneMove move || move.Direction is not ("up" or "down")) return Invalid();
                using var neighbor = Command(c, tx, $"SELECT TOP(1) {ChildColumns} FROM [learning].[CourseMilestone] WHERE OwnerId=@Owner AND CourseId=@Id AND Position{(move.Direction == "up" ? "<" : ">")}@Position ORDER BY Position {(move.Direction == "up" ? "DESC" : "ASC")};", a.OwnerId);
                Add(neighbor, "@Id", SqlDbType.UniqueIdentifier, id); Add(neighbor, "@Position", SqlDbType.Int, item.Position); var other = ChildRows(neighbor).SingleOrDefault();
                if (other is null) return Fail<CourseAcknowledgement>("PositionUnavailable", 409, "There is no milestone in that direction.");
                using var maximum = Command(c, tx, "SELECT MAX(Position) FROM [learning].[CourseMilestone] WHERE OwnerId=@Owner AND CourseId=@Id;", a.OwnerId);
                Add(maximum, "@Id", SqlDbType.UniqueIdentifier, id); var max = Convert.ToInt32(maximum.ExecuteScalar());
                if (max == int.MaxValue) return Fail<CourseAcknowledgement>("PositionUnavailable", 409, "The milestone ordering limit is reached.");
                Move(c, tx, a, id, child, max + 1); Move(c, tx, a, id, other.Id, item.Position); Move(c, tx, a, id, child, other.Position);
            }
            Touch(c, tx, a, id, operation == "completion"); return Ack(c, tx, a, id, operation == "delete" ? null : child);
        }, operation == "delete" ? (c, tx) => ReadChild(c, tx, a.OwnerId, id, child)?.Completed == true ? "learning.course.progress" : null : null);
    }
    private IdentityOperationResult<CourseAcknowledgement> Mutate(IdentityPrincipal a, string action, object request, string key, string? trace,
        Func<SqlConnection, SqlTransaction, IdentityOperationResult<CourseAcknowledgement>> work, Func<SqlConnection, SqlTransaction, string?>? extra = null)
    {
        bool Current() => Allowed(a, action) && (action != "learning.course.update" || Allowed(a, "learning.course.read"));
        if (!Current()) return Denied<CourseAcknowledgement>();
        if (!Guid.TryParse(key, out var parsed) || parsed == Guid.Empty) return Fail<CourseAcknowledgement>("IdempotencyKeyRequired", 422, "A nonempty UUID idempotency key is required.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;", a.OwnerId))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, a.UserId); if (owner.ExecuteScalar() is null) return Denied<CourseAcknowledgement>(); }
        var requiredExtra = extra?.Invoke(c, tx);
        bool TransactionAllowed() => Allowed(c, tx, a, action) && (action != "learning.course.update" || Allowed(c, tx, a, "learning.course.read")) && (requiredExtra is null || Allowed(c, tx, a, requiredExtra));
        if (!TransactionAllowed()) return Denied<CourseAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, a.UserId, action, key, JsonSerializer.Serialize(request), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<CourseAcknowledgement>("IdempotencyConflict", 409, "This key belongs to a different request.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
        {
            var receipt = JsonSerializer.Deserialize<CommandReceipt>(saved)!;
            if (receipt.ExtraAction is { } dependency && !Allowed(c, tx, a, dependency)) return Denied<CourseAcknowledgement>();
            return IdentityOperationResult<CourseAcknowledgement>.Success(receipt.Acknowledgement, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
        }
        if (!claim.IsClaimed) return Fail<CourseAcknowledgement>("RequestInProgress", 409, "The request is already in progress.");
        var result = work(c, tx); if (!result.Succeeded || result.Value is null) return result;
        if (!TransactionAllowed()) return Denied<CourseAcknowledgement>();
        using var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'learning.Course',@Id,'Succeeded',@Trace);", a.OwnerId);
        Add(audit, "@User", SqlDbType.UniqueIdentifier, a.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160); Add(audit, "@Id", SqlDbType.UniqueIdentifier, result.Value.ItemId); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery();
        receipts.Complete(c, tx, claim, result.Code, result.StatusCode, JsonSerializer.Serialize(new CommandReceipt(result.Value, requiredExtra))); tx.Commit(); return result;
    }
    private sealed record CommandReceipt(CourseAcknowledgement Acknowledgement, string? ExtraAction);
    private static IdentityOperationResult<CourseAcknowledgement> Invalid() => Fail<CourseAcknowledgement>("ValidationFailed", 422, "Check course fields, progress mode, exact progress and dates.");
    private static IdentityOperationResult<CourseAcknowledgement>? Editable(Course? item, string? etag) => item is null ? Missing<CourseAcknowledgement>() :
        Precondition(item.ETag, etag) ?? (item.Status is "Archived" or "Trash" ? Fail<CourseAcknowledgement>("LifecycleLocked", 409, "Restore or unarchive the course before editing.") : null);
    private static IdentityOperationResult<CourseAcknowledgement>? Precondition(string expected, string? etag) => string.IsNullOrWhiteSpace(etag) ?
        Fail<CourseAcknowledgement>("PreconditionRequired", 428, "If-Match is required.") : expected == etag ? null : Fail<CourseAcknowledgement>("RevisionConflict", 412, "Reload the current course or milestone.");
    private static string? Action(string op) => op is "complete" or "abandon" or "archive" or "unarchive" or "trash" or "restore" or "purge" ? "learning.course." + op : null;
    private static bool Eligible(string state, string op) => op switch { "complete" or "abandon" => state is "Planned" or "InProgress",
        "archive" => state is "Planned" or "InProgress" or "Completed" or "Abandoned", "unarchive" => state == "Archived",
        "trash" => state != "Trash", "restore" or "purge" => state == "Trash", _ => false };
    private static bool ValidTitle(string? title) => !string.IsNullOrWhiteSpace(title) && title.Length <= 200;
    private static bool Validate(CourseMetadata b, out string? url)
    {
        url = string.IsNullOrWhiteSpace(b.Url) ? null : b.Url.Trim();
        return ValidTitle(b.Title) && b.Provider is not { Length: > 200 } && b.Notes is not { Length: > 20000 } && b.ProgressMode is "ManualPercent" or "Milestones" &&
            (url is null || (url.Length <= 2048 && Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme is "http" or "https" && u.UserInfo.Length == 0));
    }
    private static bool ParseFraction(string? text, out decimal value)
    { value = 0; return text is not null && Regex.IsMatch(text, "\\A(?:0(?:\\.[0-9]{1,8})?|1(?:\\.0{1,8})?)\\z", RegexOptions.CultureInvariant) && decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value); }
    private static void Fields(SqlCommand cmd, IdentityPrincipal a, Guid id, CourseMetadata b, string? url)
    {
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@User", SqlDbType.UniqueIdentifier, a.UserId); Add(cmd, "@Title", SqlDbType.NVarChar, b.Title.Trim(), 200);
        Add(cmd, "@Provider", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(b.Provider) ? null : b.Provider.Trim(), 200); Add(cmd, "@Url", SqlDbType.NVarChar, url, 2048);
        Add(cmd, "@Mode", SqlDbType.VarChar, b.ProgressMode, 64); Add(cmd, "@Notes", SqlDbType.NVarChar, b.Notes?.Trim(), -1); Add(cmd, "@Start", SqlDbType.Date, b.StartedOn?.ToDateTime(TimeOnly.MinValue));
    }
    private static void ChildFields(SqlCommand cmd, IdentityPrincipal a, Guid id, Guid child)
    { Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Child", SqlDbType.UniqueIdentifier, child); Add(cmd, "@User", SqlDbType.UniqueIdentifier, a.UserId); }
    private static void Touch(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, Guid id, bool progress)
    {
        using var cmd = Command(c, tx, "UPDATE [learning].[Course] SET FirstProgressAt=CASE WHEN @Progress=1 THEN COALESCE(FirstProgressAt,SYSUTCDATETIME()) ELSE FirstProgressAt END,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;", a.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@User", SqlDbType.UniqueIdentifier, a.UserId); Add(cmd, "@Progress", SqlDbType.Bit, progress); cmd.ExecuteNonQuery(); UpdateResource(c, tx, a, id, "Active");
    }
    private static void Move(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, Guid id, Guid child, int position)
    {
        using var cmd = Command(c, tx, "UPDATE [learning].[CourseMilestone] SET Position=@Position,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND CourseId=@Id AND Id=@Child;", a.OwnerId);
        ChildFields(cmd, a, id, child); Add(cmd, "@Position", SqlDbType.Int, position); cmd.ExecuteNonQuery();
    }
    private static byte[] Fingerprint(SqlConnection c, SqlTransaction tx, Guid owner, Guid id)
    {
        using var cmd = Command(c, tx, "SELECT Id,Position,RowVersion FROM [learning].[CourseMilestone] WITH(UPDLOCK,HOLDLOCK) WHERE OwnerId=@Owner AND CourseId=@Id ORDER BY Position,Id;", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); using var r = cmd.ExecuteReader(); using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream);
        var count = 0; while (r.Read()) { writer.Write(r.GetGuid(0).ToByteArray()); writer.Write(r.GetInt32(1)); writer.Write(r.GetFieldValue<byte[]>(2)); count++; }
        writer.Write(count); writer.Flush(); return SHA256.HashData(stream.ToArray());
    }
    private static IdentityOperationResult<CourseAcknowledgement> Ack(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, Guid id, Guid? child = null, int status = 200) =>
        IdentityOperationResult<CourseAcknowledgement>.Success(new(id, Read(c, tx, a.OwnerId, id)!.ETag, child, child is { } childId ? ReadChild(c, tx, a.OwnerId, id, childId)!.ETag : null), status);
    private static void UpdateResource(SqlConnection c, SqlTransaction tx, IdentityPrincipal a, Guid id, string availability)
    {
        using var cmd = Command(c, tx, "UPDATE [platform].[Resource] SET Availability=@Availability,Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User,PurgedAt=CASE WHEN @Availability='Purged' THEN SYSUTCDATETIME() ELSE NULL END WHERE OwnerId=@Owner AND Id=@Id;", a.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@User", SqlDbType.UniqueIdentifier, a.UserId); Add(cmd, "@Availability", SqlDbType.VarChar, availability, 64);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Course registry integrity failed.");
    }
    private static int ReferenceCount(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id)
    { using var cmd = Command(c, tx, "SELECT COUNT(*) FROM [platform].[ResourceLink] WHERE OwnerId=@Owner AND (SourceResourceId=@Id OR TargetResourceId=@Id) AND State<>'Detached';", owner); Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Convert.ToInt32(cmd.ExecuteScalar()); }
    private static string? Previous(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, string column)
    { if (column is not ("PreArchiveState" or "PreTrashState")) throw new ArgumentException("Unknown lifecycle field.", nameof(column)); using var cmd = Command(c, tx, $"SELECT {column} FROM [learning].[Course] WHERE OwnerId=@Owner AND Id=@Id;", owner); Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return cmd.ExecuteScalar() as string; }
    private static Course? Read(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id)
    { using var cmd = Command(c, tx, $"SELECT {Columns} FROM [learning].[Course] WHERE OwnerId=@Owner AND Id=@Id;", owner); Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Counts(c, tx, owner, Rows(cmd)).FirstOrDefault(); }
    private static List<Course> Counts(SqlConnection c, SqlTransaction? tx, Guid owner, List<Course> items)
    {
        for (var n = 0; n < items.Count; n++)
        {
            using var cmd = Command(c, tx, "SELECT COUNT(*),COALESCE(SUM(CASE WHEN Completed=1 THEN 1 ELSE 0 END),0) FROM [learning].[CourseMilestone] WHERE OwnerId=@Owner AND CourseId=@Id;", owner);
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, items[n].Id); using var r = cmd.ExecuteReader(); r.Read(); items[n] = items[n] with { MilestonesTotal = r.GetInt32(0), MilestonesDone = r.GetInt32(1) };
        }
        return items;
    }
    private static List<Course> Rows(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader(); var items = new List<Course>();
        while (r.Read()) items.Add(new(r.GetGuid(0), r.GetString(1), Text(r, 2), Text(r, 3), r.GetString(4), r.IsDBNull(5) ? null : r.GetDecimal(5).ToString("0.########", CultureInfo.InvariantCulture),
            !r.IsDBNull(6), 0, 0, r.GetString(7), Date(r, 8), Date(r, 9), Text(r, 10), Utc(r.GetDateTime(11)), Utc(r.GetDateTime(12)), Tag(r.GetFieldValue<byte[]>(13)))); return items;
    }
    private static CourseMilestone? ReadChild(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id, Guid child)
    { using var cmd = Command(c, tx, $"SELECT {ChildColumns} FROM [learning].[CourseMilestone] WHERE OwnerId=@Owner AND CourseId=@Id AND Id=@Child;", owner); Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Child", SqlDbType.UniqueIdentifier, child); return ChildRows(cmd).FirstOrDefault(); }
    private static List<CourseMilestone> ChildRows(SqlCommand cmd)
    { using var r = cmd.ExecuteReader(); var items = new List<CourseMilestone>(); while (r.Read()) items.Add(new(r.GetGuid(0), r.GetString(1), r.GetInt32(2), r.GetBoolean(3), r.IsDBNull(4) ? null : Utc(r.GetDateTime(4)), Tag(r.GetFieldValue<byte[]>(5)))); return items; }
    private static string? Text(SqlDataReader r, int n) => r.IsDBNull(n) ? null : r.GetString(n);
    private static DateOnly? Date(SqlDataReader r, int n) => r.IsDBNull(n) ? null : DateOnly.FromDateTime(r.GetDateTime(n));
    private static string Tag(byte[] value) => "\"" + Convert.ToBase64String(value) + "\"";
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static SqlCommand Command(SqlConnection c, SqlTransaction? tx, string sql, Guid owner)
    { var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; Add(cmd, "@Owner", SqlDbType.UniqueIdentifier, owner); return cmd; }
    private static void Add(SqlCommand cmd, string name, SqlDbType type, object? value, int size = 0)
    { var p = cmd.Parameters.Add(name, type); if (size != 0) p.Size = size; p.Value = value ?? DBNull.Value; }
    private static void Decimal(SqlCommand cmd, string name, decimal? value)
    { var p = cmd.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 28; p.Scale = 8; p.Value = (object?)value ?? DBNull.Value; }
}
