SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT platform.Permission(ActionKey,EffectiveStatus)
SELECT ActionKey,'Resolved' FROM(VALUES(N'discovery.command.read'),(N'discovery.command.execute')) v(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM platform.Permission p WITH(UPDLOCK,HOLDLOCK) WHERE p.ActionKey=v.ActionKey);
COMMIT TRANSACTION;
GO
