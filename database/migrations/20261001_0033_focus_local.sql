SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('[time].[FocusPreference]') IS NULL
 CREATE TABLE [time].[FocusPreference](
  OwnerId uniqueidentifier NOT NULL PRIMARY KEY REFERENCES [platform].[PersonalSpace](Id),
  FocusMinutes int NOT NULL CHECK(FocusMinutes BETWEEN 1 AND 180),
  ShortBreakMinutes int NOT NULL CHECK(ShortBreakMinutes BETWEEN 1 AND 60),
  LongBreakMinutes int NOT NULL CHECK(LongBreakMinutes BETWEEN 1 AND 120),
  CycleLength int NOT NULL CHECK(CycleLength BETWEEN 1 AND 12),RowVersion rowversion NOT NULL);
IF OBJECT_ID('[time].[FocusSession]') IS NULL
BEGIN
 CREATE TABLE [time].[FocusSession](
  Id uniqueidentifier NOT NULL PRIMARY KEY,
  OwnerId uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace](Id),
  Phase varchar(16) NOT NULL CHECK(Phase IN('Focus','ShortBreak','LongBreak')),
  State varchar(16) NOT NULL CHECK(State IN('Running','Paused','Completed','Cancelled')),
  IsActive bit NOT NULL, PlannedSeconds int NOT NULL CHECK(PlannedSeconds BETWEEN 60 AND 10800),
  ElapsedMilliseconds bigint NOT NULL, StartedAt datetime2(7) NOT NULL,
  LastRunAt datetime2(7) NULL,CompletedAt datetime2(7) NULL,RowVersion rowversion NOT NULL,
  CompletionFailures int NOT NULL DEFAULT 0 CHECK(CompletionFailures BETWEEN 0 AND 5),
  NextCompletionAttemptAt datetime2(7) NULL,
  CONSTRAINT CK_Focus_Elapsed CHECK(ElapsedMilliseconds>=0 AND ElapsedMilliseconds<=CAST(PlannedSeconds AS bigint)*1000),
  CONSTRAINT CK_Focus_Active CHECK((IsActive=1 AND State IN('Running','Paused')) OR (IsActive=0 AND State IN('Completed','Cancelled'))),
  CONSTRAINT CK_Focus_Run CHECK((State='Running' AND LastRunAt IS NOT NULL) OR (State<>'Running' AND LastRunAt IS NULL)),
  CONSTRAINT UQ_Focus_Owner UNIQUE(OwnerId,Id));
 CREATE UNIQUE INDEX UX_Focus_Active ON [time].[FocusSession](OwnerId) WHERE IsActive=1;
 CREATE INDEX IX_Focus_Page ON [time].[FocusSession](OwnerId,StartedAt DESC,Id DESC);
END;
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT s.ActionKey,'Resolved' FROM (VALUES('focus.session.read'),('focus.session.start'),
 ('focus.session.pause'),('focus.session.resume'),('focus.session.cancel'),('focus.preference.update'))s(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
UPDATE [platform].[Module] SET State='Ready',SystemEnabled=1,RegistrationEnabled=1 WHERE Code='FX19';
COMMIT;
