using System.Data;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Finance;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Finance;
using Nexora.Infrastructure.Persistence;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlFinanceAuthorityTests(SqlApiFixture fixture)
{
    private SqlFinanceService Service() => new(new SqlConnectionFactory(fixture.ConnectionString!), "synthetic-finance-key");
    private static IdentityPrincipal Actor(SyntheticSession s, string role = "User") => new(s.UserId, s.OwnerId, role, DateTimeOffset.UtcNow, s.SessionId);
    private static FinanceManualRecordCommand Record(Guid id, string amount = "1.25", string currency = "USD", string? note = null) =>
        new(id, amount, currency, new DateOnly(2026, 10, 13), note);

    [Fact]
    public async Task Record_pages_preserve_filters_owner_currency_totals_and_exact_ID_read()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var other = await fixture.CreateActiveSessionAsync();
        var actor = Actor(session); var service = Service(); var category = service.CreateCategory(actor, new("Manual prices", null)).Value!;
        var ids = new List<Guid>();
        for (var i = 0; i < 27; i++) { var r = service.CreateRecord(actor, Record(category.Id, "0.00000001")); Assert.True(r.Succeeded, r.Code); ids.Add(r.Value!.Id); }
        Assert.True(service.CreateRecord(actor, Record(category.Id, "100", "VND")).Succeeded);
        var first = service.ListRecords(actor, currencyCode: "USD"); Assert.True(first.Succeeded, first.Code);
        Assert.Equal(25, first.Value!.Items.Count); Assert.NotNull(first.Value.NextCursor);
        Assert.Equal(new FinanceSummary("USD", "0.00000027"), Assert.Single(first.Value.Summaries));
        var next = service.ListRecords(actor, currencyCode: "USD", cursor: first.Value.NextCursor);
        Assert.Equal(2, next.Value!.Items.Count); Assert.Null(next.Value.NextCursor); Assert.Equal(first.Value.Summaries, next.Value.Summaries);
        Assert.Equal(ids.Order(), first.Value.Items.Concat(next.Value.Items).Select(r => r.Id).Order());
        Assert.Equal("InvalidCursor", service.ListRecords(Actor(other), currencyCode: "USD", cursor: first.Value.NextCursor).Code);
        Assert.Equal("InvalidCursor", service.ListRecords(actor, currencyCode: "VND", cursor: first.Value.NextCursor).Code);
        Assert.Equal("InvalidCursor", service.ListRecords(actor, currencyCode: "USD", limit: 10, cursor: first.Value.NextCursor).Code);
        Assert.Equal("InvalidCursor", service.ListRecords(actor, currencyCode: "USD", query: "x", cursor: first.Value.NextCursor).Code);
        Assert.Equal("InvalidCursor", service.ListRecords(actor, currencyCode: "USD", cursor: first.Value.NextCursor + "x").Code);
        var all = service.ListRecords(actor); Assert.Equal(2, all.Value!.Summaries.Count); Assert.Equal("100", all.Value.Summaries.Single(s => s.CurrencyCode == "VND").Amount);
        using var detail = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"/api/v1/finance/records/{ids[^1]}", session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode); Assert.NotNull(detail.Headers.ETag);
        using var foreign = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"/api/v1/finance/records/{ids[^1]}", other.RawSessionHandle);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        using var apiFirst = await fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/finance/records?currencyCode=USD", session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, apiFirst.StatusCode); using var json = JsonDocument.Parse(await apiFirst.Content.ReadAsStringAsync());
        var cursor = json.RootElement.GetProperty("nextCursor").GetString(); Assert.NotNull(cursor);
        using var apiNext = await fixture.SendAuthenticatedAsync(HttpMethod.Get, "/api/v1/finance/records?currencyCode=USD&cursor=" + Uri.EscapeDataString(cursor!), session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, apiNext.StatusCode); using var nextJson = JsonDocument.Parse(await apiNext.Content.ReadAsStringAsync()); Assert.Equal(2, nextJson.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Categories_page_all_rows_and_literal_search_rejects_unbounded_queries()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Service(); var ids = new List<Guid>();
        for (var i = 0; i < 27; i++) { var r = service.CreateCategory(actor, new($"Category {i:D3}", null)); Assert.True(r.Succeeded, r.Code); ids.Add(r.Value!.Id); }
        var first = service.ListCategories(actor); Assert.Equal(25, first.Value!.Items.Count); Assert.NotNull(first.Value.NextCursor);
        var next = service.ListCategories(actor, cursor: first.Value.NextCursor); Assert.Equal(2, next.Value!.Items.Count); Assert.Null(next.Value.NextCursor);
        Assert.Equal(ids.Order(), first.Value.Items.Concat(next.Value.Items).Select(c => c.Id).Order());
        Assert.Equal("InvalidCursor", service.ListCategories(actor, query: "changed", cursor: first.Value.NextCursor).Code);
        using var detail = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"/api/v1/finance/categories/{ids[^1]}", session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode); Assert.NotNull(detail.Headers.ETag);
        foreach (var literal in new[] { "%", "_", "[", "~", "\\" })
        {
            var c = service.CreateCategory(actor, new("Literal" + literal, null)); Assert.True(c.Succeeded, c.Code);
            Assert.Equal(c.Value!.Id, Assert.Single(service.ListCategories(actor, literal).Value!.Items).Id);
            var r = service.CreateRecord(actor, Record(c.Value.Id, note: "Note" + literal)); Assert.True(r.Succeeded, r.Code);
            Assert.Equal(r.Value!.Id, Assert.Single(service.ListRecords(actor, query: literal).Value!.Items).Id);
        }
        Assert.Equal("ValidationFailed", service.ListCategories(actor, new string('x', 101)).Code); Assert.Equal("ValidationFailed", service.ListRecords(actor, query: new string('x', 201)).Code);
    }

    [Fact]
    public async Task Durable_replays_preserve_one_effect_case_source_counts_titles_and_current_lifecycle()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Service();
        var key = Guid.NewGuid().ToString(); var command = new FinanceCategoryCommand("Case|title:name", null); var c = service.CreateCategory(actor, command, key);
        Assert.True(c.Succeeded, c.Code); Assert.Equal(c, service.CreateCategory(actor, command, key));
        Assert.Equal("IdempotencyConflict", service.CreateCategory(actor, command with { Title = command.Title.ToUpperInvariant() }, key).Code);
        var recordKey = Guid.NewGuid().ToString(); var recordCommand = Record(c.Value!.Id, note: "Private |currency:VND"); var r = service.CreateRecord(actor, recordCommand, recordKey);
        Assert.True(r.Succeeded, r.Code); Assert.Equal(r, service.CreateRecord(actor, recordCommand, recordKey));
        var countChanged = service.CreateCategory(actor, command, key); Assert.True(countChanged.Succeeded); Assert.Null(countChanged.Value); Assert.NotNull(countChanged.Acknowledgement);
        var renamed = service.UpdateCategory(actor, c.Value.Id, c.Value.ETag, new("Changed category title", null)); Assert.True(renamed.Succeeded, renamed.Code);
        var titleChanged = service.CreateRecord(actor, recordCommand, recordKey); Assert.True(titleChanged.Succeeded); Assert.Null(titleChanged.Value); Assert.NotNull(titleChanged.Acknowledgement);
        Assert.Equal("IdempotencyConflict", service.CreateRecord(actor, recordCommand with { Note = "Different" }, recordKey).Code); Assert.Single(service.ListRecords(actor).Value!.Items);
        var updateKey = Guid.NewGuid().ToString(); var updated = service.UpdateRecord(actor, r.Value!.Id, r.Value.ETag, recordCommand with { Amount = "2" }, updateKey);
        Assert.True(updated.Succeeded, updated.Code); Assert.Equal(updated, service.UpdateRecord(actor, r.Value.Id, r.Value.ETag, recordCommand with { Amount = "2" }, updateKey));
        var old = service.CreateRecord(actor, recordCommand, recordKey); Assert.True(old.Succeeded); Assert.Null(old.Value); Assert.Equal(r.Value.Id, old.Acknowledgement!.Id);
        Assert.Equal("RevisionConflict", service.UpdateRecord(actor, r.Value.Id, r.Value.ETag, recordCommand).Code);
        Assert.Equal("CategoryInUse", service.RemoveCategory(actor, c.Value.Id, service.GetCategory(actor, c.Value.Id).Value!.ETag).Code);
        var unusedKey = Guid.NewGuid().ToString(); var unusedCommand = new FinanceCategoryCommand("Unused", null); var unused = service.CreateCategory(actor, unusedCommand, unusedKey);
        var removeKey = Guid.NewGuid().ToString(); var removed = service.RemoveCategory(actor, unused.Value!.Id, unused.Value.ETag, removeKey);
        Assert.Equal(204, removed.StatusCode); Assert.Equal(removed, service.RemoveCategory(actor, unused.Value.Id, unused.Value.ETag, removeKey));
        var deleted = service.CreateCategory(actor, unusedCommand, unusedKey); Assert.True(deleted.Succeeded); Assert.Null(deleted.Value); Assert.NotNull(deleted.Acknowledgement);
        var stored = await fixture.ScalarStringAsync("SELECT STRING_AGG(CONVERT(nvarchar(max), [ResultJson]), N'') FROM [identity].[RequestReceipt] WHERE [OperationKey] LIKE N'finance.manual_%';");
        Assert.DoesNotContain("Private", stored); Assert.DoesNotContain("Note", stored); Assert.DoesNotContain("Amount", stored);
        await fixture.SetModuleEnabledAsync(session.UserId, "FX27", false); Assert.False(service.CreateRecord(actor, recordCommand, recordKey).Succeeded); Assert.False(service.RemoveCategory(actor, unused.Value.Id, unused.Value.ETag, removeKey).Succeeded);
    }

    [Fact]
    public async Task Admin_create_only_receives_committed_ACK_and_category_reader_cannot_read_record_counts()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync("Admin"); var actor = Actor(session, "Admin"); var service = Service();
        await Grant(session.UserId, "finance.manual_category.create"); var key = Guid.NewGuid().ToString(); var command = new FinanceCategoryCommand("Private category", null);
        var c = service.CreateCategory(actor, command, key); Assert.True(c.Succeeded, c.Code); Assert.Null(c.Value); Assert.NotNull(c.Acknowledgement); Assert.Equal(c, service.CreateCategory(actor, command, key));
        await Grant(session.UserId, "finance.manual_category.read"); await Grant(session.UserId, "finance.manual_record.create");
        var category = service.GetCategory(actor, c.Acknowledgement!.Id); Assert.True(category.Succeeded, category.Code); Assert.Null(category.Value!.UsageCount);
        var r = service.CreateRecord(actor, Record(category.Value.Id, "123.45", note: "Private note")); Assert.True(r.Succeeded, r.Code); Assert.Null(r.Value); Assert.NotNull(r.Acknowledgement); Assert.Null(Assert.Single(service.ListCategories(actor).Value!.Items).UsageCount);
        using var apiCreate = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/finance/records", new { categoryId = category.Value.Id, amount = "12.5", currencyCode = "USD", occurredOn = "2026-10-13", note = "Hidden" }, await fixture.GetCsrfAsync(), Guid.NewGuid(), session.RawSessionHandle);
        Assert.Equal(HttpStatusCode.Created, apiCreate.StatusCode); using var json = JsonDocument.Parse(await apiCreate.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "committed", "detailAvailable", "id" }, json.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray()); Assert.True(json.RootElement.GetProperty("committed").GetBoolean()); Assert.False(json.RootElement.GetProperty("detailAvailable").GetBoolean());
        await Grant(session.UserId, "finance.manual_record.read"); Assert.False(service.ListRecords(actor).Succeeded); Assert.Null(service.GetCategory(actor, category.Value.Id).Value!.UsageCount);
        await Grant(session.UserId, "finance.manual_summary.read"); Assert.Equal(2, service.GetCategory(actor, category.Value.Id).Value!.UsageCount);
        await fixture.ExecuteAsync("DELETE ap FROM [platform].[AdminPermission] ap INNER JOIN [platform].[Permission] p ON p.[Id]=ap.[PermissionId] WHERE ap.[UserId]=@User AND p.[ActionKey]='finance.manual_category.create';", User(session.UserId)); Assert.False(service.CreateCategory(actor, command, key).Succeeded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Final_authority_guard_rolls_back_business_audit_and_receipt_when_current_gate_changes(bool keyed)
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Service();
        // Deterministic fixture-only trigger changes current authority after the
        // write, before commit; no timing sleeps or production migration change.
        await fixture.ExecuteAsync($"""
            CREATE TRIGGER [finance].[FinanceAuthorityIntegrationGuard] ON [finance].[ManualCategory] AFTER INSERT AS
            BEGIN
                SET NOCOUNT ON;
                UPDATE g SET [Enabled]=0 FROM [platform].[UserModuleGrant] g INNER JOIN [platform].[Module] m ON m.[Id]=g.[ModuleId]
                WHERE m.[Code]='FX27' AND g.[UserId]='{session.UserId:D}' AND EXISTS(SELECT 1 FROM inserted i WHERE i.[OwnerId]='{session.OwnerId:D}');
            END;
            """);
        try
        {
            var receiptCount = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt];");
            var r = service.CreateCategory(actor, new("Rejected at commit", null), keyed ? Guid.NewGuid().ToString() : null); Assert.False(r.Succeeded); Assert.Equal("ModuleUnavailable", r.Code);
            Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [finance].[ManualCategory] WHERE [OwnerId]=@Owner;", Owner(session.OwnerId)));
            Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE [ActorUserId]=@User AND [ActionKey]='finance.manual_category.create';", User(session.UserId)));
            Assert.Equal(receiptCount, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[RequestReceipt];"));
            Assert.True(service.ListCategories(actor).Succeeded);
        }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [finance].[FinanceAuthorityIntegrationGuard];"); }
    }

    [Fact]
    public async Task Owner_amount_precision_locale_strict_ETag_and_legacy_receipt_guards_hold()
    {
        fixture.RequireAvailable(); var session = await fixture.CreateActiveSessionAsync(); var other = await fixture.CreateActiveSessionAsync(); var actor = Actor(session); var service = Service();
        var category = service.CreateCategory(actor, new("Amounts", null)).Value!; var foreign = service.CreateCategory(Actor(other), new("Foreign", null)).Value!;
        Assert.Equal("CategoryUnavailable", service.CreateRecord(actor, Record(foreign.Id)).Code); Assert.Equal("ResourceUnavailable", service.GetCategory(actor, foreign.Id).Code);
        foreach (var invalid in new[] { "-1", "1e3", "0.000000001", "100000000000000000000", "1,2", "NaN" }) Assert.Equal("ValidationFailed", service.CreateRecord(actor, Record(category.Id, invalid)).Code);
        var zero = service.CreateRecord(actor, Record(category.Id, "0")); Assert.True(zero.Succeeded); Assert.Equal("0", zero.Value!.Amount);
        var maximum = service.CreateRecord(actor, Record(category.Id, "99999999999999999999.99999999")); Assert.True(maximum.Succeeded, maximum.Code); Assert.Equal("99999999999999999999.99999999", maximum.Value!.Amount);
        for (var i = 0; i < 8; i++) Assert.True(service.CreateRecord(actor, Record(category.Id, "99999999999999999999.99999999")).Succeeded);
        var priorCulture = CultureInfo.CurrentCulture;
        try { foreach (var culture in new[] { "vi-VN", "en-US" }) { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture); Assert.Equal("899999999999999999999.99999991", Assert.Single(service.ListRecords(actor).Value!.Summaries).Amount); Assert.Equal("99999999999999999999.99999999", service.GetRecord(actor, maximum.Value.Id).Value!.Amount); Assert.Equal(new DateOnly(2026, 10, 13), service.GetRecord(actor, maximum.Value.Id).Value!.OccurredOn); } }
        finally { CultureInfo.CurrentCulture = priorCulture; }
        Assert.Equal("PreconditionRequired", service.UpdateRecord(actor, zero.Value.Id, zero.Value.ETag.Trim('"'), Record(category.Id)).Code); Assert.Equal("InvalidIdempotencyKey", service.CreateRecord(actor, Record(category.Id), "not-a-uuid").Code);
        var key = Guid.NewGuid().ToString(); var command = new FinanceCategoryCommand("Legacy receipt", null); var created = service.CreateCategory(actor, command, key);
        await fixture.ExecuteAsync("UPDATE [identity].[RequestReceipt] SET [ResultStatusCode]=NULL,[ResultJson]=NULL WHERE [OperationKey]='finance.manual_category.create' AND JSON_VALUE([ResultJson], '$.Id')=@Id;", new SqlParameter("@Id", SqlDbType.NVarChar, 36) { Value = created.Value!.Id.ToString() }); Assert.Equal("IdempotencyReplay", service.CreateCategory(actor, command, key).Code);
    }

    private async Task Grant(Guid user, string action) => await fixture.ExecuteAsync("INSERT INTO [platform].[AdminPermission]([UserId],[PermissionId],[Effect]) SELECT @User,[Id],'Allow' FROM [platform].[Permission] WHERE [ActionKey]=@Action;", User(user), new SqlParameter("@Action", SqlDbType.NVarChar, 160) { Value = action });
    private static SqlParameter User(Guid id) => new("@User", SqlDbType.UniqueIdentifier) { Value = id };
    private static SqlParameter Owner(Guid id) => new("@Owner", SqlDbType.UniqueIdentifier) { Value = id };
}
