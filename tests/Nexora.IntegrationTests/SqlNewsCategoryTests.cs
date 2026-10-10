using System.Data;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Nexora.Application.Identity;
using Nexora.Application.News;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.News;
using Nexora.Infrastructure.Persistence;
using Xunit;

namespace Nexora.IntegrationTests;

[Collection(SqlApiCollection.Name)]
public sealed class SqlNewsCategoryTests
{
    private readonly SqlApiFixture fixture;
    public SqlNewsCategoryTests(SqlApiFixture fixture) => this.fixture = fixture;
    private const string Root = "/api/v1/news/categories";
    private static SqlParameter Id(string name, Guid value) => new(name, SqlDbType.UniqueIdentifier) { Value = value };
    private static Guid I(JsonElement item) => item.GetProperty("id").GetGuid();
    private static string E(JsonElement item) => item.GetProperty("etag").GetString()!;
    private static object Body(string name) => new { metadata = new { schemaVersion = 1, name } };
    private static async Task<JsonElement> Json(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    { Assert.True(response.StatusCode == expected, $"Expected {expected}, got {response.StatusCode}; private payload excluded."); using var d = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); return d.RootElement.Clone(); }
    private async Task<HttpResponseMessage> Send(SyntheticSession actor, HttpMethod method, string path, object body, string? etag = null, Guid? key = null) => await fixture.SendJsonAsync(method, path, body, await fixture.GetCsrfAsync(), key ?? Guid.NewGuid(), actor.RawSessionHandle, etag);
    private async Task<JsonElement> Get(SyntheticSession actor, string path)
    { using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path, actor.RawSessionHandle); return await Json(response); }
    private async Task<JsonElement> Create(SyntheticSession actor, string name)
    { using var response = await Send(actor, HttpMethod.Post, Root, Body(name)); return await Json(response, HttpStatusCode.Created); }
    private sealed record Scope(SqlNewsCategoryTests Tests, JsonElement Original) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await Tests.fixture.ExecuteAsync("UPDATE [platform].[Module] SET State=@State,SystemEnabled=@System,RegistrationEnabled=@Registration WHERE Code='FX29'", new SqlParameter("@State", SqlDbType.VarChar, 32) { Value = Original.GetProperty("state").GetString() }, new SqlParameter("@System", SqlDbType.Bit) { Value = Original.GetProperty("systemEnabled").GetBoolean() }, new SqlParameter("@Registration", SqlDbType.Bit) { Value = Original.GetProperty("registrationEnabled").GetBoolean() });
    }
    private async Task<Scope> Enable()
    {
        fixture.RequireAvailable(); var super = await fixture.CreateActiveSessionAsync("SuperAdmin");
        var original = (await Get(super, "/api/v1/admin/modules?limit=100")).GetProperty("items").EnumerateArray().Single(x => x.GetProperty("code").GetString() == "FX29").Clone();
        await fixture.ExecuteAsync("UPDATE [platform].[Module] SET State='Ready',SystemEnabled=1,RegistrationEnabled=1 WHERE Code='FX29'"); return new(this, original);
    }
    private async Task Grant(SyntheticSession super, SyntheticSession actor, string effect, params string[] keys)
    {
        using var current = await fixture.SendAuthenticatedAsync(HttpMethod.Get, $"/api/v1/admin/users/{actor.UserId}/access", super.RawSessionHandle); await Json(current);
        var changes = keys.Select(actionKey => new { actionKey, effect }).ToArray(); using var previewResponse = await Send(super, HttpMethod.Post, $"/api/v1/admin/users/{actor.UserId}/access/preview", new { kind = "permissions", changes }); var preview = await Json(previewResponse);
        using var commit = await Send(super, HttpMethod.Put, $"/api/v1/admin/users/{actor.UserId}/access/permissions", new { kind = "permissions", changes, previewToken = preview.GetProperty("previewToken").GetString() }, current.Headers.ETag?.Tag); await Json(commit);
    }
    private static void Initialize(SqlConnection c, SqlTransaction tx, SyntheticSession actor)
    {
        var initializer = typeof(SqlNewsCategoryService).Assembly.GetType("Nexora.Infrastructure.News.SqlNewsOwnerInitializer", true)!;
        try { initializer.GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [c, tx, actor.OwnerId, actor.UserId, DateTime.UtcNow]); }
        catch (TargetInvocationException error) when (error.InnerException is not null) { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); }
    }

    [Fact] public async Task Literal_categories_allow_equal_names_return_opaque_ack_and_register_only_private_container_capabilities()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var a = await Create(owner, "  Cá nhân <script>literal</script>  "); var b = await Create(owner, "Cá nhân <script>literal</script>");
        Assert.NotEqual(I(a), I(b)); Assert.Equal(new[] { "etag", "id" }, a.EnumerateObject().Select(x => x.Name).Order().ToArray());
        var item = await Get(owner, Root + "/" + I(a)); Assert.Equal("Cá nhân <script>literal</script>", item.GetProperty("metadata").GetProperty("name").GetString());
        Assert.Equal(2, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [news].[Category] WHERE OwnerId=@Owner AND Name=N'Cá nhân <script>literal</script>'", Id("@Owner", owner.OwnerId)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId WHERE r.OwnerId=@Owner AND r.Id=@Id AND rt.ContractVersion='news-category-v1' AND JSON_VALUE(rt.CapabilitiesJson,'$.share')='false' AND JSON_VALUE(rt.CapabilitiesJson,'$.support')='false' AND JSON_VALUE(rt.CapabilitiesJson,'$.trash')='false' AND JSON_VALUE(rt.CapabilitiesJson,'$.search')='false'", Id("@Owner", owner.OwnerId), Id("@Id", I(a))));
        foreach (var path in new[] { "/remove", "/follow", "/refresh", "/share" }) { using var unavailable = await Send(owner, HttpMethod.Post, Root + "/" + I(a) + path, new { }); Assert.Equal(HttpStatusCode.NotFound, unavailable.StatusCode); }
    }
    [Fact] public async Task Binding_and_semantic_validation_fail_without_category_effects()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync();
        foreach (var body in new object[] { new { name = "Extra shape" }, new { metadata = new { name = "Missing version" } }, new { metadata = new { schemaVersion = 1, name = "Extra", ownerId = owner.OwnerId } }, new { metadata = new { schemaVersion = 1, name = 12 } } }) { using var response = await Send(owner, HttpMethod.Post, Root, body); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); }
        foreach (var name in new[] { "", "   ", new string('x', 101), "a\u0001b" }) { using var response = await Send(owner, HttpMethod.Post, Root, Body(name)); Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode); }
        using var version = await Send(owner, HttpMethod.Post, Root, new { metadata = new { schemaVersion = 2, name = "Unknown version" } }); Assert.Equal(HttpStatusCode.UnprocessableEntity, version.StatusCode);
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [news].[Category] WHERE OwnerId=@Owner", Id("@Owner", owner.OwnerId)));
        await Create(owner, new string('x', 100));
    }
    [Fact] public async Task Literal_BIN2_owner_filtered_paging_and_uniform_unavailable_cursors_use_actual_twenty_five_rows()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var other = await fixture.CreateActiveSessionAsync();
        for (var n = 0; n < 31; n++) await Create(owner, "literal_%[x]_" + n.ToString("D2")); var foreign = await Create(other, "literal_%[x]_foreign"); var filtered = await Create(owner, "different"); await Create(owner, "LITERAL_%[x]_upper");
        var path = Root + "?query=" + Uri.EscapeDataString("literal_%[x]_"); var first = await Get(owner, path); Assert.Equal(25, first.GetProperty("items").GetArrayLength()); var cursor = first.GetProperty("nextCursor").GetGuid();
        var next = await Get(owner, path + "&cursor=" + cursor); Assert.Equal(6, next.GetProperty("items").GetArrayLength()); Assert.Equal(JsonValueKind.Null, next.GetProperty("nextCursor").ValueKind);
        foreach (var unavailable in new[] { I(foreign), I(filtered), Guid.NewGuid() }) { using var response = await fixture.SendAuthenticatedAsync(HttpMethod.Get, path + "&cursor=" + unavailable, owner.RawSessionHandle); Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); }
        using var hidden = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root + "/" + I(foreign), owner.RawSessionHandle); Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        await fixture.ExecuteAsync("UPDATE [platform].[Resource] SET Availability='Trash' WHERE Id=@Id", Id("@Id", I(filtered))); using var inactive = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root + "/" + I(filtered), owner.RawSessionHandle); Assert.Equal(HttpStatusCode.NotFound, inactive.StatusCode);
    }
    [Fact] public async Task Original_receipt_after_later_rename_and_noop_never_expose_name_or_change_audit_revision()
    {
        await using var scope = await Enable(); var owner = await fixture.CreateActiveSessionAsync(); var original = await Create(owner, "Original"); var id = I(original); var key = Guid.NewGuid(); var body = Body("First rename");
        using var update = await Send(owner, HttpMethod.Put, Root + "/" + id, body, E(original), key); var ack = await Json(update);
        using var later = await Send(owner, HttpMethod.Put, Root + "/" + id, Body("Later rename"), E(ack)); var laterAck = await Json(later);
        using var replay = await Send(owner, HttpMethod.Put, Root + "/" + id, body, E(original), key); Assert.Equal(ack.GetRawText(), (await Json(replay)).GetRawText());
        using var changed = await Send(owner, HttpMethod.Put, Root + "/" + id, Body("Wrong payload"), E(original), key); Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        using var missing = await Send(owner, HttpMethod.Put, Root + "/" + id, body); Assert.Equal((HttpStatusCode)428, missing.StatusCode);
        using var malformed = await Send(owner, HttpMethod.Put, Root + "/" + id, body, "invalid-etag"); Assert.Equal(HttpStatusCode.UnprocessableEntity, malformed.StatusCode);
        using var stale = await Send(owner, HttpMethod.Put, Root + "/" + id, body, E(original)); Assert.Equal(HttpStatusCode.PreconditionFailed, stale.StatusCode);
        var revision = await fixture.ScalarIntAsync("SELECT CONVERT(int,Revision) FROM [platform].[Resource] WHERE Id=@Id", Id("@Id", id)); var audit = await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='news.category.update'", Id("@Id", id));
        using var noop = await Send(owner, HttpMethod.Put, Root + "/" + id, Body("  Later rename  "), E(laterAck)); var unchanged = await Json(noop); Assert.Equal(E(laterAck), E(unchanged));
        Assert.Equal(revision, await fixture.ScalarIntAsync("SELECT CONVERT(int,Revision) FROM [platform].[Resource] WHERE Id=@Id", Id("@Id", id))); Assert.Equal(audit, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='news.category.update'", Id("@Id", id)));
    }
    [Theory][InlineData("read")][InlineData("create")][InlineData("update")]
    public async Task Admin_requires_each_exact_action_and_update_requires_read_independently(string name)
    {
        await using var scope = await Enable(); var super = await fixture.CreateActiveSessionAsync("SuperAdmin"); var admin = await fixture.CreateActiveSessionAsync("Admin");
        using (var absent = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root + "/capabilities", admin.RawSessionHandle)) Assert.Equal(HttpStatusCode.Forbidden, absent.StatusCode);
        using (var absent = await Send(admin, HttpMethod.Post, Root, Body("Absent grant"))) Assert.Equal(HttpStatusCode.Forbidden, absent.StatusCode);
        await Grant(super, admin, "Allow", SqlNewsCategoryService.Actions); var original = await Create(admin, "Admin private"); await Grant(super, admin, "Deny", "news.category." + name);
        using var denied = name switch { "read" => await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root + "/" + I(original), admin.RawSessionHandle), "create" => await Send(admin, HttpMethod.Post, Root, Body("Denied")), _ => await Send(admin, HttpMethod.Put, Root + "/" + I(original), Body("Denied"), E(original)) }; Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        if (name == "read") { using var alsoDenied = await Send(admin, HttpMethod.Put, Root + "/" + I(original), Body("Needs read"), E(original)); Assert.Equal(HttpStatusCode.Forbidden, alsoDenied.StatusCode); }
    }
    [Fact] public async Task Create_only_Admin_receives_opaque_ack_without_acquiring_read_authority()
    {
        await using var scope = await Enable(); var super = await fixture.CreateActiveSessionAsync("SuperAdmin"); var admin = await fixture.CreateActiveSessionAsync("Admin"); await Grant(super, admin, "Allow", "news.category.create");
        var ack = await Create(admin, "Private create-only"); Assert.Equal(new[] { "etag", "id" }, ack.EnumerateObject().Select(x => x.Name).Order().ToArray());
        using var read = await fixture.SendAuthenticatedAsync(HttpMethod.Get, Root + "/" + I(ack), admin.RawSessionHandle); Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
    }
    [Fact] public async Task Initialization_marker_serializes_defaults_and_survives_renaming_without_GET_side_effects()
    {
        await using var scope = await Enable(); var actor = await fixture.CreateActiveSessionAsync(); Assert.Empty((await Get(actor, Root)).GetProperty("items").EnumerateArray());
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [news].[OwnerInitialization] WHERE OwnerId=@Owner", Id("@Owner", actor.OwnerId)));
        async Task Init() { await using var c = new SqlConnection(fixture.ConnectionString); await c.OpenAsync(); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(); Initialize(c, tx, actor); await tx.CommitAsync(); }
        await Task.WhenAll(Task.Run(Init), Task.Run(Init)); var defaults = (await Get(actor, Root)).GetProperty("items").EnumerateArray().ToArray(); Assert.Equal(new[] { "AI News", "Tech News" }, defaults.Select(x => x.GetProperty("metadata").GetProperty("name").GetString()).ToArray());
        var ai = defaults[0]; using var rename = await Send(actor, HttpMethod.Put, Root + "/" + I(ai), Body("Owner renamed default"), E(ai)); await Json(rename); await Init();
        Assert.Equal(2, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [news].[Category] WHERE OwnerId=@Owner", Id("@Owner", actor.OwnerId))); Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [news].[OwnerInitialization] WHERE OwnerId=@Owner AND DefaultCategoryVersion=1", Id("@Owner", actor.OwnerId)));
        Assert.Equal("Owner renamed default", (await Get(actor, Root + "/" + I(ai))).GetProperty("metadata").GetProperty("name").GetString());
    }
    [Fact] public async Task Real_verification_rolls_back_on_marker_failure_and_reuses_the_same_anonymous_receipt_on_retry()
    {
        await using var scope = await Enable();
        var user = Guid.NewGuid(); var tokenId = Guid.NewGuid(); var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var email = $"news-verify-{user:N}@example.invalid";
        await fixture.ExecuteAsync("""
            INSERT [identity].[User](Id,Email,NormalizedEmail,PasswordHash,SecurityStamp,State,EmailConfirmed,IsDeleted,DisplayName,TimeZoneId,Locale)
                VALUES(@User,@Email,@Email,'Synthetic pending verification',NEWID(),'PendingVerification',0,0,'Synthetic verification','Etc/UTC','en');
            INSERT [identity].[UserRole](UserId,RoleId) SELECT @User,Id FROM [identity].[Role] WHERE Code='User';
            INSERT [identity].[OneTimeToken](Id,UserId,Purpose,TokenHash,EmailSnapshot,ExpiresAt)
                VALUES(@TokenId,@User,'EmailVerification',@Hash,@Email,DATEADD(hour,1,SYSUTCDATETIME()));
            """, Id("@User", user), Id("@TokenId", tokenId), new SqlParameter("@Email", SqlDbType.NVarChar, 320) { Value = email },
            new SqlParameter("@Hash", SqlDbType.Binary, 32) { Value = SHA256.HashData(Encoding.UTF8.GetBytes(token)) });
        var csrf = await fixture.GetCsrfAsync(); var key = Guid.NewGuid();
        await fixture.ExecuteAsync("CREATE TRIGGER [news].[RejectSyntheticVerificationMarker] ON [news].[OwnerInitialization] AFTER INSERT AS BEGIN THROW 51046,'Synthetic marker failure',1; END;");
        try
        {
            using var failed = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/auth/verifications", new { token }, csrf, key);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            var safeError = await failed.Content.ReadAsStringAsync();
            Assert.False(safeError.Contains(token, StringComparison.Ordinal), "Private token must not appear in the safe error.");
            Assert.False(safeError.Contains(email, StringComparison.Ordinal), "Private email must not appear in the safe error.");
            Assert.DoesNotContain("Synthetic marker failure", safeError);
        }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [news].[RejectSyntheticVerificationMarker]"); }
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[User] WHERE Id=@User AND State='PendingVerification' AND EmailConfirmed=0 AND VerifiedAt IS NULL", Id("@User", user)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [identity].[OneTimeToken] WHERE Id=@TokenId AND ConsumedAt IS NULL", Id("@TokenId", tokenId)));
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[PersonalSpace] WHERE UserId=@User", Id("@User", user)));
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[UserModuleGrant] WHERE UserId=@User", Id("@User", user)));
        using var retry = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/auth/verifications", new { token }, csrf, key);
        var verified = await Json(retry); var owner = verified.GetProperty("profile").GetProperty("personalSpaceId").GetGuid();
        Assert.NotEqual(user, owner);
        Assert.Equal(2, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [news].[Category] WHERE OwnerId=@Owner", Id("@Owner", owner)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [news].[OwnerInitialization] WHERE OwnerId=@Owner", Id("@Owner", owner)));
        var originalIds = await fixture.ScalarStringAsync("SELECT STRING_AGG(CONVERT(varchar(36),Id),',') WITHIN GROUP(ORDER BY Id) FROM [news].[Category] WHERE OwnerId=@Owner", Id("@Owner", owner));
        // Trusted SQL arrangement changes an existing default to prove receipt replay does not reinitialize it.
        await fixture.ExecuteAsync("UPDATE [news].[Category] SET Name=N'Renamed after verification' WHERE OwnerId=@Owner AND Name=N'AI News'", Id("@Owner", owner));
        using var replay = await fixture.SendJsonAsync(HttpMethod.Post, "/api/v1/auth/verifications", new { token }, csrf, key);
        Assert.Equal(verified.GetRawText(), (await Json(replay)).GetRawText());
        Assert.Equal(originalIds, await fixture.ScalarStringAsync("SELECT STRING_AGG(CONVERT(varchar(36),Id),',') WITHIN GROUP(ORDER BY Id) FROM [news].[Category] WHERE OwnerId=@Owner", Id("@Owner", owner)));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [news].[Category] WHERE OwnerId=@Owner AND Name=N'Renamed after verification'", Id("@Owner", owner)));
    }
    [Fact] public async Task Default_marker_failure_rolls_back_both_categories_and_registry_then_exact_retry_initializes_once()
    {
        await using var scope = await Enable(); var actor = await fixture.CreateActiveSessionAsync();
        await fixture.ExecuteAsync("CREATE TRIGGER [news].[RejectSyntheticNewsMarker] ON [news].[OwnerInitialization] AFTER INSERT AS BEGIN THROW 51046,'Synthetic marker failure',1; END;");
        try { await using var c = new SqlConnection(fixture.ConnectionString); await c.OpenAsync(); await using var tx = (SqlTransaction)await c.BeginTransactionAsync(); Assert.Throws<SqlException>(() => Initialize(c, tx, actor)); }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [news].[RejectSyntheticNewsMarker]"); }
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [news].[Category] WHERE OwnerId=@Owner", Id("@Owner", actor.OwnerId))); Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [news].[OwnerInitialization] WHERE OwnerId=@Owner", Id("@Owner", actor.OwnerId))); Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[Resource] r JOIN [platform].[ResourceType] rt ON rt.Id=r.ResourceTypeId WHERE r.OwnerId=@Owner AND rt.ContractVersion='news-category-v1'", Id("@Owner", actor.OwnerId)));
        await using var retry = new SqlConnection(fixture.ConnectionString); await retry.OpenAsync(); await using var retryTx = (SqlTransaction)await retry.BeginTransactionAsync(); Initialize(retry, retryTx, actor); await retryTx.CommitAsync(); Assert.Equal(2, (await Get(actor, Root)).GetProperty("items").GetArrayLength());
    }
    [Fact] public async Task Category_audit_failure_rolls_back_business_registry_and_receipt_then_same_key_retries_once()
    {
        await using var scope = await Enable(); var actor = await fixture.CreateActiveSessionAsync(); var name = "audit-" + Guid.NewGuid().ToString("N"); var createKey = Guid.NewGuid();
        const string trigger = "CREATE TRIGGER [security].[RejectSyntheticNewsCommandAudit] ON [security].[AuditEvent] AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE ActionKey IN('news.category.create','news.category.update')) THROW 51046,'Synthetic News audit failure',1; END;";
        await fixture.ExecuteAsync(trigger);
        try { using var failed = await Send(actor, HttpMethod.Post, Root, Body(name), key: createKey); Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode); }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [security].[RejectSyntheticNewsCommandAudit]"); }
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [news].[Category] WHERE OwnerId=@Owner", Id("@Owner", actor.OwnerId)));
        Assert.Equal(0, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [platform].[Resource] WHERE OwnerId=@Owner", Id("@Owner", actor.OwnerId)));
        using var retry = await Send(actor, HttpMethod.Post, Root, Body(name), key: createKey); var ack = await Json(retry, HttpStatusCode.Created);
        var revision = await fixture.ScalarIntAsync("SELECT CONVERT(int,Revision) FROM [platform].[Resource] WHERE Id=@Id", Id("@Id", I(ack)));
        var updateKey = Guid.NewGuid(); await fixture.ExecuteAsync(trigger);
        try { using var failed = await Send(actor, HttpMethod.Put, Root + "/" + I(ack), Body("Successful retry only"), E(ack), updateKey); Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode); }
        finally { await fixture.ExecuteAsync("DROP TRIGGER [security].[RejectSyntheticNewsCommandAudit]"); }
        var unchanged = await Get(actor, Root + "/" + I(ack)); Assert.Equal(E(ack), E(unchanged)); Assert.Equal(name, unchanged.GetProperty("metadata").GetProperty("name").GetString());
        Assert.Equal(revision, await fixture.ScalarIntAsync("SELECT CONVERT(int,Revision) FROM [platform].[Resource] WHERE Id=@Id", Id("@Id", I(ack))));
        using var updated = await Send(actor, HttpMethod.Put, Root + "/" + I(ack), Body("Successful retry only"), E(ack), updateKey); await Json(updated);
        Assert.Equal(revision + 1, await fixture.ScalarIntAsync("SELECT CONVERT(int,Revision) FROM [platform].[Resource] WHERE Id=@Id", Id("@Id", I(ack))));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='news.category.create'", Id("@Id", I(ack))));
        Assert.Equal(1, await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='news.category.update'", Id("@Id", I(ack))));
    }
    private static void Denied<T>(IdentityOperationResult<T> result) { Assert.False(result.Succeeded); Assert.Equal(403, result.StatusCode); Assert.Null(result.Value); }
    [Theory]
    [InlineData("stamp")][InlineData("idle")][InlineData("absolute")][InlineData("deleted")]
    [InlineData("moduleGrant")][InlineData("space")][InlineData("unverified")]
    public async Task Current_account_and_session_boundaries_deny_reads_and_receipt_replay(string boundary)
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var created=await Create(owner,"Synthetic category");
        var source=new SqlNewsCategoryService(new SqlConnectionFactory(fixture.ConnectionString!),"synthetic-news-receipt-secret");
        var actor=new IdentityPrincipal(owner.UserId,owner.OwnerId,"User",DateTimeOffset.UtcNow,owner.SessionId);
        var key=Guid.NewGuid().ToString();var body=new NewsCategoryCommand(new(1,"Private revision"));
        Assert.True(source.Update(actor,I(created),E(created),body,key,null).Succeeded);
        var sql=boundary switch {
            "stamp"=>"UPDATE [identity].[User] SET SecurityStamp=NEWID() WHERE Id=@User",
            "idle"=>"UPDATE [identity].[Session] SET IdleExpiresAt=DATEADD(second,-1,SYSUTCDATETIME()) WHERE Id=@Session",
            "absolute"=>"UPDATE [identity].[Session] SET AbsoluteExpiresAt=DATEADD(second,-1,SYSUTCDATETIME()) WHERE Id=@Session",
            "deleted"=>"UPDATE [identity].[User] SET State='Deleted',IsDeleted=1,DeletedAt=SYSUTCDATETIME() WHERE Id=@User",
            "moduleGrant"=>"UPDATE [platform].[UserModuleGrant] SET Enabled=0 WHERE UserId=@User AND ModuleId=(SELECT Id FROM [platform].[Module] WHERE Code='FX29')",
            "space"=>"UPDATE [platform].[PersonalSpace] SET State='Suspended' WHERE Id=@Owner",
            _=>"UPDATE [identity].[User] SET EmailConfirmed=0,VerifiedAt=NULL WHERE Id=@User"};
        await fixture.ExecuteAsync(sql,Id("@User",owner.UserId),Id("@Session",owner.SessionId),Id("@Owner",owner.OwnerId));
        Denied(source.Get(actor,I(created)));Denied(source.List(actor,null,null));
        Denied(source.Update(actor,I(created),E(created),body,key,null));
        Assert.Equal(1,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='news.category.update'",Id("@Id",I(created))));
    }
    [Theory][InlineData("User")][InlineData("Admin")][InlineData("SuperAdmin")]
    public async Task Unsupported_actions_fail_closed_even_with_stale_resolved_permission_and_allow(string role)
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync(role);
        var type=typeof(SqlNewsCategoryService).Assembly.GetType("Nexora.Infrastructure.Authorization.SqlSelfCapability",throwOnError:true)!;
        var capability=Activator.CreateInstance(type,new SqlConnectionFactory(fixture.ConnectionString!))!;
        var evaluate=type.GetMethod("IsAllowed",new[]{typeof(SqlConnection),typeof(SqlTransaction),typeof(IdentityPrincipal),typeof(string),typeof(string[])})!;
        var actor=new IdentityPrincipal(owner.UserId,owner.OwnerId,role,DateTimeOffset.UtcNow,owner.SessionId);
        const string key="news.source.refresh";
        using var connection=new SqlConnection(fixture.ConnectionString!);await connection.OpenAsync();using var transaction=connection.BeginTransaction();
        using var command=new SqlCommand("IF NOT EXISTS(SELECT 1 FROM [platform].[Permission] WHERE ActionKey=@Key) INSERT [platform].[Permission](ActionKey,EffectiveStatus) VALUES(@Key,'Resolved'); UPDATE [platform].[Permission] SET EffectiveStatus='Resolved' WHERE ActionKey=@Key; INSERT [platform].[AdminPermission](UserId,PermissionId,Effect) SELECT @User,Id,'Allow' FROM [platform].[Permission] WHERE ActionKey=@Key; SELECT COUNT(*) FROM [platform].[Permission] WHERE ActionKey=@Key AND EffectiveStatus='Resolved'",connection,transaction);
        command.Parameters.AddWithValue("@Key",key);command.Parameters.AddWithValue("@User",owner.UserId);
        try{Assert.Equal(1,Convert.ToInt32(await command.ExecuteScalarAsync()));Assert.False((bool)evaluate.Invoke(capability,new object[]{connection,transaction,actor,"FX29",new[]{key}})!);}
        finally{transaction.Rollback();}
    }
    [Fact] public async Task Concurrent_new_updates_have_one_winner_and_same_key_retry_has_one_effect()
    {
        await using var scope=await Enable();var owner=await fixture.CreateActiveSessionAsync();var created=await Create(owner,"Synthetic category");var id=I(created);var body=Body("One concurrent change");
        var results=await Task.WhenAll(Send(owner,HttpMethod.Put,$"{Root}/{id}",body,E(created)),Send(owner,HttpMethod.Put,$"{Root}/{id}",body,E(created)));
        try{Assert.Equal(1,results.Count(r=>r.StatusCode==HttpStatusCode.OK));Assert.Equal(1,results.Count(r=>r.StatusCode==HttpStatusCode.PreconditionFailed));}finally{foreach(var r in results)r.Dispose();}
        var current=await Get(owner,$"{Root}/{id}");var key=Guid.NewGuid();var duplicateBody=Body("Original retry acknowledgement");var duplicates=await Task.WhenAll(Send(owner,HttpMethod.Put,$"{Root}/{id}",duplicateBody,E(current),key),Send(owner,HttpMethod.Put,$"{Root}/{id}",duplicateBody,E(current),key));
        try{Assert.Equal((await Json(duplicates[0])).GetRawText(),(await Json(duplicates[1])).GetRawText());}finally{foreach(var r in duplicates)r.Dispose();}
        Assert.Equal(2,await fixture.ScalarIntAsync("SELECT COUNT(*) FROM [security].[AuditEvent] WHERE TargetId=@Id AND ActionKey='news.category.update'",Id("@Id",id)));
    }
}
