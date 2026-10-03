/* M01-S08 preview authorization requires its dedicated, resolved SUPER action.
   Register metadata only; do not create any AdminPermission grant. */
SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM [platform].[Permission] WITH (UPDLOCK, HOLDLOCK) WHERE [ActionKey] = N'access.change.read')
    INSERT INTO [platform].[Permission] ([ActionKey], [EffectiveStatus])
        VALUES (N'access.change.read', 'Resolved');
COMMIT TRANSACTION;
GO
