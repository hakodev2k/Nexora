SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('automation.ScheduleVersion','U') IS NULL THROW 51052,'Schedule configuration must be installed first.',1;
IF COL_LENGTH('automation.Schedule','Enabled') IS NULL
 ALTER TABLE [automation].[Schedule] ADD Enabled bit NOT NULL CONSTRAINT DF_AutomationSchedule_Enabled DEFAULT 0,
 ActorUserId uniqueidentifier NULL REFERENCES [identity].[User](Id),SessionId uniqueidentifier NULL REFERENCES [identity].[Session](Id),
 ActorRole varchar(32) NULL,AuthenticatedAt datetime2(7) NULL;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_AutomationSchedule_Authority' AND parent_object_id=OBJECT_ID('automation.Schedule'))
 ALTER TABLE [automation].[Schedule] ADD CONSTRAINT CK_AutomationSchedule_Authority CHECK(Enabled=0 OR(ActorUserId IS NOT NULL AND SessionId IS NOT NULL AND ActorRole IS NOT NULL AND AuthenticatedAt IS NOT NULL));
IF OBJECT_ID('automation.ScheduleOccurrence','U') IS NULL
BEGIN
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_AutomationRun_Definition_Id' AND object_id=OBJECT_ID('automation.Run'))
  CREATE UNIQUE INDEX UX_AutomationRun_Definition_Id ON [automation].[Run](OwnerId,DefinitionId,Id);
 CREATE TABLE [automation].[ScheduleOccurrence](OwnerId uniqueidentifier NOT NULL,DefinitionId uniqueidentifier NOT NULL,
 ScheduleVersion bigint NOT NULL,DueAt datetime2(7) NOT NULL,EvaluatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
 State varchar(32) NOT NULL CHECK(State IN('Queued','Skipped')),Code varchar(64) NOT NULL,RunId uniqueidentifier NULL,
 CONSTRAINT PK_AutomationScheduleOccurrence PRIMARY KEY(OwnerId,DefinitionId,ScheduleVersion,DueAt),
 CONSTRAINT FK_AutomationOccurrence_ScheduleVersion FOREIGN KEY(OwnerId,DefinitionId,ScheduleVersion) REFERENCES [automation].[ScheduleVersion](OwnerId,DefinitionId,VersionNumber),
 CONSTRAINT FK_AutomationOccurrence_Run FOREIGN KEY(OwnerId,DefinitionId,RunId) REFERENCES [automation].[Run](OwnerId,DefinitionId,Id),
 CONSTRAINT CK_AutomationOccurrence_Outcome CHECK((State='Queued' AND RunId IS NOT NULL) OR(State='Skipped' AND RunId IS NULL)));
 CREATE INDEX IX_AutomationOccurrence_Recent ON [automation].[ScheduleOccurrence](OwnerId,DefinitionId,DueAt DESC);
 CREATE UNIQUE INDEX UX_AutomationOccurrence_Run ON [automation].[ScheduleOccurrence](OwnerId,RunId) WHERE RunId IS NOT NULL;
END;
COMMIT;
