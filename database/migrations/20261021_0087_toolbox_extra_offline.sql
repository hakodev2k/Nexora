SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
GO
-- Add only reviewed offline SELF action definitions. Never promote an existing
-- Paused/Blocked/Superseded row, change user/module grants or grant Admin access.
INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus])
SELECT source.[ActionKey], 'Resolved'
FROM (VALUES
    (N'toolbox.xml.run'),
    (N'toolbox.datetime.run'),
    (N'toolbox.csv.run'),
    (N'toolbox.jwt.run')
) source ([ActionKey])
WHERE NOT EXISTS (
    SELECT 1 FROM [platform].[Permission] existing WITH (UPDLOCK, HOLDLOCK)
    WHERE existing.[ActionKey] = source.[ActionKey]
);
GO
COMMIT TRANSACTION;
GO
