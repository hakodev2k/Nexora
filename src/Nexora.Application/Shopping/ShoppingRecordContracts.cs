using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Nexora.Application.Identity;

namespace Nexora.Application.Shopping;

public sealed record ShoppingRecordPage<T>(IReadOnlyList<T> Items, int Page, bool HasMore);
public sealed record ShoppingWriteAck(Guid Id, bool Committed = true, bool DetailAvailable = false);
public sealed record SellerWrite(string Title, string? Url = null);
public sealed record SellerRecord(Guid Id, string Title, string? Url, int? LinkedOrderCount, string ETag);
public sealed record OrderLineWrite(Guid? Id, string Title, string Quantity, string UnitAmount, string? Variant = null);
public sealed record OrderWrite(Guid? SellerId, string? OrderNumber, string PurchasedOn, string Currency,
    string ShippingAmount, string TaxAmount, string DiscountAmount, IReadOnlyList<OrderLineWrite> Lines,
    string? Notes = null, bool ConfirmDuplicateReference = false);
public sealed record OrderLineRecord(Guid Id, string Title, string Quantity, string UnitAmount, string? Variant);
public sealed record OrderSummary(Guid Id, string? OrderNumber, string? SellerTitle, string PurchasedOn,
    string Currency, string Status, string TotalAmount, string ETag);
public sealed record OrderRecord(Guid Id, Guid? SellerId, string? SellerTitle, string? OrderNumber,
    string PurchasedOn, string Currency, string Status, string ShippingAmount, string TaxAmount,
    string DiscountAmount, string TotalAmount, string? Notes, IReadOnlyList<OrderLineRecord> Lines, string ETag);
public sealed record WarrantyWrite(Guid OrderLineId, string Title, string CoverageKind, string? StartsOn = null,
    string? EndsOn = null, string? Provider = null, string? Terms = null, string? ClaimContact = null);
public sealed record WarrantyRecord(Guid Id, Guid OrderLineId, string Title, string CoverageKind, string? StartsOn,
    string? EndsOn, string? Provider, string? Terms, string? ClaimContact, string ETag);

public interface IShoppingRecordsService
{
    IdentityOperationResult<IReadOnlyDictionary<string, bool>> Capabilities(IdentityPrincipal actor);
    IdentityOperationResult<ShoppingRecordPage<SellerRecord>> Sellers(IdentityPrincipal actor, int page, string? query);
    IdentityOperationResult<SellerRecord> Seller(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<ShoppingWriteAck> SaveSeller(IdentityPrincipal actor, Guid? id, string? etag, SellerWrite body, string key, string? trace);
    IdentityOperationResult<ShoppingRecordPage<OrderSummary>> Orders(IdentityPrincipal actor, int page, string? query);
    IdentityOperationResult<OrderRecord> Order(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<ShoppingWriteAck> SaveOrder(IdentityPrincipal actor, Guid? id, string? etag, OrderWrite body, string key, string? trace);
    IdentityOperationResult<ShoppingRecordPage<WarrantyRecord>> Warranties(IdentityPrincipal actor, int page, string? query);
    IdentityOperationResult<WarrantyRecord> Warranty(IdentityPrincipal actor, Guid id);
    IdentityOperationResult<ShoppingWriteAck> SaveWarranty(IdentityPrincipal actor, Guid? id, string? etag, WarrantyWrite body, string key, string? trace);
}

/// <summary>Pure validation of the approved manual subset; never performs FX/provider operations.</summary>
public static class ShoppingRecordRules
{
    private static readonly Regex DecimalPattern = new("^[0-9]{1,20}(?:\\.[0-9]{1,8})?$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    private static readonly HashSet<string> Currencies = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
        .Select(c => new RegionInfo(c.Name).ISOCurrencySymbol).ToHashSet(StringComparer.Ordinal);
    private const decimal Maximum = 99999999999999999999.99999999m;
    public static bool ValidSeller(SellerWrite body) => body is not null && Text(body.Title, 200, true) &&
        (string.IsNullOrWhiteSpace(body.Url) || (body.Url.Length <= 2048 && !body.Url.Any(char.IsControl) &&
        Uri.TryCreate(body.Url.Trim(), UriKind.Absolute, out var url) && url.Scheme is "http" or "https" && url.UserInfo.Length == 0));
    public static bool TryDecimal(string? text, out decimal value)
    {
        value = 0;
        return text is not null && DecimalPattern.IsMatch(text) &&
            decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value) && value <= Maximum;
    }
    public static bool Date(string? text, out DateOnly value) =>
        DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    public static string Exact(decimal value) => value.ToString("0.########", CultureInfo.InvariantCulture);
    public static bool Text(string? value, int max, bool required = false) =>
        (!required || !string.IsNullOrWhiteSpace(value)) && (value is null || value.Length <= max);
    public static bool TryTotal(OrderWrite body, out decimal total)
    {
        total = 0;
        if (body is null || !Text(body.OrderNumber, 200) || !Text(body.Notes, 20000) || !Date(body.PurchasedOn, out _) ||
            body.Currency is null || !Currencies.Contains(body.Currency) || body.Lines is null || body.Lines.Count is < 1 or > 100 ||
            !TryDecimal(body.ShippingAmount, out var shipping) || !TryDecimal(body.TaxAmount, out var tax) ||
            !TryDecimal(body.DiscountAmount, out var discount)) return false;
        var ids = body.Lines.Where(l => l?.Id is not null).Select(l => l.Id).ToArray();
        if (ids.Distinct().Count() != ids.Length || ids.Any(id => id == Guid.Empty)) return false;
        var totalUnits = BigInteger.Zero;
        foreach (var line in body.Lines)
        {
            if (line is null || !Text(line.Title, 200, true) || !Text(line.Variant, 300) ||
                !TryDecimal(line.Quantity, out var quantity) || quantity <= 0 || !TryDecimal(line.UnitAmount, out _)) return false;
            // Integer arithmetic detects precision loss before .NET decimal multiplication
            // could round a high-significance product on the caller's behalf.
            var amountUnits = BigInteger.DivRem(Units(line.Quantity) * Units(line.UnitAmount), 100000000, out var remainder);
            if (remainder != 0) return false;
            totalUnits += amountUnits;
        }
        totalUnits += Units(body.ShippingAmount) + Units(body.TaxAmount) - Units(body.DiscountAmount);
        if (totalUnits < 0 || totalUnits > BigInteger.Parse("9999999999999999999999999999", CultureInfo.InvariantCulture)) return false;
        total = (decimal)totalUnits / 100000000m;
        return true;
    }
    private static BigInteger Units(string value)
    {
        var parts = value.Split('.');
        return BigInteger.Parse(parts[0] + (parts.Length == 2 ? parts[1] : "").PadRight(8, '0'), CultureInfo.InvariantCulture);
    }
    public static bool ValidWarranty(WarrantyWrite body)
    {
        if (body is null || body.OrderLineId == Guid.Empty || !Text(body.Title, 200, true) ||
            !Text(body.Provider, 200) || !Text(body.Terms, 20000) || !Text(body.ClaimContact, 1000)) return false;
        return body.CoverageKind switch
        {
            "Fixed" => Date(body.StartsOn, out var start) && Date(body.EndsOn, out var end) && end >= start,
            "Lifetime" or "Unknown" => body.StartsOn is null && body.EndsOn is null,
            _ => false
        };
    }
}
