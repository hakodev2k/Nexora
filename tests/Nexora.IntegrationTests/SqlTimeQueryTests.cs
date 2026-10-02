using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlTimeQueryTests(SqlApiFixture fixture)
{
    private async Task<JsonElement> Get(SyntheticSession owner, string path)
    {
        using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private async Task<Guid> Create(SyntheticSession owner, DateTimeOffset start, int minutes, string category, string description)
    {
        using var response = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/time/entries",
            new { startAt = start, endAt = start.AddMinutes(minutes), description, category, confirmOverlap = true },
            await fixture.GetCsrfAsync(), Guid.NewGuid(), owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Filtered_pages_traverse_all_entries_and_fail_closed_for_foreign_or_filtered_cursors()
    {
        fixture.RequireAvailable();
        var owner = await fixture.CreateActiveSessionAsync();
        var foreign = await fixture.CreateActiveSessionAsync();
        var from = new DateTimeOffset(2026, 3, 29, 0, 30, 0, TimeSpan.Zero);
        var category = "page-" + Guid.NewGuid().ToString("N");
        var expected = new HashSet<Guid>();
        for (var index = 0; index < 31; index++)
            expected.Add(await Create(owner, from.AddMinutes(index / 2 * 2), 1, category, "literal_%[ entry " + index));
        var excluded = await Create(owner, from, 1, "other-" + Guid.NewGuid(), "excluded");
        var foreignId = await Create(foreign, from, 1, category, "literal_%[ foreign");
        var path = "/api/v1/time/entries?category=" + category + "&query=" + Uri.EscapeDataString("_%[");
        var first = await Get(owner, path);
        Assert.Equal(25, first.GetProperty("items").GetArrayLength());
        var cursor = first.GetProperty("nextCursor").GetGuid();
        var second = await Get(owner, path + "&cursor=" + cursor);
        Assert.Equal(6, second.GetProperty("items").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, second.GetProperty("nextCursor").ValueKind);
        var actual = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray())
            .Select(item => item.GetProperty("id").GetGuid()).ToArray();
        Assert.Equal(actual.Length, actual.Distinct().Count());
        Assert.True(expected.SetEquals(actual));
        var starts = first.GetProperty("items").EnumerateArray().Concat(second.GetProperty("items").EnumerateArray())
            .Select(item => item.GetProperty("startAt").GetDateTimeOffset()).ToArray();
        Assert.Equal(starts.OrderDescending().ToArray(), starts);
        foreach (var unavailableCursor in new[] { foreignId, excluded, Guid.NewGuid() })
            Assert.Empty((await Get(owner, path + "&cursor=" + unavailableCursor)).GetProperty("items").EnumerateArray());
        Assert.Empty((await Get(owner, "/api/v1/time/entries?query=" + Uri.EscapeDataString("does-not-exist_%[")))
            .GetProperty("items").EnumerateArray());
        Assert.Equal(31, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [time].[Entry] WHERE OwnerId=@Owner AND Category=@Category",
            new SqlParameter("@Owner", SqlDbType.UniqueIdentifier) { Value = owner.OwnerId },
            new SqlParameter("@Category", SqlDbType.NVarChar, 200) { Value = category }));
    }

    [Fact]
    public async Task Range_report_keeps_gross_overlap_and_uses_half_open_UTC_boundaries()
    {
        fixture.RequireAvailable();
        var owner = await fixture.CreateActiveSessionAsync(); var foreign = await fixture.CreateActiveSessionAsync();
        var start = new DateTimeOffset(2026, 3, 29, 0, 30, 0, TimeSpan.Zero);
        var category = "report-" + Guid.NewGuid().ToString("N");
        await Create(owner, start, 120, category, "included A");
        await Create(owner, start.AddMinutes(30), 60, category, "included B");
        await Create(owner, start.AddHours(1), 10, category, "exclusive upper endpoint");
        await Create(owner, start.AddMinutes(-1), 30, category, "begins before range but overlaps");
        await Create(owner, start.AddMinutes(-20), 20, category, "exclusive lower endpoint");
        await Create(foreign, start, 120, category, "included foreign");
        using var running = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/time/timer",
            new { description = "included running", category }, await fixture.GetCsrfAsync(), Guid.NewGuid(), owner.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, running.StatusCode);
        Assert.Equal(5, (await Get(owner, "/api/v1/time/report?category=" + category)).GetProperty("entryCount").GetInt32());
        var query = "?from=" + Uri.EscapeDataString(start.ToOffset(TimeSpan.FromHours(7)).ToString("O")) +
                    "&to=" + Uri.EscapeDataString(start.AddHours(1).ToString("O")) + "&category=" + category;
        var report = await Get(owner, "/api/v1/time/report" + query);
        Assert.Equal(3, report.GetProperty("entryCount").GetInt32());
        Assert.Equal(210 * 60_000L, report.GetProperty("grossDurationMilliseconds").GetInt64());
        Assert.True(report.GetProperty("hasOverlaps").GetBoolean());
        var single = await Get(owner, "/api/v1/time/report" + query + "&query=included%20A");
        Assert.Equal(120 * 60_000L, single.GetProperty("grossDurationMilliseconds").GetInt64());
        Assert.False(single.GetProperty("hasOverlaps").GetBoolean());
        var list = await Get(owner, "/api/v1/time/entries" + query);
        Assert.Equal(3, list.GetProperty("items").GetArrayLength());
        var first = list.GetProperty("items")[0];
        using var trash = await fixture.SendJsonAsync(HttpMethod.Post,
            "/api/v1/time/entries/" + first.GetProperty("id").GetGuid() + "/trash", new { },
            await fixture.GetCsrfAsync(), Guid.NewGuid(), owner.RawSessionHandle, first.GetProperty("etag").GetString());
        Assert.Equal(HttpStatusCode.OK, trash.StatusCode);
        Assert.Equal(2, (await Get(owner, "/api/v1/time/report" + query)).GetProperty("entryCount").GetInt32());
        Assert.Single((await Get(owner, "/api/v1/time/entries" + query + "&trash=true")).GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Filters_do_not_bypass_authentication_admin_grants_or_validation()
    {
        fixture.RequireAvailable(); var owner = await fixture.CreateActiveSessionAsync();
        var admin = await fixture.CreateActiveSessionAsync("Admin");
        foreach (var endpoint in new[] { "/api/v1/time/entries", "/api/v1/time/report" })
        {
            using var anonymous = await fixture.Client.GetAsync(endpoint + "?category=Synthetic");
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            using var denied = await fixture.SendAuthenticatedAsync(HttpMethod.Get, endpoint + "?category=Synthetic", admin.RawSessionHandle);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            foreach (var query in new[] { "?from=2026-03-29T01:00:00Z&to=2026-03-29T01:00:00Z",
                         "?from=2026-03-29T02:00:00Z&to=2026-03-29T01:00:00Z",
                         "?category=" + new string('x', 201), "?query=" + new string('x', 2001) })
            {
                using var invalid = await fixture.SendAuthenticatedAsync(HttpMethod.Get, endpoint + query, owner.RawSessionHandle);
                Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
            }
        }
        await fixture.SetModuleEnabledAsync(owner.UserId, "FX18", false);
        foreach (var endpoint in new[] { "/api/v1/time/entries", "/api/v1/time/report" })
        {
            using var disabled = await fixture.SendAuthenticatedAsync(HttpMethod.Get, endpoint + "?category=Synthetic", owner.RawSessionHandle);
            Assert.Equal(HttpStatusCode.Forbidden, disabled.StatusCode);
        }
    }
}
