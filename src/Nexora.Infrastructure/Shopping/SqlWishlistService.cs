using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Shopping;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Shopping;

/// <summary>Manual desired purchases only. No tracked-price, checkout, order, Finance or provider effects.</summary>
public sealed class SqlWishlistService
    : IWishlistService
{
    public static readonly string[] Actions = ["shopping.wishlist.read", "shopping.wishlist.create", "shopping.wishlist.update",
        "shopping.wishlist.mark_purchased", "shopping.wishlist.archive", "shopping.wishlist.unarchive",
        "shopping.wishlist.trash", "shopping.wishlist.restore", "shopping.wishlist.purge"];
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    private const string Columns = "Id,Title,Url,Quantity,TargetAmount,Currency,Notes,Status,CreatedAt,UpdatedAt,RowVersion";
    private static readonly Regex DecimalPattern = new("^[0-9]{1,20}(?:\\.[0-9]{1,8})?$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly HashSet<string> Currencies = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
        .Select(culture => new RegionInfo(culture.Name).ISOCurrencySymbol).ToHashSet(StringComparer.Ordinal);

    public SqlWishlistService(SqlConnectionFactory connections, string secret)
    { this.connections = connections; capabilities = new(connections); receipts = new(secret); }
    private bool Allowed(IdentityPrincipal actor, string action) => capabilities.IsAllowed(actor, "FX31", action);
    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) =>
        capabilities.IsAllowed(c, tx, actor, "FX31", action);
    private static IdentityOperationResult<T> Fail<T>(string code, int status, string title) => IdentityOperationResult<T>.Failure(code, status, title);
    private static IdentityOperationResult<T> Denied<T>() => Fail<T>("ModuleUnavailable", 403, "The module or action is unavailable.");
    private static IdentityOperationResult<T> Missing<T>() => Fail<T>("ResourceUnavailable", 404, "The resource is unavailable.");

    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        var result = Actions.ToDictionary(action => action, action => Allowed(actor, action));
        return result.Values.Any(value => value) ? IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(result)
            : Denied<IReadOnlyDictionary<string, bool>>();
    }
    public IdentityOperationResult<WishlistPage> List(IdentityPrincipal actor, Guid? cursor, string? status, string? query)
    {
        if (!Allowed(actor, "shopping.wishlist.read")) return Denied<WishlistPage>();
        status ??= "Wanted";
        if (status is not ("Wanted" or "Purchased" or "Archived" or "Trash") || query is { Length: > 200 })
            return Fail<WishlistPage>("ValidationFailed", 422, "Choose a valid wishlist state and a query up to 200 characters.");
        using var c = connections.Create(); c.Open();
        using var cmd = Command(c, null, $"""
            WITH selected AS (SELECT {Columns} FROM [shopping].[WishlistItem]
                WHERE OwnerId=@Owner AND Status=@Status AND (@Query IS NULL OR CHARINDEX(@Query,Title)>0))
            SELECT TOP(26) {Columns} FROM selected
            WHERE @Cursor IS NULL OR EXISTS(SELECT 1 FROM selected boundary WHERE boundary.Id=@Cursor
                AND (selected.UpdatedAt<boundary.UpdatedAt OR (selected.UpdatedAt=boundary.UpdatedAt AND selected.Id<boundary.Id)))
            ORDER BY UpdatedAt DESC,Id DESC;
            """, actor.OwnerId);
        Add(cmd, "@Status", SqlDbType.VarChar, status, 64); Add(cmd, "@Cursor", SqlDbType.UniqueIdentifier, cursor);
        Add(cmd, "@Query", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(query) ? null : query.Trim(), 200);
        var items = Rows(cmd);
        return IdentityOperationResult<WishlistPage>.Success(new(items.Take(25).ToArray(), items.Count > 25 ? items[24].Id : null));
    }
    public IdentityOperationResult<WishlistItem> Get(IdentityPrincipal actor, Guid id)
    {
        if (!Allowed(actor, "shopping.wishlist.read")) return Denied<WishlistItem>();
        using var c = connections.Create(); c.Open(); var item = Read(c, null, actor.OwnerId, id);
        return item is null ? Missing<WishlistItem>() : IdentityOperationResult<WishlistItem>.Success(item);
    }
    public IdentityOperationResult<WishlistAcknowledgement> Save(IdentityPrincipal actor, Guid? id, string? etag,
        WishlistCommand body, string key, string? trace)
    {
        var action = id is null ? "shopping.wishlist.create" : "shopping.wishlist.update";
        return Mutate(actor, action, new { id, etag, body }, key, trace, (c, tx) =>
        {
            if (id is not null && !Allowed(c, tx, actor, "shopping.wishlist.read")) return Denied<WishlistAcknowledgement>();
            if (!Validate(body, out var quantity, out var amount, out var currency, out var url))
                return Fail<WishlistAcknowledgement>("ValidationFailed", 422, "Check title, URL, quantity, desired amount, explicit currency and notes.");
            var target = id ?? Guid.NewGuid();
            if (id is { } existing)
            {
                var item = Read(c, tx, actor.OwnerId, existing);
                if (item is null) return Missing<WishlistAcknowledgement>();
                var stale = Precondition<WishlistAcknowledgement>(item, etag); if (stale is not null) return stale;
                if (item.Status is "Archived" or "Trash") return Fail<WishlistAcknowledgement>("LifecycleLocked", 409, "Unarchive or restore the item before editing.");
            }
            else
            {
                using var resource = Command(c, tx, """
                    INSERT [platform].[Resource](Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                    SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM [platform].[ResourceType] rt
                    JOIN [platform].[Module] m ON m.Id=rt.ModuleId WHERE m.Code='FX31' AND rt.Code='WishlistItem' AND rt.ContractVersion='wishlist-v1';
                    """, actor.OwnerId);
                Add(resource, "@Id", SqlDbType.UniqueIdentifier, target); Add(resource, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
                if (resource.ExecuteNonQuery() != 1) return Fail<WishlistAcknowledgement>("DependencyUnavailable", 409, "The wishlist resource contract is unavailable.");
            }
            using var cmd = Command(c, tx, id is null ? """
                INSERT [shopping].[WishlistItem](Id,OwnerId,Title,Url,Quantity,TargetAmount,Currency,Notes,Status,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Title,@Url,@Quantity,@Amount,@Currency,@Notes,'Wanted',SYSUTCDATETIME(),@User,@User);
                """ : """
                UPDATE [shopping].[WishlistItem] SET Title=@Title,Url=@Url,Quantity=@Quantity,TargetAmount=@Amount,
                    Currency=@Currency,Notes=@Notes,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                WHERE OwnerId=@Owner AND Id=@Id;
                """, actor.OwnerId);
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, target); Add(cmd, "@Title", SqlDbType.NVarChar, body.Title.Trim(), 200);
            Add(cmd, "@Url", SqlDbType.NVarChar, url, 2048); Decimal(cmd, "@Quantity", quantity); Decimal(cmd, "@Amount", amount);
            Add(cmd, "@Currency", SqlDbType.Char, currency, 3); Add(cmd, "@Notes", SqlDbType.NVarChar, body.Notes?.Trim(), -1);
            Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId); cmd.ExecuteNonQuery();
            if (id is not null) UpdateResource(c, tx, actor, target, "Active");
            var updated = Read(c, tx, actor.OwnerId, target)!;
            return IdentityOperationResult<WishlistAcknowledgement>.Success(new(target, updated.ETag), id is null ? 201 : 200);
        });
    }
    public IdentityOperationResult<WishlistPreview> Preview(IdentityPrincipal actor, Guid id, string operation)
    {
        var action = Action(operation);
        if (action is null) return Fail<WishlistPreview>("ValidationFailed", 422, "Choose a supported wishlist operation.");
        if (!Allowed(actor, action)) return Denied<WishlistPreview>();
        using var c = connections.Create(); c.Open(); var item = Read(c, null, actor.OwnerId, id);
        if (item is null) return Missing<WishlistPreview>();
        if (!Eligible(item.Status, operation)) return Fail<WishlistPreview>("LifecycleLocked", 409, "The wishlist state does not allow this operation.");
        return IdentityOperationResult<WishlistPreview>.Success(new(id, item.ETag, operation, ReferenceCount(c, null, actor.OwnerId, id)));
    }
    public IdentityOperationResult<WishlistAcknowledgement> Transition(IdentityPrincipal actor, Guid id, string? etag,
        string operation, WishlistConfirmation body, string key, string? trace)
    {
        var action = Action(operation);
        if (action is null) return Fail<WishlistAcknowledgement>("ValidationFailed", 422, "Choose a supported wishlist operation.");
        return Mutate(actor, action, new { id, etag, operation, body }, key, trace, (c, tx) =>
        {
            var item = Read(c, tx, actor.OwnerId, id); if (item is null) return Missing<WishlistAcknowledgement>();
            var stale = Precondition<WishlistAcknowledgement>(item, etag); if (stale is not null) return stale;
            if (!body.Confirm) return Fail<WishlistAcknowledgement>("ConfirmationRequired", 422, "Confirm this wishlist operation.");
            if (!Eligible(item.Status, operation)) return Fail<WishlistAcknowledgement>("LifecycleLocked", 409, "The wishlist state does not allow this operation.");
            var next = operation switch
            {
                "mark-purchased" when item.Status == "Wanted" => "Purchased",
                "archive" when item.Status is "Wanted" or "Purchased" => "Archived",
                "unarchive" when item.Status == "Archived" => Previous(c, tx, actor.OwnerId, id, "PreArchiveState"),
                "trash" when item.Status != "Trash" => "Trash",
                "restore" when item.Status == "Trash" => Previous(c, tx, actor.OwnerId, id, "PreTrashState"),
                "purge" when item.Status == "Trash" => "Purged",
                _ => null
            };
            if (next is null) return Fail<WishlistAcknowledgement>("LifecycleLocked", 409, "The wishlist state does not allow this operation.");
            if (next == "Purged" && ReferenceCount(c, tx, actor.OwnerId, id) != 0)
                return Fail<WishlistAcknowledgement>("ResourcePinned", 409, "A retained reference prevents permanent deletion.");
            var batch = Guid.NewGuid();
            if (operation is "trash" or "restore" or "purge")
            {
                using var cohort = Command(c, tx, operation == "trash" ? """
                    INSERT [operations].[TrashBatch](Id,OwnerId,RootResourceId,DeletedAt,State,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                    VALUES(@Batch,@Owner,@Id,SYSUTCDATETIME(),'Trashed',SYSUTCDATETIME(),@User,@User);
                    INSERT [operations].[TrashMember](Id,OwnerId,BatchId,ResourceId,PreviousLifecycle,Depth,UpdatedAt,CreatedByUserId,UpdatedByUserId)
                    VALUES(NEWID(),@Owner,@Batch,@Id,@Previous,0,SYSUTCDATETIME(),@User,@User);
                    SELECT 1;
                    """ : """
                    DECLARE @CurrentBatch uniqueidentifier=(SELECT TrashBatchId FROM [shopping].[WishlistItem] WHERE OwnerId=@Owner AND Id=@Id);
                    IF NOT EXISTS(SELECT 1 FROM [operations].[TrashBatch] b JOIN [operations].[TrashMember] m ON m.OwnerId=b.OwnerId AND m.BatchId=b.Id
                        WHERE b.OwnerId=@Owner AND b.Id=@CurrentBatch AND b.RootResourceId=@Id AND b.State='Trashed'
                        AND m.ResourceId=@Id AND m.PreviousLifecycle=@Previous AND m.Depth=0 AND m.ParentResourceId IS NULL AND m.PurgedAt IS NULL)
                        OR (SELECT COUNT(*) FROM [operations].[TrashMember] WHERE OwnerId=@Owner AND BatchId=@CurrentBatch)<>1
                        SELECT 0;
                    ELSE BEGIN
                        UPDATE [operations].[TrashBatch] SET State=CASE WHEN @Operation='restore' THEN 'Restored' ELSE 'Purged' END,
                            RestoredAt=CASE WHEN @Operation='restore' THEN SYSUTCDATETIME() ELSE NULL END,
                            UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@CurrentBatch;
                        IF @Operation='purge' UPDATE [operations].[TrashMember] SET PurgedAt=SYSUTCDATETIME(),UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                            WHERE OwnerId=@Owner AND BatchId=@CurrentBatch AND ResourceId=@Id;
                        SELECT 1;
                    END;
                    """, actor.OwnerId);
                Add(cohort, "@Id", SqlDbType.UniqueIdentifier, id); Add(cohort, "@Batch", SqlDbType.UniqueIdentifier, batch);
                Add(cohort, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(cohort, "@Operation", SqlDbType.VarChar, operation, 64);
                Add(cohort, "@Previous", SqlDbType.VarChar, operation == "trash" ? item.Status : Previous(c, tx, actor.OwnerId, id, "PreTrashState"), 64);
                if (Convert.ToInt32(cohort.ExecuteScalar()) != 1)
                    return Fail<WishlistAcknowledgement>("LifecycleLocked", 409, "The deletion cohort is unavailable.");
            }
            using var cmd = Command(c, tx, next == "Purged" ? "DELETE [shopping].[WishlistItem] WHERE OwnerId=@Owner AND Id=@Id;" : """
                UPDATE [shopping].[WishlistItem] SET
                    PreArchiveState=CASE WHEN @Operation='archive' THEN Status WHEN @Operation='unarchive' THEN NULL ELSE PreArchiveState END,
                    PreTrashState=CASE WHEN @Operation='trash' THEN Status WHEN @Operation='restore' THEN NULL ELSE PreTrashState END,
                    TrashBatchId=CASE WHEN @Operation='trash' THEN @Batch WHEN @Operation='restore' THEN NULL ELSE TrashBatchId END,
                    Status=@Status,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id;
                """, actor.OwnerId);
            Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Operation", SqlDbType.VarChar, operation, 64);
            Add(cmd, "@Status", SqlDbType.VarChar, next, 64); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
            Add(cmd, "@Batch", SqlDbType.UniqueIdentifier, batch); cmd.ExecuteNonQuery();
            UpdateResource(c, tx, actor, id, next is "Archived" or "Trash" or "Purged" ? next : "Active");
            return IdentityOperationResult<WishlistAcknowledgement>.Success(new(id, next == "Purged" ? null : Read(c, tx, actor.OwnerId, id)!.ETag));
        });
    }
    private IdentityOperationResult<WishlistAcknowledgement> Mutate(IdentityPrincipal actor, string action, object request,
        string key, string? trace, Func<SqlConnection, SqlTransaction, IdentityOperationResult<WishlistAcknowledgement>> work)
    {
        if (!Allowed(actor, action)) return Denied<WishlistAcknowledgement>();
        if (!Guid.TryParse(key, out var parsedKey) || parsedKey == Guid.Empty)
            return Fail<WishlistAcknowledgement>("IdempotencyKeyRequired", 422, "A nonempty UUID idempotency key is required.");
        using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
        using (var owner = Command(c, tx, "SELECT Id FROM [platform].[PersonalSpace] WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;", actor.OwnerId))
        { Add(owner, "@User", SqlDbType.UniqueIdentifier, actor.UserId); if (owner.ExecuteScalar() is null) return Denied<WishlistAcknowledgement>(); }
        if (!Allowed(c, tx, actor, action) || (action == "shopping.wishlist.update" && !Allowed(c, tx, actor, "shopping.wishlist.read")))
            return Denied<WishlistAcknowledgement>();
        var claim = receipts.TryClaim(c, tx, actor.UserId, action, key, JsonSerializer.Serialize(request), DateTime.UtcNow);
        if (claim.IsConflict) return Fail<WishlistAcknowledgement>("IdempotencyConflict", 409, "This key belongs to a different request.");
        if (claim.IsReplay && claim.ResultJson is { } saved)
            return IdentityOperationResult<WishlistAcknowledgement>.Success(JsonSerializer.Deserialize<WishlistAcknowledgement>(saved)!, claim.ResultStatusCode ?? 200, claim.ResultCode ?? "Ok");
        if (!claim.IsClaimed) return Fail<WishlistAcknowledgement>("RequestInProgress", 409, "The request is already in progress.");
        var result = work(c, tx); if (!result.Succeeded || result.Value is null) return result;
        if (!Allowed(c, tx, actor, action)) return Denied<WishlistAcknowledgement>();
        using var audit = Command(c, tx, "INSERT [security].[AuditEvent](ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId) VALUES(@User,@User,@Action,'shopping.WishlistItem',@Id,'Succeeded',@Trace);", actor.OwnerId);
        Add(audit, "@User", SqlDbType.UniqueIdentifier, actor.UserId); Add(audit, "@Action", SqlDbType.NVarChar, action, 160);
        Add(audit, "@Id", SqlDbType.UniqueIdentifier, result.Value.ItemId); Add(audit, "@Trace", SqlDbType.NVarChar, trace, 100); audit.ExecuteNonQuery();
        receipts.Complete(c, tx, claim, result.Code, result.StatusCode, JsonSerializer.Serialize(result.Value)); tx.Commit(); return result;
    }
    private static string? Action(string operation) => operation switch
    {
        "mark-purchased" => "shopping.wishlist.mark_purchased", "archive" => "shopping.wishlist.archive",
        "unarchive" => "shopping.wishlist.unarchive", "trash" => "shopping.wishlist.trash",
        "restore" => "shopping.wishlist.restore", "purge" => "shopping.wishlist.purge", _ => null
    };
    private static bool Eligible(string status, string operation) => operation switch
    {
        "mark-purchased" => status == "Wanted", "archive" => status is "Wanted" or "Purchased",
        "unarchive" => status == "Archived", "trash" => status != "Trash",
        "restore" or "purge" => status == "Trash", _ => false
    };
    private static IdentityOperationResult<T>? Precondition<T>(WishlistItem item, string? etag) =>
        string.IsNullOrWhiteSpace(etag) ? Fail<T>("PreconditionRequired", 428, "If-Match is required.") :
        item.ETag == etag ? null : Fail<T>("RevisionConflict", 412, "Reload the current wishlist item.");
    private static bool Validate(WishlistCommand body, out decimal quantity, out decimal? amount, out string? currency, out string? url)
    {
        quantity = 0; amount = null; currency = body.Currency?.Trim().ToUpperInvariant(); url = string.IsNullOrWhiteSpace(body.Url) ? null : body.Url.Trim();
        if (string.IsNullOrWhiteSpace(body.Title) || body.Title.Length > 200 || body.Notes is { Length: > 20000 } ||
            !ParseDecimal(body.Quantity, out quantity) || quantity <= 0) return false;
        if (body.TargetAmount is not null)
        { if (!ParseDecimal(body.TargetAmount, out var price) || currency is null || !Currencies.Contains(currency)) return false; amount = price; }
        else if (body.Currency is not null) return false;
        return url is null || (url.Length <= 2048 && Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https" && uri.UserInfo.Length == 0);
    }
    private static bool ParseDecimal(string? text, out decimal value)
    { value = 0; return text is not null && DecimalPattern.IsMatch(text) && decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value); }
    private static void UpdateResource(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, string availability)
    {
        using var cmd = Command(c, tx, "UPDATE [platform].[Resource] SET Availability=@Availability,Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User,PurgedAt=CASE WHEN @Availability='Purged' THEN SYSUTCDATETIME() ELSE NULL END WHERE OwnerId=@Owner AND Id=@Id;", actor.OwnerId);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); Add(cmd, "@Availability", SqlDbType.VarChar, availability, 64); Add(cmd, "@User", SqlDbType.UniqueIdentifier, actor.UserId);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Wishlist registry integrity failed.");
    }
    private static int ReferenceCount(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id)
    {
        using var cmd = Command(c, tx, "SELECT COUNT(*) FROM [platform].[ResourceLink] WHERE OwnerId=@Owner AND (SourceResourceId=@Id OR TargetResourceId=@Id) AND State<>'Detached';", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Convert.ToInt32(cmd.ExecuteScalar());
    }
    private static string? Previous(SqlConnection c, SqlTransaction tx, Guid owner, Guid id, string column)
    {
        if (column is not ("PreArchiveState" or "PreTrashState")) throw new ArgumentException("Unknown lifecycle field.", nameof(column));
        using var cmd = Command(c, tx, $"SELECT {column} FROM [shopping].[WishlistItem] WHERE OwnerId=@Owner AND Id=@Id;", owner);
        Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return cmd.ExecuteScalar() as string;
    }
    private static WishlistItem? Read(SqlConnection c, SqlTransaction? tx, Guid owner, Guid id)
    { using var cmd = Command(c, tx, $"SELECT {Columns} FROM [shopping].[WishlistItem] WHERE OwnerId=@Owner AND Id=@Id;", owner); Add(cmd, "@Id", SqlDbType.UniqueIdentifier, id); return Rows(cmd).FirstOrDefault(); }
    private static List<WishlistItem> Rows(SqlCommand cmd)
    {
        using var r = cmd.ExecuteReader(); var items = new List<WishlistItem>();
        while (r.Read()) items.Add(new(r.GetGuid(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2), Format(r.GetDecimal(3)),
            r.IsDBNull(4) ? null : Format(r.GetDecimal(4)), r.IsDBNull(5) ? null : r.GetString(5), r.IsDBNull(6) ? null : r.GetString(6),
            r.GetString(7), Utc(r.GetDateTime(8)), Utc(r.GetDateTime(9)), "\"" + Convert.ToBase64String(r.GetFieldValue<byte[]>(10)) + "\""));
        return items;
    }
    private static string Format(decimal value) => value.ToString("0.########", CultureInfo.InvariantCulture);
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static SqlCommand Command(SqlConnection c, SqlTransaction? tx, string sql, Guid owner)
    { var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql; Add(cmd, "@Owner", SqlDbType.UniqueIdentifier, owner); return cmd; }
    private static void Add(SqlCommand cmd, string name, SqlDbType type, object? value, int size = 0)
    { var p = cmd.Parameters.Add(name, type); if (size != 0) p.Size = size; p.Value = value ?? DBNull.Value; }
    private static void Decimal(SqlCommand cmd, string name, decimal? value)
    { var p = cmd.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 28; p.Scale = 8; p.Value = (object?)value ?? DBNull.Value; }
}
