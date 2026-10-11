SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('automation.Definition','DeletionBatchId') IS NULL THROW 51049,'Automation lifecycle must be installed first.',1;
-- Replace only prior column checks by their metadata identity (0047 used system names).
DECLARE @drop nvarchar(max)=N'';
SELECT @drop=@drop+N'ALTER TABLE [automation].[Definition] DROP CONSTRAINT '+QUOTENAME(name)+N';'
 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('automation.Definition')
 AND parent_column_id IN(COLUMNPROPERTY(OBJECT_ID('automation.Definition'),'State','ColumnId'),COLUMNPROPERTY(OBJECT_ID('automation.Definition'),'Enabled','ColumnId'));
IF @drop<>N'' EXEC sp_executesql @drop;
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_AutomationDefinition_ExecutionState' AND parent_object_id=OBJECT_ID('automation.Definition'))
 ALTER TABLE [automation].[Definition] ADD CONSTRAINT CK_AutomationDefinition_ExecutionState CHECK((Enabled=1 AND State='Enabled' AND DeletionBatchId IS NULL) OR(Enabled=0 AND State IN('Draft','Disabled')));
IF OBJECT_ID('automation.Run','U') IS NULL
BEGIN
 CREATE TABLE [automation].[Run](
 Id uniqueidentifier NOT NULL PRIMARY KEY NONCLUSTERED, OwnerId uniqueidentifier NOT NULL,
 DefinitionId uniqueidentifier NOT NULL, VersionNumber bigint NOT NULL,
 ActorUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id), SessionId uniqueidentifier NOT NULL REFERENCES [identity].[Session](Id), ActorRole varchar(32) NOT NULL,
 AuthenticatedAt datetime2(7) NOT NULL,
 State varchar(32) NOT NULL DEFAULT 'Queued' CHECK(State IN('Queued','Running','Succeeded','PartiallySucceeded','Failed','Cancelled','TimedOut','Skipped')),
 CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), StartedAt datetime2(7) NULL, FinishedAt datetime2(7) NULL,
 DeadlineAt datetime2(7) NULL, NextAttemptAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
 LeaseToken uniqueidentifier NULL, LeaseUntil datetime2(7) NULL, CancelRequested bit NOT NULL DEFAULT 0,
 Code varchar(64) NULL, EffectUncertain bit NOT NULL DEFAULT 0, RowVersion rowversion NOT NULL,
 CONSTRAINT UQ_AutomationRun_Owner_Id UNIQUE(OwnerId,Id),
 CONSTRAINT FK_AutomationRun_Version FOREIGN KEY(OwnerId,DefinitionId,VersionNumber) REFERENCES [automation].[DefinitionVersion](OwnerId,DefinitionId,VersionNumber));
 CREATE UNIQUE CLUSTERED INDEX CX_AutomationRun_Owner_Created_Id ON [automation].[Run](OwnerId,CreatedAt DESC,Id DESC);
 CREATE INDEX IX_AutomationRun_Claim ON [automation].[Run](State,NextAttemptAt,LeaseUntil,CreatedAt);
 CREATE INDEX IX_AutomationRun_Definition ON [automation].[Run](OwnerId,DefinitionId,CreatedAt DESC,Id DESC);
 CREATE UNIQUE INDEX UX_AutomationRun_OneRunningDefinition ON [automation].[Run](OwnerId,DefinitionId) WHERE State='Running';
END;
IF OBJECT_ID('automation.StepRun','U') IS NULL
BEGIN
 CREATE TABLE [automation].[StepRun](
 OwnerId uniqueidentifier NOT NULL, RunId uniqueidentifier NOT NULL, StepId int NOT NULL CHECK(StepId>0),
 Position int NOT NULL CHECK(Position BETWEEN 1 AND 20), CommandKey uniqueidentifier NOT NULL,
 State varchar(32) NOT NULL DEFAULT 'Queued' CHECK(State IN('Queued','Running','Succeeded','Failed','Cancelled','TimedOut','Skipped')),
 Attempts int NOT NULL DEFAULT 0 CHECK(Attempts BETWEEN 0 AND 4), TaskId uniqueidentifier NULL,
 Code varchar(64) NULL, EffectUncertain bit NOT NULL DEFAULT 0,
 CONSTRAINT PK_AutomationStepRun PRIMARY KEY(OwnerId,RunId,StepId),
 CONSTRAINT UQ_AutomationStepRun_Position UNIQUE(OwnerId,RunId,Position),
 CONSTRAINT UQ_AutomationStepRun_Command UNIQUE(CommandKey),
 CONSTRAINT FK_AutomationStepRun_Run FOREIGN KEY(OwnerId,RunId) REFERENCES [automation].[Run](OwnerId,Id));
END;
UPDATE [platform].[Permission] SET EffectiveStatus='Resolved' WHERE ActionKey IN('automation.definition.enable','automation.definition.disable','automation.run.start','automation.run.read','automation.run.cancel','automation.step.dispatch');
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
 SELECT s.ActionKey,'Resolved' FROM(VALUES('automation.definition.enable'),('automation.definition.disable'),('automation.run.start'),('automation.run.read'),('automation.run.cancel'),('automation.step.dispatch'))s(ActionKey)
 WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=s.ActionKey);
COMMIT;
