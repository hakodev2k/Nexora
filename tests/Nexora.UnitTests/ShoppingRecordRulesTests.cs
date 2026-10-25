using Nexora.Application.Shopping;
using Xunit;

namespace Nexora.UnitTests;

public sealed class ShoppingRecordRulesTests
{
    private static OrderWrite Order(string quantity = "2", string unit = "12.5") =>
        new(null, null, "2026-10-25", "USD", "1", "2", "3", [new(null, "Synthetic item", quantity, unit)]);
    [Fact]
    public void Totals_are_exact_explicit_currency_manual_amounts()
    {
        Assert.True(ShoppingRecordRules.TryTotal(Order(), out var total)); Assert.Equal(25m, total);
        Assert.True(ShoppingRecordRules.TryTotal(Order("0.1", "0.0000001") with { ShippingAmount = "0", TaxAmount = "0", DiscountAmount = "0" }, out total));
        Assert.Equal(0.00000001m, total);
    }
    [Theory]
    [InlineData("0", "1")]
    [InlineData("-1", "1")]
    [InlineData("1e2", "1")]
    [InlineData("1,2", "1")]
    [InlineData("1.000000001", "1")]
    [InlineData("99999999999999999999", "99999999999999999999")]
    [InlineData("0.1", "0.00000001")]
    [InlineData("99999999999999999999.99999999", "0.1")]
    public void Invalid_or_unrepresentable_line_amounts_fail_without_rounding(string quantity, string unit) =>
        Assert.False(ShoppingRecordRules.TryTotal(Order(quantity, unit), out _));
    [Theory]
    [InlineData("usd", "2026-10-25")]
    [InlineData("", "2026-10-25")]
    [InlineData("USD", "2026-02-29")]
    [InlineData("USD", "10/25/2026")]
    public void Currency_and_purchase_day_are_not_inferred(string currency, string day) =>
        Assert.False(ShoppingRecordRules.TryTotal(Order() with { Currency = currency, PurchasedOn = day }, out _));
    [Fact]
    public void Duplicate_line_identity_and_negative_total_are_rejected()
    {
        var id = Guid.NewGuid();
        Assert.False(ShoppingRecordRules.TryTotal(Order() with { Lines = [new(id, "A", "1", "1"), new(id, "B", "1", "1")] }, out _));
        Assert.False(ShoppingRecordRules.TryTotal(Order() with { DiscountAmount = "999" }, out _));
        Assert.False(ShoppingRecordRules.TryTotal(Order() with { Lines = [] }, out _));
    }
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://name:proof@example.invalid/")]
    [InlineData("https://example.invalid/\nheader")]
    public void Seller_website_is_inert_public_http_metadata(string url) => Assert.False(ShoppingRecordRules.ValidSeller(new("Synthetic seller", url)));
    [Fact]
    public void Seller_name_is_required_and_duplicate_names_are_not_an_auto_merge_rule()
    {
        Assert.False(ShoppingRecordRules.ValidSeller(new(" ", null)));
        Assert.True(ShoppingRecordRules.ValidSeller(new("Same name", "https://example.invalid/shop")));
        Assert.True(ShoppingRecordRules.ValidSeller(new("Same name", null)));
    }
    [Theory]
    [InlineData("Fixed", "2026-10-25", "2026-10-26", true)]
    [InlineData("Fixed", "2026-10-25", "2026-10-25", true)]
    [InlineData("Fixed", "2026-10-25", "2026-10-24", false)]
    [InlineData("Fixed", null, null, false)]
    [InlineData("Lifetime", null, null, true)]
    [InlineData("Lifetime", "2026-10-25", "9999-12-31", false)]
    [InlineData("Unknown", null, null, true)]
    [InlineData("Unknown", null, "2026-10-25", false)]
    public void Warranty_coverage_keeps_dates_distinct_from_lifetime_and_unknown(string kind, string? start, string? end, bool valid) =>
        Assert.Equal(valid, ShoppingRecordRules.ValidWarranty(new(Guid.NewGuid(), "Synthetic warranty", kind, start, end)));
}
