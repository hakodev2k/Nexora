using Nexora.Application.Learning;
using Nexora.Infrastructure.Learning;
using Xunit;

namespace Nexora.UnitTests;

public sealed class CertificationContractTests
{
    [Theory]
    [InlineData("2026-10-24", "Expired")]
    [InlineData("2026-10-25", "Active")]
    [InlineData("2026-10-26", "Active")]
    [InlineData(null, "Active")]
    public void Date_only_expiry_remains_valid_through_the_owners_current_date(string? expiry, string expected)
    {
        var day = expiry is null ? (DateOnly?)null : DateOnly.ParseExact(expiry, "yyyy-MM-dd");
        Assert.Equal(expected, CertificationRules.EffectiveStatus("Active", day, new DateOnly(2026, 10, 25)));
        Assert.Equal("Archived", CertificationRules.EffectiveStatus("Archived", day, new DateOnly(2026, 10, 25)));
    }
    [Fact]
    public void Optional_dates_allow_unknown_and_nonexpiring_but_never_inverted_ranges()
    {
        var date = new DateOnly(2026, 10, 25);
        Assert.True(CertificationRules.ValidDates(null, null));
        Assert.True(CertificationRules.ValidDates(date, null));
        Assert.True(CertificationRules.ValidDates(null, date));
        Assert.True(CertificationRules.ValidDates(date, date));
        Assert.False(CertificationRules.ValidDates(date, date.AddDays(-1)));
    }
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///private/credential")]
    [InlineData("https://user:password@example.invalid/credential")]
    [InlineData("https://example.invalid/\ncredential")]
    public void Verification_URL_rejects_active_content_credentials_and_control_characters(string url) =>
        Assert.False(CertificationRules.ValidMetadata("Chứng chỉ", "Đơn vị cấp", url));
    [Fact]
    public void Metadata_has_required_bounded_names_and_optional_inert_HTTP_URL()
    {
        Assert.True(CertificationRules.ValidMetadata(new string('a', 200), "Issuer", null));
        Assert.True(CertificationRules.ValidMetadata("Title", "Issuer", "https://example.invalid/check"));
        Assert.False(CertificationRules.ValidMetadata(new string('a', 201), "Issuer", null));
        Assert.False(CertificationRules.ValidMetadata(" ", "Issuer", null));
        Assert.False(CertificationRules.ValidMetadata("Title", " ", null));
    }
    [Fact]
    public void Private_cursors_and_previews_cannot_cross_owner_filter_date_or_key_scopes()
    {
        var codec = new CertificationTokenCodec("synthetic-certification-key");
        var token = codec.Protect("ownerA/queryA/dateA/list", "private title and boundary");
        Assert.DoesNotContain("private", token);
        Assert.Equal("private title and boundary", codec.Unprotect("ownerA/queryA/dateA/list", token));
        foreach (var scope in new[] { "ownerB/queryA/dateA/list", "ownerA/queryB/dateA/list", "ownerA/queryA/dateB/list", "preview" })
            Assert.Null(codec.Unprotect(scope, token));
        Assert.Null(new CertificationTokenCodec("other-key").Unprotect("ownerA/queryA/dateA/list", token));
        Assert.Null(codec.Unprotect("ownerA/queryA/dateA/list", token + "="));
        Assert.Null(codec.Unprotect("ownerA/queryA/dateA/list", token[..12] + (token[12] == 'a' ? 'b' : 'a') + token[13..]));
    }
}
