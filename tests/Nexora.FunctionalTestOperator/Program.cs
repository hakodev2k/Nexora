using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Nexora.Domain.Identity;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Local;
var cs=LocalSqlTarget.Validate(Environment.GetEnvironmentVariable("NEXORA_SQL_CONNECTION_STRING"),"Development");
var b=new SqlConnectionStringBuilder(cs);var db=b.InitialCatalog;
if(!Regex.IsMatch(db,"^Nexora_Test_[0-9a-f]{32}$"))throw new Exception("Isolated database required");

if(args is ["inventory"]){
 await using var inventory=new SqlConnection(cs);await inventory.OpenAsync();
 var counts=new Dictionary<string,long>();
 foreach(var table in new[]{"[identity].[User]","[platform].[PersonalSpace]","[productivity].[Project]","[productivity].[Task]","[calendar].[Event]"}){
  await using var q=new SqlCommand($"SELECT COUNT_BIG(*) FROM {table}",inventory);counts[table]=(long)(await q.ExecuteScalarAsync())!;
 }
 Console.WriteLine(JsonSerializer.Serialize(counts));return;
}
if(args is ["catalog"]) {
 await using var c=new SqlConnection(cs);await c.OpenAsync();
 await using var q=new SqlCommand("SELECT JSON_QUERY((SELECT Code,State,SystemEnabled,RegistrationEnabled FROM [platform].[Module] ORDER BY Code FOR JSON PATH)) AS modules,JSON_QUERY((SELECT ActionKey,EffectiveStatus FROM [platform].[Permission] ORDER BY ActionKey FOR JSON PATH)) AS actions FOR JSON PATH, WITHOUT_ARRAY_WRAPPER",c);
 await using var reader=await q.ExecuteReaderAsync();var output=new System.Text.StringBuilder();while(await reader.ReadAsync())output.Append(reader.GetString(0));
 Console.WriteLine(output.ToString());return;
}
if(args is ["read-access",var userIdText]) {
 if(!Guid.TryParse(userIdText,out var userId))throw new ArgumentException("A UUID is required");
 await using var c=new SqlConnection(cs);await c.OpenAsync();
 await using var q=new SqlCommand("SELECT JSON_QUERY((SELECT p.ActionKey,g.Effect FROM [platform].[AdminPermission] g JOIN [platform].[Permission] p ON p.Id=g.PermissionId WHERE g.UserId=@id FOR JSON PATH)) AS actionGrants,JSON_QUERY((SELECT m.Code,g.Enabled FROM [platform].[UserModuleGrant] g JOIN [platform].[Module] m ON m.Id=g.ModuleId WHERE g.UserId=@id FOR JSON PATH)) AS moduleGrants FOR JSON PATH, WITHOUT_ARRAY_WRAPPER",c);
 q.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=userId;
 await using var reader=await q.ExecuteReaderAsync();var output=new System.Text.StringBuilder();while(await reader.ReadAsync())output.Append(reader.GetString(0));
 Console.WriteLine(output.ToString());return;
}
if(args is ["read-session",var sessionIdText]) {
 if(!Guid.TryParse(sessionIdText,out var sessionId))throw new ArgumentException("A UUID is required");
 // Safe identity projection: no handle hash, security stamp, password or bearer token.
 await using var c=new SqlConnection(cs);await c.OpenAsync();
 await using var q=new SqlCommand("SELECT Id,UserId,DeviceLabel,CreatedAt,RevokedAt FROM [identity].[Session] WHERE Id=@id FOR JSON PATH, INCLUDE_NULL_VALUES",c);
 q.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=sessionId;
 await using var reader=await q.ExecuteReaderAsync();var output=new System.Text.StringBuilder();while(await reader.ReadAsync())output.Append(reader.GetString(0));
 Console.WriteLine(output.Length==0?"[]":output.ToString());return;
}
if(args is ["read-profile",var profileIdText]) {
 if(!Guid.TryParse(profileIdText,out var profileId))throw new ArgumentException("A UUID is required");
 // Explicit safe projection: never select password hashes, security stamps, tokens or MFA data.
 await using var c=new SqlConnection(cs);await c.OpenAsync();
 await using var q=new SqlCommand("SELECT Id,DisplayName,TimeZoneId,Locale,State FROM [identity].[User] WHERE Id=@id FOR JSON PATH",c);
 q.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=profileId;
 await using var reader=await q.ExecuteReaderAsync();var output=new System.Text.StringBuilder();while(await reader.ReadAsync())output.Append(reader.GetString(0));
 Console.WriteLine(output.Length==0?"[]":output.ToString());return;
}
if(args is ["read-time-report-series",var ownerText]) {
 if(!Guid.TryParse(ownerText,out var owner))throw new ArgumentException("A UUID is required");
 await using var c=new SqlConnection(cs);await c.OpenAsync();
 await using var q=new SqlCommand("SELECT Id,StartAt,EndAt FROM [time].[Entry] WHERE OwnerId=@owner AND Status='Stopped' ORDER BY Id FOR JSON PATH",c);
 q.Parameters.AddWithValue("@owner",owner);
 await using var r=await q.ExecuteReaderAsync();var output=new System.Text.StringBuilder();while(await r.ReadAsync())output.Append(r.GetString(0));
 Console.WriteLine(output.Length==0?"[]":output.ToString());return;
}
if(args is ["read-focus-completion",var focusText]) {
 if(!Guid.TryParse(focusText,out var focusId))throw new ArgumentException("A UUID is required");
 await using var c=new SqlConnection(cs);await c.OpenAsync();
 await using var q=new SqlCommand("SELECT n.Id,n.OwnerUserId,n.LogicalKey,JSON_QUERY((SELECT d.Channel,d.State,d.Attempts,d.LastErrorCode FROM [notifications].[Delivery] d WHERE d.NotificationId=n.Id ORDER BY d.Channel FOR JSON PATH)) AS deliveries FROM [notifications].[Notification] n WHERE n.LogicalKey=@key FOR JSON PATH",c);
 q.Parameters.AddWithValue("@key",$"focus.completed:{focusId:N}");
 await using var r=await q.ExecuteReaderAsync();var output=new System.Text.StringBuilder();while(await r.ReadAsync())output.Append(r.GetString(0));
 Console.WriteLine(output.Length==0?"[]":output.ToString());return;
}
if(args is ["read-resource",var kind,var idText]) {
 if(!Guid.TryParse(idText,out var id))throw new ArgumentException("A UUID is required");
 var tables=new Dictionary<string,string> {
  ["WishlistItem"]="[shopping].[WishlistItem]",
  ["Skill"]="[learning].[Skill]",
  ["PersonalAsset"]="[assets].[PersonalAsset]",["AssetVersion"]="[assets].[AssetVersion]",
  ["Course"]="[learning].[Course]",["CourseMilestone"]="[learning].[CourseMilestone]",
  ["TimeEntry"]="[time].[Entry]",["FocusSession"]="[time].[FocusSession]",["Notification"]="[notifications].[Notification]",["Bookmark"]="[knowledge].[Bookmark]",["Tag"]="[organization].[Tag]",["Category"]="[finance].[ManualCategory]",["Record"]="[finance].[ManualRecord]",
  ["Project"]="[productivity].[Project]",["Task"]="[productivity].[Task]",["Event"]="[calendar].[Event]",["Goal"]="[productivity].[Goal]",["GoalTarget"]="[productivity].[GoalTarget]",
  ["Habit"]="[productivity].[Habit]",["HabitCheckIn"]="[productivity].[HabitCheckIn]",["HabitSchedule"]="[productivity].[HabitSchedule]",["PlannerPin"]="[productivity].[PlannerPin]",
  ["Reminder"]="[calendar].[Reminder]",["Document"]="[documents].[Page]",["Snippet"]="[knowledge].[Snippet]",["ReadingItem"]="[knowledge].[ReadingItem]",["Favorite"]="[discovery].[Favorite]",["Preference"]="[platform].[Preference]"
 };
 if(!tables.TryGetValue(kind,out var table))throw new ArgumentException("Resource type is not allowed");
 await using var c=new SqlConnection(cs);await c.OpenAsync();
 var query=kind=="Notification"
  ? "SELECT Id,OwnerUserId,Title,ReadAt,DeletedAt FROM [notifications].[Notification] WHERE Id=@id FOR JSON PATH, INCLUDE_NULL_VALUES"
  : kind=="Snippet"
  ? "SELECT s.*,v.SourceText AS Body FROM [knowledge].[Snippet] s JOIN [knowledge].[SnippetVersion] v ON v.SnippetId=s.Id AND v.OwnerId=s.OwnerId AND v.VersionNumber=s.CurrentVersion WHERE s.Id=@id FOR JSON PATH"
  : $"SELECT * FROM {table} WHERE Id=@id FOR JSON PATH";
 await using var q=new SqlCommand(query,c);q.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=id;
 await using var reader=await q.ExecuteReaderAsync();var output=new System.Text.StringBuilder();while(await reader.ReadAsync())output.Append(reader.GetString(0));
 Console.WriteLine(output.Length==0?"[]":output.ToString());return;
}
if(args is ["migrate"]) {
 await new SqlMigrationRunner().ApplyAsync(cs,Environment.GetEnvironmentVariable("NEXORA_MIGRATIONS_DIR")!,M01MigrationManifest.RequiredFileNames);
 Console.WriteLine($"Applied {M01MigrationManifest.RequiredFileNames.Count} approved migrations.");return;
}
if(args is not ["seed"])throw new ArgumentException("Use seed, migrate, inventory, catalog, read-access UUID, read-session UUID, read-profile UUID, or read-resource TYPE UUID");
b.InitialCatalog="master";
await using(var c=new SqlConnection(b.ConnectionString)){await c.OpenAsync();await using var q=new SqlCommand($"IF DB_ID(@db) IS NULL CREATE DATABASE [{db}]",c);q.Parameters.AddWithValue("@db",db);await q.ExecuteNonQueryAsync();}
await new SqlMigrationRunner().ApplyAsync(cs,Environment.GetEnvironmentVariable("NEXORA_MIGRATIONS_DIR")!,M01MigrationManifest.RequiredFileNames);
var hasher=new Pbkdf2PasswordHasher();var accounts=new List<object>();
await using var conn=new SqlConnection(cs);await conn.OpenAsync();
foreach(var role in new[]{"SuperAdmin","UserA","UserB","Admin","AdminReader","Restricted","Disabled","Suspended"}){
 var email=$"qa-{role.ToLowerInvariant()}-{db[^8..]}@example.invalid";var password="Nx!7"+Convert.ToHexString(RandomNumberGenerator.GetBytes(18));var hash=hasher.Hash(password);
 if(role=="SuperAdmin")await new SqlBootstrapSuperAdmin(cs).ExecuteAsync(new SqlBootstrapSuperAdminCommand{Email=email,DisplayName="QA SuperAdmin",TimeZoneId="Asia/Ho_Chi_Minh",PasswordHash=hash});
 else{
 await using var cmd=new SqlCommand("""
 INSERT [identity].[User](Id,Email,NormalizedEmail,PasswordHash,SecurityStamp,State,EmailConfirmed,VerifiedAt,DisplayName,TimeZoneId,Locale)
 VALUES(@u,@email,@email,@hash,@stamp,@state,1,SYSUTCDATETIME(),@name,'Asia/Ho_Chi_Minh','vi');
 INSERT [platform].[PersonalSpace](Id,UserId) VALUES(@owner,@u);
 INSERT [identity].[UserRole](UserId,RoleId) SELECT @u,Id FROM [identity].[Role] WHERE Code='User' OR Code=@role;
 INSERT [platform].[UserModuleGrant](UserId,ModuleId,Enabled) SELECT @u,Id,1 FROM [platform].[Module] WHERE State='Ready' AND SystemEnabled=1 AND RegistrationEnabled=1;
 IF @reader=1 INSERT [platform].[AdminPermission](UserId,PermissionId,Effect) SELECT @u,Id,'Allow' FROM [platform].[Permission] WHERE ActionKey IN('access.user.read','access.permission.read','modules.catalog.read','settings.preference.read','settings.preference.update') AND EffectiveStatus='Resolved';
 IF @restricted=1 UPDATE g SET Enabled=0 FROM [platform].[UserModuleGrant] g JOIN [platform].[Module] m ON m.Id=g.ModuleId WHERE g.UserId=@u AND m.Code='FX11';
 IF @suspended=1 UPDATE [platform].[PersonalSpace] SET State='Suspended' WHERE UserId=@u;
 """,conn);
 cmd.Parameters.AddWithValue("@u",Guid.NewGuid());cmd.Parameters.AddWithValue("@owner",Guid.NewGuid());cmd.Parameters.AddWithValue("@email",email);cmd.Parameters.AddWithValue("@hash",hash);cmd.Parameters.AddWithValue("@stamp",Guid.NewGuid().ToString("N"));cmd.Parameters.AddWithValue("@state",role=="Disabled"?"Disabled":"Active");cmd.Parameters.AddWithValue("@name","QA "+role);cmd.Parameters.AddWithValue("@role",role.StartsWith("Admin")?"Admin":"User");cmd.Parameters.AddWithValue("@reader",role=="AdminReader");cmd.Parameters.AddWithValue("@restricted",role=="Restricted");cmd.Parameters.AddWithValue("@suspended",role=="Suspended");await cmd.ExecuteNonQueryAsync();
 }
 accounts.Add(new{role,email,password});
}
var path=Environment.GetEnvironmentVariable("NEXORA_E2E_ACCOUNTS")!;
await File.WriteAllTextAsync(path,JsonSerializer.Serialize(accounts));if(!OperatingSystem.IsWindows())File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite);
Console.WriteLine($"Applied {M01MigrationManifest.RequiredFileNames.Count} migrations; created {accounts.Count} synthetic password accounts.");
