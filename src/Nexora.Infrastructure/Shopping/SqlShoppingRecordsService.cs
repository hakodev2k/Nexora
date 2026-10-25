using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.Shopping;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.Shopping;

/// <summary>Owner-private manual records. No payment, provider, Finance or Asset effects.</summary>
public sealed class SqlShoppingRecordsService : IShoppingRecordsService
{
    public static readonly string[] Actions = ["shopping.seller.read", "shopping.seller.create", "shopping.seller.update",
        "shopping.order.read", "shopping.order.create", "shopping.order.update",
        "shopping.warranty.read", "shopping.warranty.create", "shopping.warranty.update"];
    private readonly SqlConnectionFactory connections;
    private readonly SqlSelfCapability capabilities;
    private readonly SqlRequestReceiptStore receipts;
    public SqlShoppingRecordsService(SqlConnectionFactory connections, string secret)
    { this.connections = connections; capabilities = new(connections); receipts = new(secret); }

    private bool Allowed(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string action) =>
        SqlCurrentActor.IsLive(c, tx, actor) && capabilities.IsAllowed(c, tx, actor, "FX31", action,
            action.EndsWith(".update", StringComparison.Ordinal) ? action[..^6] + "read" : action) &&
        (!action.StartsWith("shopping.warranty.", StringComparison.Ordinal) || action.EndsWith(".read", StringComparison.Ordinal) ||
         capabilities.IsAllowed(c, tx, actor, "FX31", "shopping.order.read"));
    private static IdentityOperationResult<T> Fail<T>(string code, int status) =>
        IdentityOperationResult<T>.Failure(code, status, code switch
        {
            "ValidationFailed" => "Check the entered fields, dates, currency and exact amounts.",
            "RevisionConflict" => "The record changed. Reload its current version before saving.",
            "PreconditionRequired" => "A current strong If-Match revision is required.",
            "DuplicateOrderReference" => "This order reference already exists. Review it and explicitly confirm another record.",
            "ResourcePinned" => "A warranty still references an order line that would be removed.",
            "LifecycleLocked" => "Only Draft order metadata can be edited in this manual slice.",
            "PersistenceUnavailable" => "Persistence is temporarily unavailable.",
            _ => "The resource, action or request is unavailable."
        });
    private static bool PageValid(int page, string? query) => page is >= 1 and <= 1000 && query is not { Length: > 200 };
    private IdentityOperationResult<T> Read<T>(IdentityPrincipal actor, string action,
        Func<SqlConnection, SqlTransaction, IdentityOperationResult<T>> work)
    {
        try
        {
            using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            if (!Allowed(c, tx, actor, action)) return Fail<T>("ActionDenied", 403);
            var result = work(c, tx);
            if (!Allowed(c, tx, actor, action)) return Fail<T>("ActionDenied", 403);
            if (result.Succeeded) tx.Commit(); return result;
        }
        catch (SqlException) { return Fail<T>("PersistenceUnavailable", 503); }
    }
    public IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor)
    {
        try
        {
            using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            var values = Actions.ToDictionary(a => a, a => Allowed(c, tx, actor, a));
            tx.Commit(); return IdentityOperationResult<IReadOnlyDictionary<string, bool>>.Success(values);
        }
        catch (SqlException) { return Fail<IReadOnlyDictionary<string, bool>>("PersistenceUnavailable", 503); }
    }
    public IdentityOperationResult<ShoppingRecordPage<SellerRecord>> Sellers(IdentityPrincipal actor, int page, string? query) =>
        Read(actor, "shopping.seller.read", (c, tx) =>
        {
            if (!PageValid(page, query)) return Fail<ShoppingRecordPage<SellerRecord>>("ValidationFailed", 422);
            var canCount = Allowed(c, tx, actor, "shopping.order.read");
            using var cmd = Cmd(c, tx, actor, """
                SELECT s.Id,s.Title,s.Url,s.RowVersion,CASE WHEN @Count=1 THEN
                  (SELECT COUNT(*) FROM shopping.PurchaseOrder o JOIN platform.Resource r ON r.OwnerId=o.OwnerId AND r.Id=o.Id
                   WHERE o.OwnerId=@Owner AND o.SellerId=s.Id AND r.Availability='Active') ELSE NULL END
                FROM shopping.Seller s JOIN platform.Resource r ON r.OwnerId=s.OwnerId AND r.Id=s.Id
                WHERE s.OwnerId=@Owner AND s.MergedIntoId IS NULL AND r.Availability='Active'
                 AND (@Query IS NULL OR CHARINDEX(@Query,s.Title)>0)
                ORDER BY s.Title,s.Id OFFSET @Skip ROWS FETCH NEXT 26 ROWS ONLY;
                """, ("@Count", canCount), ("@Query", Query(query)), ("@Skip", (page - 1) * 25));
            var rows = new List<SellerRecord>();
            using (var r = cmd.ExecuteReader()) while (r.Read()) rows.Add(new(r.GetGuid(0), r.GetString(1), Str(r, 2),
                r.IsDBNull(4) ? null : r.GetInt32(4), Tag(r, 3)));
            return IdentityOperationResult<ShoppingRecordPage<SellerRecord>>.Success(new(rows.Take(25).ToArray(), page, rows.Count > 25));
        });
    public IdentityOperationResult<SellerRecord> Seller(IdentityPrincipal actor, Guid id) => Read(actor, "shopping.seller.read", (c, tx) =>
    {
        var row = SellerRow(c, tx, actor, id);
        return row is null ? Fail<SellerRecord>("ResourceUnavailable", 404) : IdentityOperationResult<SellerRecord>.Success(row);
    });
    public IdentityOperationResult<ShoppingWriteAck> SaveSeller(IdentityPrincipal actor, Guid? id, string? etag, SellerWrite body, string key, string? trace) =>
        Write(actor, "seller", id, etag, body, key, trace, (c, tx, target) =>
        {
            if (!ShoppingRecordRules.ValidSeller(body)) return Fail<ShoppingWriteAck>("ValidationFailed", 422);
            if (id is not null)
            {
                var current = SellerRow(c, tx, actor, target);
                if (current is null) return Fail<ShoppingWriteAck>("ResourceUnavailable", 404);
                var precondition = Match(current.ETag, etag); if (precondition is not null) return precondition;
            }
            else if (!Register(c, tx, actor, target, "Seller")) return Fail<ShoppingWriteAck>("DependencyUnavailable", 409);
            using var cmd = Cmd(c, tx, actor, id is null ? """
                INSERT shopping.Seller(Id,OwnerId,Title,Url,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Title,@Url,@User,@User);
                """ : """
                UPDATE shopping.Seller SET Title=@Title,Url=@Url,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                WHERE OwnerId=@Owner AND Id=@Id;
                """, ("@Id", target), ("@Title", body.Title.Trim()), ("@Url", Empty(body.Url)));
            cmd.ExecuteNonQuery(); return IdentityOperationResult<ShoppingWriteAck>.Success(new(target));
        });

    public IdentityOperationResult<ShoppingRecordPage<OrderSummary>> Orders(IdentityPrincipal actor, int page, string? query) =>
        Read(actor, "shopping.order.read", (c, tx) =>
        {
            if (!PageValid(page, query)) return Fail<ShoppingRecordPage<OrderSummary>>("ValidationFailed", 422);
            using var cmd = Cmd(c, tx, actor, """
                SELECT o.Id,o.OrderNumber,o.SellerTitle,o.PurchasedOn,o.Currency,o.Status,o.TotalAmount,o.RowVersion
                FROM shopping.PurchaseOrder o JOIN platform.Resource r ON r.OwnerId=o.OwnerId AND r.Id=o.Id
                WHERE o.OwnerId=@Owner AND r.Availability='Active'
                 AND (@Query IS NULL OR CHARINDEX(@Query,COALESCE(o.OrderNumber,N''))>0 OR CHARINDEX(@Query,COALESCE(o.SellerTitle,N''))>0)
                ORDER BY o.UpdatedAt DESC,o.Id DESC OFFSET @Skip ROWS FETCH NEXT 26 ROWS ONLY;
                """, ("@Query", Query(query)), ("@Skip", (page - 1) * 25));
            var rows = new List<OrderSummary>();
            using (var r = cmd.ExecuteReader()) while (r.Read()) rows.Add(new(r.GetGuid(0), Str(r, 1), Str(r, 2), Day(r, 3)!,
                r.GetString(4), r.GetString(5), ShoppingRecordRules.Exact(r.GetDecimal(6)), Tag(r, 7)));
            return IdentityOperationResult<ShoppingRecordPage<OrderSummary>>.Success(new(rows.Take(25).ToArray(), page, rows.Count > 25));
        });
    public IdentityOperationResult<OrderRecord> Order(IdentityPrincipal actor, Guid id) => Read(actor, "shopping.order.read", (c, tx) =>
    {
        var row = OrderRow(c, tx, actor, id);
        return row is null ? Fail<OrderRecord>("ResourceUnavailable", 404) : IdentityOperationResult<OrderRecord>.Success(row);
    });
    public IdentityOperationResult<ShoppingWriteAck> SaveOrder(IdentityPrincipal actor, Guid? id, string? etag, OrderWrite body, string key, string? trace) =>
        Write(actor, "order", id, etag, body, key, trace, (c, tx, target) =>
        {
            if (!ShoppingRecordRules.TryTotal(body, out var total)) return Fail<ShoppingWriteAck>("ValidationFailed", 422);
            var current = id is null ? null : OrderRow(c, tx, actor, target);
            if (id is not null)
            {
                if (current is null) return Fail<ShoppingWriteAck>("ResourceUnavailable", 404);
                var precondition = Match(current.ETag, etag); if (precondition is not null) return precondition;
                if (current.Status != "Draft") return Fail<ShoppingWriteAck>("LifecycleLocked", 409);
            }
            string? sellerTitle = null;
            if (body.SellerId is { } sellerId)
            {
                if (!Allowed(c, tx, actor, "shopping.seller.read")) return Fail<ShoppingWriteAck>("ActionDenied", 403);
                var seller = SellerRow(c, tx, actor, sellerId);
                if (seller is null) return Fail<ShoppingWriteAck>("ResourceUnavailable", 404);
                sellerTitle = current?.SellerId == sellerId ? current.SellerTitle : seller.Title;
            }
            if (!body.ConfirmDuplicateReference && Empty(body.OrderNumber) is { } orderNumber)
            {
                using var duplicate = Cmd(c, tx, actor, """
                    SELECT TOP(1) o.Id FROM shopping.PurchaseOrder o JOIN platform.Resource r ON r.OwnerId=o.OwnerId AND r.Id=o.Id
                    WHERE o.OwnerId=@Owner AND o.Id<>@Id AND o.OrderNumber=@Number AND r.Availability='Active';
                    """, ("@Id", target), ("@Number", orderNumber));
                if (duplicate.ExecuteScalar() is not null) return Fail<ShoppingWriteAck>("DuplicateOrderReference", 409);
            }
            var retained = body.Lines.Where(l => l.Id is not null).Select(l => l.Id!.Value).ToHashSet();
            var previous = current?.Lines ?? Array.Empty<OrderLineRecord>();
            if (retained.Any(line => previous.All(old => old.Id != line))) return Fail<ShoppingWriteAck>("ResourceUnavailable", 404);
            foreach (var removed in previous.Where(l => !retained.Contains(l.Id)))
            {
                using var pinned = Cmd(c, tx, actor, "SELECT TOP(1) Id FROM shopping.Warranty WHERE OwnerId=@Owner AND OrderLineId=@Line;", ("@Line", removed.Id));
                if (pinned.ExecuteScalar() is not null) return Fail<ShoppingWriteAck>("ResourcePinned", 409);
            }
            if (id is null && !Register(c, tx, actor, target, "PurchaseOrder")) return Fail<ShoppingWriteAck>("DependencyUnavailable", 409);
            ShoppingRecordRules.Date(body.PurchasedOn, out var date);
            using (var cmd = Cmd(c, tx, actor, id is null ? """
                INSERT shopping.PurchaseOrder(Id,OwnerId,SellerId,SellerTitle,OrderNumber,PurchasedOn,Currency,ShippingAmount,TaxAmount,DiscountAmount,TotalAmount,Notes,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Seller,@SellerTitle,@Number,@Date,@Currency,@Shipping,@Tax,@Discount,@Total,@Notes,@User,@User);
                """ : """
                UPDATE shopping.PurchaseOrder SET SellerId=@Seller,SellerTitle=@SellerTitle,OrderNumber=@Number,PurchasedOn=@Date,Currency=@Currency,
                 ShippingAmount=@Shipping,TaxAmount=@Tax,DiscountAmount=@Discount,TotalAmount=@Total,Notes=@Notes,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                WHERE OwnerId=@Owner AND Id=@Id;
                """, ("@Id", target), ("@Seller", body.SellerId), ("@SellerTitle", sellerTitle), ("@Number", Empty(body.OrderNumber)),
                ("@Date", date.ToDateTime(TimeOnly.MinValue)), ("@Currency", body.Currency), ("@Shipping", Number(body.ShippingAmount)),
                ("@Tax", Number(body.TaxAmount)), ("@Discount", Number(body.DiscountAmount)), ("@Total", total), ("@Notes", Empty(body.Notes)))) cmd.ExecuteNonQuery();
            foreach (var removed in previous.Where(l => !retained.Contains(l.Id)))
            {
                using var cmd = Cmd(c, tx, actor, "DELETE shopping.OrderLine WHERE OwnerId=@Owner AND OrderId=@Order AND Id=@Id;", ("@Order", target), ("@Id", removed.Id));
                cmd.ExecuteNonQuery();
            }
            for (var position = 0; position < body.Lines.Count; position++)
            {
                var line = body.Lines[position]; var lineId = line.Id ?? Guid.NewGuid();
                using var cmd = Cmd(c, tx, actor, line.Id is null ? """
                    INSERT shopping.OrderLine(Id,OwnerId,OrderId,Position,Title,Quantity,UnitAmount,Variant,CreatedByUserId,UpdatedByUserId)
                    VALUES(@Id,@Owner,@Order,@Position,@Title,@Quantity,@Unit,@Variant,@User,@User);
                    """ : """
                    UPDATE shopping.OrderLine SET Position=@Position,Title=@Title,Quantity=@Quantity,UnitAmount=@Unit,Variant=@Variant,
                     UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND OrderId=@Order AND Id=@Id;
                    """, ("@Id", lineId), ("@Order", target), ("@Position", position), ("@Title", line.Title.Trim()),
                    ("@Quantity", Number(line.Quantity)), ("@Unit", Number(line.UnitAmount)), ("@Variant", Empty(line.Variant)));
                cmd.ExecuteNonQuery();
            }
            if (body.SellerId is not null && !Allowed(c, tx, actor, "shopping.seller.read")) return Fail<ShoppingWriteAck>("ActionDenied", 403);
            return IdentityOperationResult<ShoppingWriteAck>.Success(new(target));
        });

    public IdentityOperationResult<ShoppingRecordPage<WarrantyRecord>> Warranties(IdentityPrincipal actor, int page, string? query) =>
        Read(actor, "shopping.warranty.read", (c, tx) =>
        {
            if (!PageValid(page, query)) return Fail<ShoppingRecordPage<WarrantyRecord>>("ValidationFailed", 422);
            using var cmd = Cmd(c, tx, actor, """
                SELECT w.Id,w.OrderLineId,w.Title,w.CoverageKind,w.StartsOn,w.EndsOn,w.Provider,w.Terms,w.ClaimContact,w.RowVersion
                FROM shopping.Warranty w JOIN platform.Resource r ON r.OwnerId=w.OwnerId AND r.Id=w.Id
                WHERE w.OwnerId=@Owner AND r.Availability='Active' AND (@Query IS NULL OR CHARINDEX(@Query,w.Title)>0)
                ORDER BY CASE WHEN w.EndsOn IS NULL THEN 1 ELSE 0 END,w.EndsOn,w.Id OFFSET @Skip ROWS FETCH NEXT 26 ROWS ONLY;
                """, ("@Query", Query(query)), ("@Skip", (page - 1) * 25));
            var rows = new List<WarrantyRecord>(); using (var r = cmd.ExecuteReader()) while (r.Read()) rows.Add(WarrantyFrom(r));
            return IdentityOperationResult<ShoppingRecordPage<WarrantyRecord>>.Success(new(rows.Take(25).ToArray(), page, rows.Count > 25));
        });
    public IdentityOperationResult<WarrantyRecord> Warranty(IdentityPrincipal actor, Guid id) => Read(actor, "shopping.warranty.read", (c, tx) =>
    {
        var row = WarrantyRow(c, tx, actor, id);
        return row is null ? Fail<WarrantyRecord>("ResourceUnavailable", 404) : IdentityOperationResult<WarrantyRecord>.Success(row);
    });
    public IdentityOperationResult<ShoppingWriteAck> SaveWarranty(IdentityPrincipal actor, Guid? id, string? etag, WarrantyWrite body, string key, string? trace) =>
        Write(actor, "warranty", id, etag, body, key, trace, (c, tx, target) =>
        {
            if (!ShoppingRecordRules.ValidWarranty(body)) return Fail<ShoppingWriteAck>("ValidationFailed", 422);
            if (!Allowed(c, tx, actor, "shopping.order.read")) return Fail<ShoppingWriteAck>("ActionDenied", 403);
            using (var source = Cmd(c, tx, actor, """
                SELECT l.Id FROM shopping.OrderLine l JOIN shopping.PurchaseOrder o ON o.OwnerId=l.OwnerId AND o.Id=l.OrderId
                JOIN platform.Resource r ON r.OwnerId=o.OwnerId AND r.Id=o.Id
                WHERE l.OwnerId=@Owner AND l.Id=@Line AND r.Availability='Active';
                """, ("@Line", body.OrderLineId))) if (source.ExecuteScalar() is null) return Fail<ShoppingWriteAck>("ResourceUnavailable", 404);
            if (id is not null)
            {
                var current = WarrantyRow(c, tx, actor, target);
                if (current is null) return Fail<ShoppingWriteAck>("ResourceUnavailable", 404);
                var precondition = Match(current.ETag, etag); if (precondition is not null) return precondition;
            }
            else if (!Register(c, tx, actor, target, "Warranty")) return Fail<ShoppingWriteAck>("DependencyUnavailable", 409);
            using var cmd = Cmd(c, tx, actor, id is null ? """
                INSERT shopping.Warranty(Id,OwnerId,OrderLineId,Title,CoverageKind,StartsOn,EndsOn,Provider,Terms,ClaimContact,CreatedByUserId,UpdatedByUserId)
                VALUES(@Id,@Owner,@Line,@Title,@Kind,@Start,@End,@Provider,@Terms,@Contact,@User,@User);
                """ : """
                UPDATE shopping.Warranty SET OrderLineId=@Line,Title=@Title,CoverageKind=@Kind,StartsOn=@Start,EndsOn=@End,
                 Provider=@Provider,Terms=@Terms,ClaimContact=@Contact,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User
                WHERE OwnerId=@Owner AND Id=@Id;
                """, ("@Id", target), ("@Line", body.OrderLineId), ("@Title", body.Title.Trim()), ("@Kind", body.CoverageKind),
                ("@Start", DateValue(body.StartsOn)), ("@End", DateValue(body.EndsOn)), ("@Provider", Empty(body.Provider)),
                ("@Terms", Empty(body.Terms)), ("@Contact", Empty(body.ClaimContact)));
            cmd.ExecuteNonQuery();
            if (!Allowed(c, tx, actor, "shopping.order.read")) return Fail<ShoppingWriteAck>("ActionDenied", 403);
            return IdentityOperationResult<ShoppingWriteAck>.Success(new(target));
        });

    private sealed record Receipt(int Version, Guid OwnerId, string Kind, Guid Id, string Action);
    private IdentityOperationResult<ShoppingWriteAck> Write(IdentityPrincipal actor, string kind, Guid? id, string? etag,
        object body, string key, string? trace, Func<SqlConnection, SqlTransaction, Guid, IdentityOperationResult<ShoppingWriteAck>> work)
    {
        if (!Guid.TryParseExact(key, "D", out var keyId) || keyId == Guid.Empty) return Fail<ShoppingWriteAck>("InvalidIdempotencyKey", 422);
        if (id is not null && !ValidTag(etag)) return Fail<ShoppingWriteAck>("PreconditionRequired", 428);
        var action = "shopping." + kind + (id is null ? ".create" : ".update");
        try
        {
            using var c = connections.Create(); c.Open(); using var tx = c.BeginTransaction(IsolationLevel.Serializable);
            using (var owner = Cmd(c, tx, actor, "SELECT Id FROM platform.PersonalSpace WITH(UPDLOCK,HOLDLOCK) WHERE Id=@Owner AND UserId=@User;"))
                if (owner.ExecuteScalar() is null) return Fail<ShoppingWriteAck>("ActionDenied", 403);
            if (!Allowed(c, tx, actor, action)) return Fail<ShoppingWriteAck>("ActionDenied", 403);
            if ((body is SellerWrite seller && !ShoppingRecordRules.ValidSeller(seller)) ||
                (body is OrderWrite order && !ShoppingRecordRules.TryTotal(order, out _)) ||
                (body is WarrantyWrite warranty && !ShoppingRecordRules.ValidWarranty(warranty)))
                return Fail<ShoppingWriteAck>("ValidationFailed", 422);
            var sourceFailure = SourceFailure(c, tx, actor, body); if (sourceFailure is not null) return sourceFailure;
            var claim = receipts.TryClaim(c, tx, actor.UserId, action, key,
                JsonSerializer.Serialize(new { Version = 1, actor.OwnerId, Id = id, ETag = etag, Body = body }), DateTime.UtcNow);
            var status = id is null ? 201 : 200;
            if (claim.IsReplay)
            {
                Receipt? saved = null; try { saved = JsonSerializer.Deserialize<Receipt>(claim.ResultJson ?? "null"); } catch (JsonException) { }
                if (saved is null || saved.Version != 1 || saved.OwnerId != actor.OwnerId || saved.Kind != kind || saved.Action != action ||
                    saved.Id == Guid.Empty || (id is not null && saved.Id != id) || claim.ResultCode != "ShoppingCommitted" || claim.ResultStatusCode != status)
                    return Fail<ShoppingWriteAck>("IdempotencyConflict", 409);
                if (!Allowed(c, tx, actor, action)) return Fail<ShoppingWriteAck>("ActionDenied", 403);
                sourceFailure = SourceFailure(c, tx, actor, body); if (sourceFailure is not null) return sourceFailure;
                tx.Commit(); return IdentityOperationResult<ShoppingWriteAck>.Success(new(saved.Id), status, "ShoppingCommitted");
            }
            if (claim.IsConflict) return Fail<ShoppingWriteAck>("IdempotencyConflict", 409);
            if (!claim.IsClaimed) return Fail<ShoppingWriteAck>("RequestInProgress", 409);
            var target = id ?? Guid.NewGuid();
            var result = work(c, tx, target); if (!result.Succeeded) return result;
            if (!Allowed(c, tx, actor, action)) return Fail<ShoppingWriteAck>("ActionDenied", 403);
            sourceFailure = SourceFailure(c, tx, actor, body); if (sourceFailure is not null) return sourceFailure;
            using (var registry = Cmd(c, tx, actor, "UPDATE platform.Resource SET Revision=Revision+1,UpdatedAt=SYSUTCDATETIME(),UpdatedByUserId=@User WHERE OwnerId=@Owner AND Id=@Id AND Availability='Active';", ("@Id", target)))
                if (registry.ExecuteNonQuery() != 1) return Fail<ShoppingWriteAck>("ResourceUnavailable", 404);
            using (var audit = Cmd(c, tx, actor, """
                INSERT security.AuditEvent(ActorUserId,OwnerUserId,ActionKey,TargetType,TargetId,Result,TraceId)
                VALUES(@User,@User,@Action,@Type,@Id,'Succeeded',@Trace);
                """, ("@Action", action), ("@Type", "shopping." + kind), ("@Id", target), ("@Trace", trace is { Length: > 100 } ? trace[..100] : trace))) audit.ExecuteNonQuery();
            receipts.Complete(c, tx, claim, "ShoppingCommitted", status, JsonSerializer.Serialize(new Receipt(1, actor.OwnerId, kind, target, action)));
            tx.Commit(); return IdentityOperationResult<ShoppingWriteAck>.Success(new(target), status, "ShoppingCommitted");
        }
        catch (SqlException) { return Fail<ShoppingWriteAck>("PersistenceUnavailable", 503); }
    }
    private IdentityOperationResult<ShoppingWriteAck>? SourceFailure(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, object body)
    {
        if (body is OrderWrite { SellerId: { } sellerId })
        {
            if (!Allowed(c, tx, actor, "shopping.seller.read")) return Fail<ShoppingWriteAck>("ActionDenied", 403);
            if (SellerRow(c, tx, actor, sellerId) is null) return Fail<ShoppingWriteAck>("ResourceUnavailable", 404);
        }
        if (body is WarrantyWrite warranty)
        {
            if (!Allowed(c, tx, actor, "shopping.order.read")) return Fail<ShoppingWriteAck>("ActionDenied", 403);
            using var cmd = Cmd(c, tx, actor, """
                SELECT l.Id FROM shopping.OrderLine l JOIN shopping.PurchaseOrder o ON o.OwnerId=l.OwnerId AND o.Id=l.OrderId
                JOIN platform.Resource r ON r.OwnerId=o.OwnerId AND r.Id=o.Id
                WHERE l.OwnerId=@Owner AND l.Id=@Line AND r.Availability='Active';
                """, ("@Line", warranty.OrderLineId));
            if (cmd.ExecuteScalar() is null) return Fail<ShoppingWriteAck>("ResourceUnavailable", 404);
        }
        return null;
    }
    private static IdentityOperationResult<ShoppingWriteAck>? Match(string current, string? supplied) =>
        !ValidTag(supplied) ? Fail<ShoppingWriteAck>("PreconditionRequired", 428) : current != supplied ? Fail<ShoppingWriteAck>("RevisionConflict", 412) : null;
    private static bool ValidTag(string? value)
    {
        if (value is null || value.Length != 14 || value[0] != '"' || value[^1] != '"') return false;
        try { var bytes = Convert.FromBase64String(value[1..^1]); return bytes.Length == 8 && "\"" + Convert.ToBase64String(bytes) + "\"" == value; }
        catch (FormatException) { return false; }
    }
    private static bool Register(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id, string type)
    {
        using var cmd = Cmd(c, tx, actor, """
            INSERT platform.Resource(Id,OwnerId,ResourceTypeId,Availability,Revision,UpdatedAt,CreatedByUserId,UpdatedByUserId)
            SELECT @Id,@Owner,rt.Id,'Active',1,SYSUTCDATETIME(),@User,@User FROM platform.ResourceType rt
            JOIN platform.Module m ON m.Id=rt.ModuleId WHERE m.Code='FX31' AND rt.Code=@Type AND rt.ContractVersion='manual-records-v1';
            """, ("@Id", id), ("@Type", type));
        return cmd.ExecuteNonQuery() == 1;
    }
    private static SellerRecord? SellerRow(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id)
    {
        using var cmd = Cmd(c, tx, actor, """
            SELECT s.Id,s.Title,s.Url,s.RowVersion FROM shopping.Seller s JOIN platform.Resource r ON r.OwnerId=s.OwnerId AND r.Id=s.Id
            WHERE s.OwnerId=@Owner AND s.Id=@Id AND s.MergedIntoId IS NULL AND r.Availability='Active';
            """, ("@Id", id));
        using var r = cmd.ExecuteReader(); return r.Read() ? new(r.GetGuid(0), r.GetString(1), Str(r, 2), null, Tag(r, 3)) : null;
    }
    private static OrderRecord? OrderRow(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id)
    {
        using var cmd = Cmd(c, tx, actor, """
            SELECT o.Id,o.SellerId,o.SellerTitle,o.OrderNumber,o.PurchasedOn,o.Currency,o.Status,o.ShippingAmount,o.TaxAmount,o.DiscountAmount,o.TotalAmount,o.Notes,o.RowVersion
            FROM shopping.PurchaseOrder o JOIN platform.Resource r ON r.OwnerId=o.OwnerId AND r.Id=o.Id
            WHERE o.OwnerId=@Owner AND o.Id=@Id AND r.Availability='Active';
            """, ("@Id", id));
        OrderRecord? row;
        using (var r = cmd.ExecuteReader()) row = r.Read() ? new(r.GetGuid(0), r.IsDBNull(1) ? null : r.GetGuid(1), Str(r, 2), Str(r, 3), Day(r, 4)!,
            r.GetString(5), r.GetString(6), ShoppingRecordRules.Exact(r.GetDecimal(7)), ShoppingRecordRules.Exact(r.GetDecimal(8)),
            ShoppingRecordRules.Exact(r.GetDecimal(9)), ShoppingRecordRules.Exact(r.GetDecimal(10)), Str(r, 11), Array.Empty<OrderLineRecord>(), Tag(r, 12)) : null;
        if (row is null) return null;
        using var lines = Cmd(c, tx, actor, "SELECT Id,Title,Quantity,UnitAmount,Variant FROM shopping.OrderLine WHERE OwnerId=@Owner AND OrderId=@Id ORDER BY Position,Id;", ("@Id", id));
        var items = new List<OrderLineRecord>();
        using (var r = lines.ExecuteReader()) while (r.Read()) items.Add(new(r.GetGuid(0), r.GetString(1), ShoppingRecordRules.Exact(r.GetDecimal(2)), ShoppingRecordRules.Exact(r.GetDecimal(3)), Str(r, 4)));
        return row with { Lines = items };
    }
    private static WarrantyRecord? WarrantyRow(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, Guid id)
    {
        using var cmd = Cmd(c, tx, actor, """
            SELECT w.Id,w.OrderLineId,w.Title,w.CoverageKind,w.StartsOn,w.EndsOn,w.Provider,w.Terms,w.ClaimContact,w.RowVersion
            FROM shopping.Warranty w JOIN platform.Resource r ON r.OwnerId=w.OwnerId AND r.Id=w.Id
            WHERE w.OwnerId=@Owner AND w.Id=@Id AND r.Availability='Active';
            """, ("@Id", id));
        using var r = cmd.ExecuteReader(); return r.Read() ? WarrantyFrom(r) : null;
    }
    private static WarrantyRecord WarrantyFrom(SqlDataReader r) => new(r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetString(3),
        Day(r, 4), Day(r, 5), Str(r, 6), Str(r, 7), Str(r, 8), Tag(r, 9));
    private static string? Str(SqlDataReader r, int index) => r.IsDBNull(index) ? null : r.GetString(index);
    private static string Tag(SqlDataReader r, int index) => "\"" + Convert.ToBase64String(r.GetFieldValue<byte[]>(index)) + "\"";
    private static string? Day(SqlDataReader r, int index) => r.IsDBNull(index) ? null : DateOnly.FromDateTime(r.GetDateTime(index)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static decimal Number(string text) { ShoppingRecordRules.TryDecimal(text, out var value); return value; }
    private static object? DateValue(string? text) => ShoppingRecordRules.Date(text, out var day) ? day.ToDateTime(TimeOnly.MinValue) : null;
    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? Query(string? value) => Empty(value);
    private static SqlCommand Cmd(SqlConnection c, SqlTransaction tx, IdentityPrincipal actor, string sql, params (string Key, object? Value)[] values)
    {
        var cmd = c.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = sql;
        Add(cmd, "@Owner", actor.OwnerId); Add(cmd, "@User", actor.UserId);
        foreach (var (key, value) in values) Add(cmd, key, value); return cmd;
    }
    private static void Add(SqlCommand cmd, string key, object? value)
    {
        var p = value switch
        {
            Guid => cmd.Parameters.Add(key, SqlDbType.UniqueIdentifier),
            int => cmd.Parameters.Add(key, SqlDbType.Int),
            bool => cmd.Parameters.Add(key, SqlDbType.Bit),
            decimal => cmd.Parameters.Add(key, SqlDbType.Decimal),
            DateTime => cmd.Parameters.Add(key, SqlDbType.Date),
            _ => cmd.Parameters.Add(key, SqlDbType.NVarChar, -1)
        };
        if (value is decimal) { p.Precision = 28; p.Scale = 8; }
        p.Value = value ?? DBNull.Value;
    }
}
