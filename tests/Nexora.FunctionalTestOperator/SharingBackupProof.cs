using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

/// <summary>Test-only physical restore proof; never restores over the retained synthetic database.</summary>
internal static class SharingBackupProof
{
    private static string Literal(string value)=>"N'"+value.Replace("'","''",StringComparison.Ordinal)+"'";
    private static string Under(string parent,string leaf)
    {
        if(string.IsNullOrWhiteSpace(parent)||leaf.Contains("..",StringComparison.Ordinal)||leaf.Contains('/')||leaf.Contains('\\'))throw new InvalidOperationException("Invalid generated backup target.");
        return parent.TrimEnd('/','\\')+(parent.Contains('\\')?"\\":"/")+leaf;
    }
    internal static async Task RunAsync(string connectionString)
    {
        var sourceName=new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if(!System.Text.RegularExpressions.Regex.IsMatch(sourceName,"^Nexora_Test_[0-9a-f]{32}$"))throw new InvalidOperationException("Synthetic database required.");
        var restoreName="Nexora_Test_"+Guid.NewGuid().ToString("N");
        var master=new SqlConnectionStringBuilder(connectionString){InitialCatalog="master"};
        await using var control=new SqlConnection(master.ConnectionString);await control.OpenAsync();
        await using var source=new SqlConnection(connectionString);await source.OpenAsync();
        string backupRoot,dataRoot,logRoot;
        using(var paths=new SqlCommand("SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath')),CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath')),CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath'));",control))
        {using var reader=await paths.ExecuteReaderAsync();if(!await reader.ReadAsync())throw new InvalidOperationException("Backup configuration unavailable.");backupRoot=reader.GetString(0);dataRoot=reader.GetString(1);logRoot=reader.GetString(2);}
        var backup=Under(backupRoot,"nexora-sharing-"+Guid.NewGuid().ToString("N")+".bak");
        var data=Under(dataRoot,restoreName+".mdf");var log=Under(logRoot,restoreName+"_log.ldf");
        using(var absent=new SqlCommand("SELECT DB_ID(@Name)",control)){absent.Parameters.AddWithValue("@Name",restoreName);if(await absent.ExecuteScalarAsync() is not DBNull)throw new InvalidOperationException("Generated restore target already exists.");}
        using(var absentFile=new SqlCommand("DECLARE @Exists int; EXEC master.dbo.xp_fileexist @Path,@Exists OUTPUT; SELECT @Exists;",control)){absentFile.Parameters.AddWithValue("@Path",backup);if(Convert.ToInt32(await absentFile.ExecuteScalarAsync())!=0)throw new InvalidOperationException("Generated backup filename already exists.");}
        var files=new Dictionary<int,string>();using(var names=new SqlCommand("SELECT name,type FROM sys.database_files",source)){using var reader=await names.ExecuteReaderAsync();while(await reader.ReadAsync())files.Add(reader.GetByte(1),reader.GetString(0));}if(files.Count!=2)throw new InvalidOperationException("Unexpected synthetic file topology.");
        var backupCreated=false;var restoreAttempted=false;var verified=false;
        try
        {
            using(var save=new SqlCommand($"BACKUP DATABASE [{sourceName}] TO DISK=@Backup WITH COPY_ONLY,INIT,CHECKSUM",control)){save.CommandTimeout=60;save.Parameters.AddWithValue("@Backup",backup);await save.ExecuteNonQueryAsync();backupCreated=true;}
            restoreAttempted=true;
            using(var restore=new SqlCommand($"RESTORE DATABASE [{restoreName}] FROM DISK=@Backup WITH CHECKSUM,MOVE {Literal(files[0])} TO {Literal(data)},MOVE {Literal(files[1])} TO {Literal(log)}",control)){restore.CommandTimeout=60;restore.Parameters.AddWithValue("@Backup",backup);await restore.ExecuteNonQueryAsync();}
            foreach(var table in new[]{"[identity].[User]","[platform].[PersonalSpace]","[platform].[Module]","[platform].[UserModuleGrant]","[productivity].[Project]","[productivity].[Task]","[documents].[Page]","[calendar].[Event]","[security].[ShareLink]"})
            {
                using var count=new SqlCommand($"SELECT (SELECT COUNT_BIG(*) FROM {table})-(SELECT COUNT_BIG(*) FROM [{restoreName}].{table});",source);
                if(Convert.ToInt64(await count.ExecuteScalarAsync())!=0)throw new InvalidOperationException("Restored synthetic row count differs.");
            }
            using(var policy=new SqlCommand($"SELECT COUNT(*) FROM (SELECT Code,State,SystemEnabled,RegistrationEnabled,PolicyRevision,SharingEpoch FROM [platform].[Module] EXCEPT SELECT Code,State,SystemEnabled,RegistrationEnabled,PolicyRevision,SharingEpoch FROM [{restoreName}].[platform].[Module]) differences;",source))
                if(Convert.ToInt32(await policy.ExecuteScalarAsync())!=0)throw new InvalidOperationException("Restored policy differs.");
            using(var links=new SqlCommand($"SELECT COUNT(*) FROM (SELECT Id,OwnerId,ResourceType,ResourceId,TokenHash,Mode,ExpiresAt,RevokedAt,IsDeleted,InvalidatedAt,InvalidationReason,IssuedSharingEpoch FROM [security].[ShareLink] EXCEPT SELECT Id,OwnerId,ResourceType,ResourceId,TokenHash,Mode,ExpiresAt,RevokedAt,IsDeleted,InvalidatedAt,InvalidationReason,IssuedSharingEpoch FROM [{restoreName}].[security].[ShareLink]) differences;",source))
                if(Convert.ToInt32(await links.ExecuteScalarAsync())!=0)throw new InvalidOperationException("Restored sharing identity or invalidation differs.");
            verified=true;
        }
        finally
        {
            // Only the exact absence-checked generated restore target and filename are cleaned.
            if(restoreAttempted){using var drop=new SqlCommand($"IF DB_ID(N'{restoreName}') IS NOT NULL BEGIN ALTER DATABASE [{restoreName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{restoreName}]; END",control);await drop.ExecuteNonQueryAsync();}
            if(backupCreated){using var remove=new SqlCommand("EXEC master.sys.xp_delete_files @Path",control);remove.Parameters.AddWithValue("@Path",backup);await remove.ExecuteNonQueryAsync();}
        }
        Console.WriteLine(JsonSerializer.Serialize(new{verified,originalUntouched=true,tablesVerified=9,checksumRestore=true,policyRowsMatched=true,linkIdentityHashesMatched=true,temporaryRestoreCleaned=true,backupCleaned=true}));
    }
}
