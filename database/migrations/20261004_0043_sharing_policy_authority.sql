SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

IF COL_LENGTH(N'platform.Module', N'SharingEnabled') IS NULL
    ALTER TABLE [platform].[Module] ADD [SharingEnabled] bit NULL;
GO

/* Install the independent policy trigger before backfill. The old trigger
   must never observe a registration-default update during this transition. */
CREATE OR ALTER TRIGGER [platform].[TR_ShareLink_ModuleInvalidation]
ON [platform].[Module]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

    UPDATE link
    SET [IsDeleted] = 1,
        [RevokedAt] = COALESCE(link.[RevokedAt], SYSUTCDATETIME()),
        [InvalidatedAt] = COALESCE(link.[InvalidatedAt], SYSUTCDATETIME()),
        [InvalidationReason] = COALESCE(link.[InvalidationReason], 'SharingDisabled'),
        [UpdatedAt] = SYSUTCDATETIME()
    FROM [security].[ShareLink] link
    INNER JOIN inserted moduleRow
      ON moduleRow.[Code] = 'FX04'
      OR (moduleRow.[Code] = 'FX11' AND link.[ResourceType] = 'Project')
      OR (moduleRow.[Code] = 'FX20' AND link.[ResourceType] = 'Document')
    WHERE link.[IsDeleted] = 0
      AND (moduleRow.[State] <> 'Ready' OR moduleRow.[SystemEnabled] <> 1 OR moduleRow.[SharingEnabled] = 0);

    /* Advance once on a true->false sharing transition. A nested update has
       false->false and cannot advance again; no reset or source epoch column. */
    UPDATE moduleRow
    SET [SharingEpoch] = moduleRow.[SharingEpoch] + 1
    FROM [platform].[Module] moduleRow
    INNER JOIN inserted currentRow ON currentRow.[Id] = moduleRow.[Id]
    INNER JOIN deleted previousRow ON previousRow.[Id] = currentRow.[Id]
    WHERE previousRow.[SharingEnabled] = 1 AND currentRow.[SharingEnabled] = 0;
END
GO

/* PO carryforward preserves the former effective policy only for reviewed
   providers; existing tokens and permanent invalidations are never rewritten. */
UPDATE [platform].[Module]
SET [SharingEnabled] = CASE WHEN [Code] IN ('FX04','FX11','FX20') THEN [RegistrationEnabled] ELSE CONVERT(bit,0) END
WHERE [SharingEnabled] IS NULL;
GO

ALTER TABLE [platform].[Module] ALTER COLUMN [SharingEnabled] bit NOT NULL;
GO

/* No default: future module installation must supply an explicit policy. */
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WHERE [ActionKey] = N'modules.policy.sharing')
    INSERT [platform].[Permission] ([ActionKey],[EffectiveStatus]) VALUES (N'modules.policy.sharing','Resolved');
GO
MERGE [platform].[Permission] AS target
USING (VALUES (N'projects.project.share'), (N'documents.page.share')) source(ActionKey)
ON target.ActionKey=source.ActionKey
WHEN NOT MATCHED THEN INSERT(ActionKey,EffectiveStatus) VALUES(source.ActionKey,'Resolved');
GO
COMMIT TRANSACTION;
GO
