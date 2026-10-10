using Nexora.Application.Monitoring;
using Nexora.Infrastructure.Monitoring;
using Xunit;

namespace Nexora.UnitTests;

public sealed class MonitorInputTests
{
    [Fact]
    public void Unicode_titles_and_idn_hosts_preserve_literal_content_without_network_resolution()
    {
        var input = new MonitorMetadata(1, "  Cá nhân <script>literal</script>  ", "Http", "https://bücher.example:443/health", int.MaxValue, null);
        Assert.True(MonitorInput.TryNormalize(input, out var result));
        Assert.Equal("Cá nhân <script>literal</script>", result.Title);
        Assert.Equal("https://xn--bcher-kva.example/health", result.Target);
        Assert.Null(result.ExpectedStatus);
        Assert.Equal(int.MaxValue, result.IntervalSeconds);
    }

    [Theory]
    [InlineData("https://monitor.internal/health")]
    [InlineData("https://monitor.local/health")]
    [InlineData("https://monitor.example./health")]
    [InlineData("https://a..example/health")]
    [InlineData("https://-a.example/health")]
    [InlineData("https://a_.example/health")]
    [InlineData("https://user:secret@monitor.example/health")]
    [InlineData("https://monitor.example/health\nmore")]
    public void Malformed_or_private_target_spelling_is_rejected(string target)
        => Assert.False(MonitorInput.Target(target, out _));

    [Fact]
    public void Invalid_unicode_and_utf16_length_limits_fail_closed()
    {
        var valid = new MonitorMetadata(1, new string('x', 200), "Http", "http://monitor.example/health", 1, 100);
        Assert.True(MonitorInput.TryNormalize(valid, out _));
        Assert.False(MonitorInput.TryNormalize(valid with { Title = new string('x', 201) }, out _));
        Assert.False(MonitorInput.TryNormalize(valid with { Title = "broken\ud800" }, out _));
        Assert.False(MonitorInput.TryNormalize(valid with { Title = "control\u0000" }, out _));
        Assert.False(MonitorInput.TryNormalize(valid with { SchemaVersion = 2 }, out _));
        Assert.False(MonitorInput.TryNormalize(valid with { Kind = "Heartbeat" }, out _));
        Assert.False(MonitorInput.TryNormalize(valid with { ExpectedStatus = 600 }, out _));
    }
}
