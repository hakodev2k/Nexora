using System.Text.Json;
using Nexora.Application.Organization;
using Xunit;

namespace Nexora.UnitTests;

public sealed class BookmarkTemplateRulesTests
{
    [Fact]
    public void Seed_is_a_draft_and_does_not_bypass_required_target_create_fields()
    {
        Assert.True(BookmarkTemplateRules.TryNormalize(new("  Reading list  ", new(null, null, null)), out var result));
        Assert.Equal("Reading list", result!.Name);
        Assert.Equal("", result.Seed.Title);
        Assert.Equal("", result.Seed.Url);
        Assert.Null(result.Seed.Description);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///c:/private")]
    [InlineData("data:text/html,<script>code</script>")]
    [InlineData("https://user:password@example.invalid/path")]
    [InlineData("/relative/path")]
    public void Unsafe_or_credential_bearing_URL_seed_is_rejected(string url)
    {
        Assert.False(BookmarkTemplateRules.TryNormalize(new("Template", new("Title", url, null)), out _));
    }

    [Fact]
    public void Names_and_seed_bounds_are_validated_without_truncating_content()
    {
        Assert.False(BookmarkTemplateRules.TryNormalize(new(" ", new(null, null, null)), out _));
        Assert.False(BookmarkTemplateRules.TryNormalize(new(new string('n', 101), new(null, null, null)), out _));
        Assert.False(BookmarkTemplateRules.TryNormalize(new("Name", new(new string('t', 201), null, null)), out _));
        Assert.False(BookmarkTemplateRules.TryNormalize(new("Name", new(null, null, new string('d', 4001))), out _));
        Assert.True(BookmarkTemplateRules.TryNormalize(new(new string('n', 100), new(new string('t', 200), "https://example.invalid/path", new string('界', 4000))), out var max));
        Assert.Equal(4000, max!.Seed.Description!.Length);
    }

    [Theory]
    [InlineData("{\"name\":\"Name\",\"seed\":{},\"ownerId\":\"foreign\"}")]
    [InlineData("{\"name\":\"Name\",\"seed\":{\"resourceId\":\"foreign\"}}")]
    [InlineData("{\"name\":\"Name\",\"seed\":{\"shareToken\":\"invalid\"}}")]
    [InlineData("{\"name\":\"Name\",\"seed\":{\"editorMode\":\"Code\"}}")]
    public void Unknown_identity_share_or_editor_fields_are_not_template_seed_fields(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TemplateWrite>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
