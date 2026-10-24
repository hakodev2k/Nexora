SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
GO
-- Definitions only: preserve every existing status and every actor/module grant.
-- A catalog entry or definition does not grant Admin execution authority.
INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus])
SELECT source.[ActionKey], 'Resolved'
FROM (VALUES
    (N'toolbox.text_diff.run'),
    (N'toolbox.color.run')
) source ([ActionKey])
WHERE NOT EXISTS (
    SELECT 1 FROM [platform].[Permission] existing WITH (UPDLOCK, HOLDLOCK)
    WHERE existing.[ActionKey] = source.[ActionKey]
);
GO
COMMIT TRANSACTION;
GO
