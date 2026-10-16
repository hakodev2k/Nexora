SET XACT_ABORT ON;
BEGIN TRANSACTION;
-- Register only the implemented owner history QUERY. Existing non-Resolved status remains denied.
-- No Admin Allow/Deny, user/module grant, personal data, or lifecycle row is modified.
INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus])
SELECT N'documents.page.history', 'Resolved'
WHERE NOT EXISTS (SELECT 1 FROM [platform].[Permission] WITH (UPDLOCK, HOLDLOCK) WHERE [ActionKey]=N'documents.page.history');
COMMIT TRANSACTION;
GO
