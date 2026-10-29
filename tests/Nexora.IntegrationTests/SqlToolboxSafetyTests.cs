using System.Net;
using System.Text.Json;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlToolboxSafetyTests(SqlApiFixture fixture)
{
    [Fact]
    public async Task Invalid_UTF8_is_a_safe_validation_error()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        using var response = await Run(session.RawSessionHandle, "base64", "/w==", new { operation = "decode" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("InputInvalid", body);
        Assert.DoesNotContain("DecoderFallbackException", body);
    }

    [Fact]
    public async Task Invalid_regex_does_not_echo_private_pattern()
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        using var response = await Run(session.RawSessionHandle, "regex", "sample", new { pattern = "(private-pattern-canary" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("RegexInvalid", body);
        Assert.DoesNotContain("private-pattern-canary", body);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(99, false)]
    [InlineData(100, false)]
    [InlineData(101, true)]
    [InlineData(10000, true)]
    public async Task Regex_result_cap_and_truncation_are_exact(int count, bool truncated)
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        using var response = await Run(session.RawSessionHandle, "regex", new string('a', count), new { pattern = "a" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        using var output = JsonDocument.Parse(result.RootElement.GetProperty("output").GetString()!);
        Assert.Equal(Math.Min(count, 100), output.RootElement.GetProperty("count").GetInt32());
        Assert.Equal(truncated, output.RootElement.GetProperty("truncated").GetBoolean());
        Assert.Equal(Math.Min(count, 100), output.RootElement.GetProperty("matches").GetArrayLength());
    }

    [Theory]
    [InlineData("SHA1")]
    [InlineData("SHA-1")]
    [InlineData("MD5")]
    public async Task Legacy_hash_aliases_always_warn(string algorithm)
    {
        fixture.RequireAvailable();
        var session = await fixture.CreateActiveSessionAsync();
        using var response = await Run(session.RawSessionHandle, "hash", "test", new { algorithm });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains("Legacy checksum", result.RootElement.GetProperty("warning").GetString());
    }

    private async Task<HttpResponseMessage> Run(string session, string toolCode, string input, object options)
    {
        var csrf = await fixture.GetCsrfAsync();
        return await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/developer/tools/run",
            new { toolCode, input, options }, csrf, Guid.NewGuid(), session);
    }
}
