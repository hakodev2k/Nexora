SET XACT_ABORT ON;
BEGIN TRANSACTION;
/* Installed subset only. Preserve all policy bits, grants and capabilities. */
IF EXISTS(SELECT 1 FROM [platform].[Module] WITH(UPDLOCK,HOLDLOCK)
 WHERE Code='FX04' AND State='Blocked' AND SystemEnabled=1 AND SharingEnabled=1)
 THROW 51044,'Sharing readiness requires explicit policy activation.',1;
UPDATE [platform].[Module]
SET Name=N'Read-only Sharing — Project/Document local subset',State='Ready',
 PolicyRevision=PolicyRevision+1,UpdatedAt=SYSUTCDATETIME()
WHERE Code='FX04' AND State='Blocked';
COMMIT;