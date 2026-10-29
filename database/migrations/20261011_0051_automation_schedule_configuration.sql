SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('automation.DefinitionVersion','U') IS NULL THROW 51051,'Automation definitions must be installed first.',1;
IF OBJECT_ID('automation.Schedule','U') IS NULL
 CREATE TABLE [automation].[Schedule](OwnerId uniqueidentifier NOT NULL,DefinitionId uniqueidentifier NOT NULL,
 CurrentVersion bigint NOT NULL CHECK(CurrentVersion>0),NextDueAt datetime2(7) NULL,
 CONSTRAINT PK_AutomationSchedule PRIMARY KEY(OwnerId,DefinitionId),
 CONSTRAINT FK_AutomationSchedule_Definition FOREIGN KEY(OwnerId,DefinitionId) REFERENCES [automation].[Definition](OwnerId,Id));
IF OBJECT_ID('automation.ScheduleVersion','U') IS NULL
 CREATE TABLE [automation].[ScheduleVersion](OwnerId uniqueidentifier NOT NULL,DefinitionId uniqueidentifier NOT NULL,
 VersionNumber bigint NOT NULL CHECK(VersionNumber>0),DefinitionVersion bigint NOT NULL,
 ConfigurationJson nvarchar(2048) NOT NULL CHECK(ISJSON(ConfigurationJson)=1),
 CreatedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
 CONSTRAINT PK_AutomationScheduleVersion PRIMARY KEY(OwnerId,DefinitionId,VersionNumber),
 CONSTRAINT FK_AutomationScheduleVersion_DefinitionVersion FOREIGN KEY(OwnerId,DefinitionId,DefinitionVersion) REFERENCES [automation].[DefinitionVersion](OwnerId,DefinitionId,VersionNumber));
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_AutomationSchedule_CurrentVersion' AND parent_object_id=OBJECT_ID('automation.Schedule'))
 ALTER TABLE [automation].[Schedule] ADD CONSTRAINT FK_AutomationSchedule_CurrentVersion FOREIGN KEY(OwnerId,DefinitionId,CurrentVersion) REFERENCES [automation].[ScheduleVersion](OwnerId,DefinitionId,VersionNumber);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_AutomationSchedule_NextDue' AND object_id=OBJECT_ID('automation.Schedule'))
 CREATE INDEX IX_AutomationSchedule_NextDue ON [automation].[Schedule](NextDueAt) INCLUDE(OwnerId,DefinitionId,CurrentVersion) WHERE NextDueAt IS NOT NULL;
UPDATE [platform].[Permission] SET EffectiveStatus='Resolved' WHERE ActionKey='automation.schedule.update';
IF NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey='automation.schedule.update')
 INSERT [platform].[Permission](ActionKey,EffectiveStatus) VALUES('automation.schedule.update','Resolved');
-- No grants, enabled schedules, runtime execution or provider activation is introduced.
COMMIT;
