SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/*
   FX32-S01/S02 local pure toolbox actions. Network actions, code execution,
   provider calls and opted-in history storage are intentionally not enabled
   by this migration.
*/
IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES
        (N'toolbox.catalog.read'),
        (N'toolbox.base64.run'),
        (N'toolbox.url_codec.run'),
        (N'toolbox.html_codec.run'),
        (N'toolbox.hash.run'),
        (N'toolbox.uuid.run'),
        (N'toolbox.password.run'),
        (N'toolbox.json.run'),
        (N'toolbox.regex.run')
    ) AS source ([ActionKey])
    ON target.[ActionKey] = source.[ActionKey]
    WHEN MATCHED AND target.[EffectiveStatus] <> 'Resolved' THEN
        UPDATE SET [EffectiveStatus] = 'Resolved'
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], 'Resolved');
END
GO

COMMIT TRANSACTION;
GO
