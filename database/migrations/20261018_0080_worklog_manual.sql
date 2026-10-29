SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'[learning].[WorkLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [learning].[WorkLog] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_WorkLog] PRIMARY KEY NONCLUSTERED,
        [OwnerId] uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace]([Id]),
        [Title] nvarchar(200) NOT NULL CHECK (LEN(LTRIM(RTRIM([Title]))) BETWEEN 1 AND 200),
        [RecordedOn] date NOT NULL,
        [Minutes] int NOT NULL CHECK ([Minutes] BETWEEN 1 AND 1440),
        [Notes] nvarchar(max) NULL CHECK ([Notes] IS NULL OR DATALENGTH([Notes]) <= 40000),
        [Category] nvarchar(100) NULL,
        [Employer] nvarchar(200) NULL,
        [Status] varchar(64) NOT NULL CHECK ([Status] IN ('Active','Archived')),
        [PreArchiveState] varchar(64) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [UpdatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [UQ_WorkLog_Owner_Id] UNIQUE ([OwnerId],[Id]),
        CONSTRAINT [FK_WorkLog_Resource] FOREIGN KEY ([OwnerId],[Id]) REFERENCES [platform].[Resource]([OwnerId],[Id]),
        CONSTRAINT [CK_WorkLog_Archive] CHECK (([Status]='Active' AND [PreArchiveState] IS NULL)
            OR ([Status]='Archived' AND [PreArchiveState]='Active' AND [PreArchiveState] IS NOT NULL))
    );
    CREATE UNIQUE CLUSTERED INDEX [CX_WorkLog_Owner_Created_Id] ON [learning].[WorkLog]([OwnerId],[CreatedAt],[Id]);
    CREATE INDEX [IX_WorkLog_Owner_Status_Date_Id] ON [learning].[WorkLog]([OwnerId],[Status],[RecordedOn] DESC,[Id] DESC);
END;
INSERT [platform].[ResourceType] ([Id],[ModuleId],[Code],[ContractVersion],[CapabilitiesJson])
SELECT NEWID(),m.[Id],'WorkLog','worklog-manual-v1',N'{"schemaVersion":1,"trash":false,"share":false,"support":false,"search":false}'
FROM [platform].[Module] m WHERE m.[Code]='FX40'
  AND NOT EXISTS (SELECT 1 FROM [platform].[ResourceType] rt WITH (UPDLOCK,HOLDLOCK) WHERE rt.[ModuleId]=m.[Id] AND rt.[Code]='WorkLog');
INSERT [platform].[Permission] ([ActionKey],[EffectiveStatus])
SELECT v.[ActionKey],'Resolved' FROM (VALUES
    (N'learning.worklog.read'),(N'learning.worklog.create'),(N'learning.worklog.update'),
    (N'learning.worklog.archive'),(N'learning.worklog.unarchive'),(N'learning.report.read')) v([ActionKey])
WHERE NOT EXISTS (SELECT 1 FROM [platform].[Permission] p WITH (UPDLOCK,HOLDLOCK) WHERE p.[ActionKey]=v.[ActionKey]);
COMMIT TRANSACTION;
GO
