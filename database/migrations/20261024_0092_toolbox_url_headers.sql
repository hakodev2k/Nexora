SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
GO
-- Add definitions only. Never promote an existing status or change actor grants.
INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus])
SELECT source.[ActionKey], 'Resolved'
FROM (VALUES
    (N'toolbox.url_parse.run'),
    (N'toolbox.headers.run')
) source ([ActionKey])
WHERE NOT EXISTS (
    SELECT 1 FROM [platform].[Permission] existing WITH (UPDLOCK, HOLDLOCK)
    WHERE existing.[ActionKey] = source.[ActionKey]
);
GO
COMMIT TRANSACTION;
GO
