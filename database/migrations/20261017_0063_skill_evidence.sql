SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'learning.SkillEvidence',N'U') IS NULL
BEGIN
    CREATE TABLE [learning].[SkillEvidence](
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_SkillEvidence] PRIMARY KEY,
        [OwnerId] uniqueidentifier NOT NULL,
        [SkillId] uniqueidentifier NOT NULL,
        [ResourceType] varchar(64) NOT NULL,
        [ResourceId] uniqueidentifier NOT NULL,
        [Note] nvarchar(1000) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [UpdatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [FK_SkillEvidence_Skill] FOREIGN KEY([OwnerId],[SkillId]) REFERENCES [learning].[Skill]([OwnerId],[Id]),
        CONSTRAINT [FK_SkillEvidence_Resource] FOREIGN KEY([OwnerId],[ResourceId]) REFERENCES [platform].[Resource]([OwnerId],[Id]),
        CONSTRAINT [UQ_SkillEvidence_Owner_Id] UNIQUE([OwnerId],[Id]),
        CONSTRAINT [UQ_SkillEvidence_Source] UNIQUE([OwnerId],[SkillId],[ResourceId]),
        CONSTRAINT [CK_SkillEvidence_Type] CHECK([ResourceType]='learning.Course')
    );
    CREATE INDEX [IX_SkillEvidence_Skill] ON [learning].[SkillEvidence]([OwnerId],[SkillId],[Id]);
END;
INSERT [platform].[Permission]([ActionKey],[EffectiveStatus])
SELECT N'learning.skill.evidence','Resolved'
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE [ActionKey]=N'learning.skill.evidence');
COMMIT TRANSACTION;
GO
