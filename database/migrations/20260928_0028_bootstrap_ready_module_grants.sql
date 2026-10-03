/*
   Bootstrap users predate the verified-user grant path. Give each active
   SuperAdmin the same approved local-ready module defaults, without touching
   disabled, paused, or registration-ineligible modules.
*/
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [platform].[UserModuleGrant] ([UserId], [ModuleId], [Enabled], [CreatedAt], [UpdatedAt])
SELECT [user].[Id], [module].[Id], CAST(1 AS bit), SYSUTCDATETIME(), SYSUTCDATETIME()
FROM [identity].[User] AS [user]
INNER JOIN [identity].[UserRole] AS [assignment] ON [assignment].[UserId] = [user].[Id]
INNER JOIN [identity].[Role] AS [role] ON [role].[Id] = [assignment].[RoleId] AND [role].[Code] = 'SuperAdmin'
CROSS JOIN [platform].[Module] AS [module]
WHERE [user].[State] = 'Active'
  AND [user].[IsDeleted] = 0
  AND [module].[State] = 'Ready'
  AND [module].[SystemEnabled] = 1
  AND [module].[RegistrationEnabled] = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM [platform].[UserModuleGrant] AS [grant]
      WHERE [grant].[UserId] = [user].[Id]
        AND [grant].[ModuleId] = [module].[Id]
  );
GO

COMMIT TRANSACTION;
GO
