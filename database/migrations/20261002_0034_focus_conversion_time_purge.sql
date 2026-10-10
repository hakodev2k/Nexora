SET XACT_ABORT ON;
BEGIN TRANSACTION;
-- Time and Focus share the existing time persistence boundary. The conversion
-- pin is durable; deleting a receipt cannot make a session convertible twice.
IF OBJECT_ID('[time].[FocusConversion]', 'U') IS NULL
BEGIN
 CREATE TABLE [time].[FocusConversion](
  OwnerId uniqueidentifier NOT NULL,
  SessionId uniqueidentifier NOT NULL,
  EntryId uniqueidentifier NOT NULL,
  CreatedAt datetime2(7) NOT NULL CONSTRAINT DF_FocusConversion_Created DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_FocusConversion PRIMARY KEY(OwnerId,SessionId),
  CONSTRAINT UQ_FocusConversion_Entry UNIQUE(OwnerId,EntryId),
  CONSTRAINT FK_FocusConversion_Session FOREIGN KEY(OwnerId,SessionId) REFERENCES [time].[FocusSession](OwnerId,Id),
  CONSTRAINT FK_FocusConversion_Entry FOREIGN KEY(OwnerId,EntryId) REFERENCES [time].[Entry](OwnerId,Id));
END;
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES('focus.session.record_time'),('time.entry.purge'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
COMMIT;
