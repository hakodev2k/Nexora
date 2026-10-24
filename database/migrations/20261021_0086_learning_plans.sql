-- D06: install only inert local schema. Permissions remain gated until review.
-- No backfill, source mutation, grants, module enablement or destructive rollback.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF (OBJECT_ID(N'[learning].[Plan]',N'U') IS NOT NULL AND NOT EXISTS(
        SELECT 1 FROM sys.extended_properties WHERE major_id=OBJECT_ID(N'[learning].[Plan]') AND minor_id=0
          AND name=N'Nexora.LearningPlan.Contract' AND CONVERT(nvarchar(100),value)=N'learning-plans-v1'))
 OR (OBJECT_ID(N'[learning].[PlanItem]',N'U') IS NOT NULL AND NOT EXISTS(
        SELECT 1 FROM sys.extended_properties WHERE major_id=OBJECT_ID(N'[learning].[PlanItem]') AND minor_id=0
          AND name=N'Nexora.LearningPlan.Contract' AND CONVERT(nvarchar(100),value)=N'learning-plans-v1'))
    THROW 51860, 'Unexpected existing Plan schema: explicit reconciliation is required.', 1;
IF OBJECT_ID(N'[learning].[Plan]',N'U') IS NULL
BEGIN
CREATE TABLE [learning].[Plan](
    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_LearningPlan] PRIMARY KEY NONCLUSTERED,
    [OwnerId] uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace]([Id]),
    [Title] nvarchar(200) NOT NULL CHECK (LEN(LTRIM(RTRIM([Title]))) BETWEEN 1 AND 200),
    [StartsOn] date NULL, [EndsOn] date NULL,
    [Notes] nvarchar(max) NULL CHECK ([Notes] IS NULL OR DATALENGTH([Notes])<=40000),
    [Status] varchar(64) NOT NULL CHECK ([Status] IN('Planned','Active','Completed','Archived')),
    [PreArchiveState] varchar(64) NULL,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
    [UpdatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [UQ_LearningPlan_Owner_Id] UNIQUE ([OwnerId],[Id]),
    CONSTRAINT [FK_LearningPlan_Resource] FOREIGN KEY ([OwnerId],[Id]) REFERENCES [platform].[Resource]([OwnerId],[Id]),
    CONSTRAINT [CK_LearningPlan_Period] CHECK ([StartsOn] IS NULL OR [EndsOn] IS NULL OR [StartsOn]<=[EndsOn]),
    CONSTRAINT [CK_LearningPlan_Archive] CHECK (([Status]<>'Archived' AND [PreArchiveState] IS NULL)
      OR ([Status]='Archived' AND [PreArchiveState] IS NOT NULL AND [PreArchiveState] IN('Planned','Active','Completed')))
);
CREATE UNIQUE CLUSTERED INDEX [CX_LearningPlan_Owner_Created_Id] ON [learning].[Plan]([OwnerId],[CreatedAt],[Id]);
CREATE INDEX [IX_LearningPlan_Seek] ON [learning].[Plan]([OwnerId],[Status],[UpdatedAt] DESC,[Id] DESC);
EXEC sys.sp_addextendedproperty @name=N'Nexora.LearningPlan.Contract',@value=N'learning-plans-v1',
    @level0type=N'SCHEMA',@level0name=N'learning',@level1type=N'TABLE',@level1name=N'Plan';
END;
IF OBJECT_ID(N'[learning].[PlanItem]',N'U') IS NULL
BEGIN
CREATE TABLE [learning].[PlanItem](
    [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_LearningPlanItem] PRIMARY KEY NONCLUSTERED,
    [OwnerId] uniqueidentifier NOT NULL REFERENCES [platform].[PersonalSpace]([Id]),
    [PlanId] uniqueidentifier NOT NULL,
    [ResourceKind] varchar(16) COLLATE Latin1_General_100_BIN2 NOT NULL CHECK ([ResourceKind] IN('Goal','Task','Course')),
    [ResourceId] uniqueidentifier NOT NULL,
    [Position] int NOT NULL CHECK ([Position]>=0), [TargetOn] date NULL,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
    [UpdatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [UQ_LearningPlanItem_Owner_Id] UNIQUE ([OwnerId],[Id]),
    CONSTRAINT [FK_LearningPlanItem_Plan] FOREIGN KEY ([OwnerId],[PlanId]) REFERENCES [learning].[Plan]([OwnerId],[Id]),
    CONSTRAINT [FK_LearningPlanItem_Source] FOREIGN KEY ([OwnerId],[ResourceId]) REFERENCES [platform].[Resource]([OwnerId],[Id]),
    CONSTRAINT [UQ_LearningPlanItem_Position] UNIQUE ([OwnerId],[PlanId],[Position]),
    CONSTRAINT [UQ_LearningPlanItem_Source] UNIQUE ([OwnerId],[PlanId],[ResourceKind],[ResourceId])
);
CREATE UNIQUE CLUSTERED INDEX [CX_LearningPlanItem_Owner_Created_Id] ON [learning].[PlanItem]([OwnerId],[CreatedAt],[Id]);
EXEC sys.sp_addextendedproperty @name=N'Nexora.LearningPlan.Contract',@value=N'learning-plans-v1',
    @level0type=N'SCHEMA',@level0name=N'learning',@level1type=N'TABLE',@level1name=N'PlanItem';
END;
-- Contract markers are committed with the schema so a crash after COMMIT but
-- before the runner's journal insert can safely replay without losing data.
-- Module-owned reference registry types; payload modules remain authoritative for lifecycle.
IF EXISTS(SELECT 1 FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId
    WHERE (m.Code='FX40' AND rt.Code='Plan' AND rt.ContractVersion<>'learning-plans-v1')
       OR (m.Code='FX16' AND rt.Code='Goal' AND rt.ContractVersion<>'goals-plan-source-v1')
       OR (m.Code='FX12' AND rt.Code='Task' AND rt.ContractVersion<>'tasks-plan-source-v1'))
    THROW 51861, 'Unexpected source registry contract: do not relabel existing identities.', 1;
INSERT [platform].[ResourceType]([Id],[ModuleId],[Code],[ContractVersion],[CapabilitiesJson])
SELECT NEWID(),m.Id,v.Kind,v.Version,N'{"schemaVersion":1,"trash":false,"share":false,"support":false,"search":false}'
FROM (VALUES('FX40','Plan','learning-plans-v1'),('FX16','Goal','goals-plan-source-v1'),('FX12','Task','tasks-plan-source-v1')) v(Module,Kind,Version)
JOIN [platform].[Module] m ON m.Code=v.Module
WHERE NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] rt WITH(UPDLOCK,HOLDLOCK) WHERE rt.ModuleId=m.Id AND rt.Code=v.Kind);
INSERT [platform].[Permission]([ActionKey],[EffectiveStatus])
SELECT v.ActionKey,'Gated' FROM (VALUES(N'learning.plan.read'),(N'learning.plan.create'),(N'learning.plan.update'),
    (N'learning.plan.link'),(N'learning.plan.unlink'),(N'learning.plan.reorder'),(N'learning.plan.complete')) v(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] p WITH(UPDLOCK,HOLDLOCK) WHERE p.ActionKey=v.ActionKey);
COMMIT TRANSACTION;
GO
