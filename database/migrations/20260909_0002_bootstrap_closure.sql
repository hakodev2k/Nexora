SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH(N'platform.SecurityInvariant', N'BootstrapCompletedAt') IS NULL
    ALTER TABLE [platform].[SecurityInvariant] ADD [BootstrapCompletedAt] datetime2(7) NULL;
GO
UPDATE [platform].[SecurityInvariant]
SET [BootstrapCompletedAt] = SYSUTCDATETIME()
WHERE [Id] = 1 AND [BootstrapCompletedAt] IS NULL
AND EXISTS (
    SELECT 1 FROM [identity].[UserRole] ur
    JOIN [identity].[Role] r ON r.Id = ur.RoleId
    WHERE r.Code = 'SuperAdmin'
);
COMMIT TRANSACTION;
GO
