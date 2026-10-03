SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF SCHEMA_ID('time') IS NULL EXEC('CREATE SCHEMA [time]');
IF OBJECT_ID('[time].[Entry]') IS NULL
BEGIN
 CREATE TABLE [time].[Entry](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_TimeEntry PRIMARY KEY,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  StartAt datetime2(7) NOT NULL, EndAt datetime2(7) NULL,
  Description nvarchar(2000) NULL, Category nvarchar(200) NULL,
  Status varchar(16) NOT NULL, UpdatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
  RowVersion rowversion NOT NULL,
  CONSTRAINT UQ_TimeEntry_Owner UNIQUE(OwnerId,Id),
  CONSTRAINT CK_TimeEntry_State CHECK(
   (Status='Running' AND EndAt IS NULL) OR
   (Status IN ('Stopped','Trash') AND EndAt IS NOT NULL AND EndAt>StartAt)));
 CREATE UNIQUE INDEX UX_TimeEntry_Running ON [time].[Entry](OwnerId) WHERE Status='Running';
 CREATE INDEX IX_TimeEntry_Page ON [time].[Entry](OwnerId,Id DESC) INCLUDE(Status,StartAt,EndAt);
END;
IF OBJECT_ID('[time].[Correction]') IS NULL
BEGIN
 CREATE TABLE [time].[Correction](
  Id uniqueidentifier NOT NULL CONSTRAINT PK_TimeCorrection PRIMARY KEY,
  OwnerId uniqueidentifier NOT NULL, EntryId uniqueidentifier NOT NULL,
  Action nvarchar(160) NOT NULL, At datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
  BeforeJson nvarchar(max) NOT NULL CHECK(ISJSON(BeforeJson)=1),
  CONSTRAINT FK_TimeCorrection_Entry FOREIGN KEY(OwnerId,EntryId) REFERENCES [time].[Entry](OwnerId,Id));
 CREATE INDEX IX_TimeCorrection_Page ON [time].[Correction](OwnerId,EntryId,Id DESC);
END;
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES
 ('time.timer.read'),('time.timer.start'),('time.timer.stop'),('time.timer.resume'),
 ('time.entry.read'),('time.entry.create'),('time.entry.update'),('time.entry.trash'),
 ('time.entry.restore'),('time.entry.history'),('time.report.read'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
-- Existing user entitlements are deliberately preserved. New registrations use this default.
UPDATE [platform].[Module] SET State='Ready',SystemEnabled=1,RegistrationEnabled=1 WHERE Code='FX18';
COMMIT;
