SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/*
   The R1 catalog is broader than the local implementation slice. Keep the
   catalog rows for traceability, but only expose modules that have source,
   persistence, authorization and local UI/runtime coverage in this build.
   FX30/FX34/FX35 remain deliberately paused; no real provider execution is
   enabled by this migration.
*/
IF OBJECT_ID(N'[platform].[Module]', N'U') IS NOT NULL
BEGIN
    DECLARE @RuntimeCatalog TABLE
    (
        [Code] varchar(64) NOT NULL PRIMARY KEY,
        [State] varchar(32) NOT NULL,
        [SystemEnabled] bit NOT NULL,
        [RegistrationEnabled] bit NOT NULL
    );

    INSERT INTO @RuntimeCatalog ([Code], [State], [SystemEnabled], [RegistrationEnabled])
    VALUES
        ('FX01', 'Ready', 1, 1),
        ('FX02', 'Ready', 1, 1),
        ('FX03', 'Ready', 1, 1),
        ('FX04', 'Blocked', 0, 0),
        ('FX05', 'Blocked', 0, 0),
        ('FX06', 'Ready', 1, 1),
        ('FX07', 'Blocked', 0, 0),
        ('FX08', 'Ready', 1, 1),
        ('FX09', 'Ready', 1, 1),
        ('FX10', 'Blocked', 0, 0),
        ('FX11', 'Ready', 1, 1),
        ('FX12', 'Ready', 1, 1),
        ('FX13', 'Ready', 1, 1),
        ('FX14', 'Blocked', 0, 0),
        ('FX15', 'Blocked', 0, 0),
        ('FX16', 'Ready', 1, 1),
        ('FX17', 'Blocked', 0, 0),
        ('FX18', 'Blocked', 0, 0),
        ('FX19', 'Blocked', 0, 0),
        ('FX20', 'Ready', 1, 1),
        ('FX21', 'Ready', 1, 1),
        ('FX22', 'Ready', 1, 1),
        ('FX23', 'Ready', 1, 1),
        ('FX24', 'Ready', 1, 1),
        ('FX25', 'Ready', 1, 1),
        ('FX26', 'Ready', 1, 1),
        ('FX27', 'Ready', 1, 1),
        ('FX28', 'Blocked', 0, 0),
        ('FX29', 'Blocked', 0, 0),
        ('FX30', 'Paused', 0, 0),
        ('FX31', 'Blocked', 0, 0),
        ('FX32', 'Ready', 1, 1),
        ('FX33', 'Blocked', 0, 0),
        ('FX34', 'Paused', 0, 0),
        ('FX35', 'Paused', 0, 0),
        ('FX36', 'Blocked', 0, 0),
        ('FX37', 'Blocked', 0, 0),
        ('FX38', 'Blocked', 0, 0),
        ('FX39', 'Blocked', 0, 0),
        ('FX40', 'Blocked', 0, 0);

    UPDATE moduleRow
    SET [State] = catalog.[State],
        [SystemEnabled] = catalog.[SystemEnabled],
        [RegistrationEnabled] = catalog.[RegistrationEnabled],
        [UpdatedAt] = SYSUTCDATETIME()
    FROM [platform].[Module] moduleRow
    INNER JOIN @RuntimeCatalog catalog ON catalog.[Code] = moduleRow.[Code]
    WHERE moduleRow.[State] <> catalog.[State]
       OR moduleRow.[SystemEnabled] <> catalog.[SystemEnabled]
       OR moduleRow.[RegistrationEnabled] <> catalog.[RegistrationEnabled];

    /* Existing grants are retained for audit/history but cannot keep a gated
       module usable after the catalog correction. */
    UPDATE grantRow
    SET [Enabled] = 0,
        [UpdatedAt] = SYSUTCDATETIME()
    FROM [platform].[UserModuleGrant] grantRow
    INNER JOIN [platform].[Module] moduleRow ON moduleRow.[Id] = grantRow.[ModuleId]
    INNER JOIN @RuntimeCatalog catalog ON catalog.[Code] = moduleRow.[Code]
    WHERE catalog.[State] <> 'Ready'
       OR catalog.[SystemEnabled] <> 1
       OR catalog.[RegistrationEnabled] <> 1;
END
GO

COMMIT TRANSACTION;
GO
