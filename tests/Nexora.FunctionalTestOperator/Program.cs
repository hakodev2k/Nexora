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
if(args is ["read-resource",var kind,var idText]) {
 if(!Guid.TryParse(idText,out var id))throw new ArgumentException("A UUID is required");
 var tables=new Dictionary<string,string> {
  ["Bookmark"]="[knowledge].[Bookmark]",["Tag"]="[organization].[Tag]",["Category"]="[finance].[ManualCategory]",["Record"]="[finance].[ManualRecord]",
  ["Project"]="[productivity].[Project]",["Task"]="[productivity].[Task]",["Event"]="[calendar].[Event]",["Goal"]="[productivity].[Goal]",["GoalTarget"]="[productivity].[GoalTarget]",
  ["Habit"]="[productivity].[Habit]",["HabitCheckIn"]="[productivity].[HabitCheckIn]",["HabitSchedule"]="[productivity].[HabitSchedule]",["PlannerPin"]="[productivity].[PlannerPin]",
  ["Reminder"]="[calendar].[Reminder]",["Document"]="[documents].[Page]",["Snippet"]="[knowledge].[Snippet]",["ReadingItem"]="[knowledge].[ReadingItem]",["Favorite"]="[discovery].[Favorite]"
 };
 if(!tables.TryGetValue(kind,out var table))throw new ArgumentException("Resource type is not allowed");
 await using var c=new SqlConnection(cs);await c.OpenAsync();
 await using var q=new SqlCommand($"SELECT * FROM {table} WHERE Id=@id FOR JSON PATH",c);q.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=id;
 await using var reader=await q.ExecuteReaderAsync();var output=new System.Text.StringBuilder();while(await reader.ReadAsync())output.Append(reader.GetString(0));
 Console.WriteLine(output.Length==0?"[]":output.ToString());return;
}
if(args is ["migrate"]) {
 await new SqlMigrationRunner().ApplyAsync(cs,Environment.GetEnvironmentVariable("NEXORA_MIGRATIONS_DIR")!,M01MigrationManifest.RequiredFileNames);
 Console.WriteLine($"Applied {M01MigrationManifest.RequiredFileNames.Count} approved migrations.");return;
}
if(args is not ["seed"])throw new ArgumentException("Use seed, migrate, inventory, or read-resource TYPE UUID");
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
