SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'learning.Skill') AND name=N'CK_Skill_MergeUnavailable')
    ALTER TABLE [learning].[Skill] DROP CONSTRAINT [CK_Skill_MergeUnavailable];
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'learning.Skill') AND name=N'CK_Skill_MergeTombstone')
    ALTER TABLE [learning].[Skill] WITH CHECK ADD CONSTRAINT [CK_Skill_MergeTombstone]
        CHECK(MergedIntoId IS NULL OR (MergedIntoId<>Id AND Status='Archived'));
IF OBJECT_ID(N'learning.SkillMergeHistory',N'U') IS NULL
BEGIN
    CREATE TABLE [learning].[SkillMergeHistory](
        Id uniqueidentifier NOT NULL CONSTRAINT PK_SkillMergeHistory PRIMARY KEY,
        OwnerId uniqueidentifier NOT NULL,
        SourceId uniqueidentifier NOT NULL,
        TargetId uniqueidentifier NOT NULL,
        SourceJson nvarchar(max) NOT NULL CHECK(ISJSON(SourceJson)=1),
        TargetJson nvarchar(max) NOT NULL CHECK(ISJSON(TargetJson)=1),
        SourceEvidenceCount int NOT NULL CHECK(SourceEvidenceCount>=0),
        TargetEvidenceCount int NOT NULL CHECK(TargetEvidenceCount>=0),
        OverlapCount int NOT NULL,
        MergedAt datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedByUserId uniqueidentifier NOT NULL REFERENCES [identity].[User](Id),
        CONSTRAINT UQ_SkillMergeHistory_Source UNIQUE(OwnerId,SourceId),
        CONSTRAINT FK_SkillMergeHistory_Source FOREIGN KEY(OwnerId,SourceId) REFERENCES [learning].[Skill](OwnerId,Id),
        CONSTRAINT FK_SkillMergeHistory_Target FOREIGN KEY(OwnerId,TargetId) REFERENCES [learning].[Skill](OwnerId,Id),
        CONSTRAINT CK_SkillMergeHistory_Distinct CHECK(SourceId<>TargetId),
        CONSTRAINT CK_SkillMergeHistory_Overlap CHECK(OverlapCount>=0 AND OverlapCount<=SourceEvidenceCount AND OverlapCount<=TargetEvidenceCount)
    );
    CREATE INDEX IX_SkillMergeHistory_Source ON [learning].[SkillMergeHistory](OwnerId,SourceId,Id);
    CREATE INDEX IX_SkillMergeHistory_Target ON [learning].[SkillMergeHistory](OwnerId,TargetId,Id);
END;
INSERT [platform].[Permission](ActionKey,EffectiveStatus)
SELECT N'learning.skill.merge','Resolved'
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] WITH(UPDLOCK,HOLDLOCK) WHERE ActionKey=N'learning.skill.merge');
COMMIT TRANSACTION;
GO
