-- Plan archive/unarchive expansion only. No grants, activation, backfill or source writes.
-- Requires installed0086; rollback disables code/readiness and preserves all data/receipts/audit.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'[learning].[Plan]',N'U') IS NULL
 OR COL_LENGTH(N'learning.Plan',N'PreArchiveState') IS NULL
 OR NOT EXISTS(SELECT 1 FROM sys.extended_properties
     WHERE major_id=OBJECT_ID(N'[learning].[Plan]') AND minor_id=0
       AND name=N'Nexora.LearningPlan.Contract' AND CONVERT(nvarchar(100),value)=N'learning-plans-v1')
 OR NOT EXISTS(SELECT 1 FROM sys.check_constraints
     WHERE parent_object_id=OBJECT_ID(N'[learning].[Plan]') AND name=N'CK_LearningPlan_Archive'
       AND is_disabled=0 AND is_not_trusted=0)
    THROW 51890, 'Plan lifecycle requires the reviewed0086 schema and trusted prior-state constraint.', 1;

IF EXISTS(SELECT 1 FROM [learning].[Plan] WHERE
    Status NOT IN('Planned','Active','Completed','Archived')
    OR (Status='Archived' AND (PreArchiveState IS NULL OR PreArchiveState NOT IN('Planned','Active','Completed')))
    OR (Status<>'Archived' AND PreArchiveState IS NOT NULL))
    THROW 51891, 'Invalid Plan prior state: explicit reconciliation is required; no state will be guessed.', 1;

IF NOT EXISTS(SELECT 1 FROM [platform].[ResourceType] rt JOIN [platform].[Module] m ON m.Id=rt.ModuleId
    WHERE m.Code='FX40' AND rt.Code='Plan' AND rt.ContractVersion='learning-plans-v1')
    THROW 51892, 'Plan lifecycle registry contract is unavailable.', 1;

INSERT [platform].[Permission]([ActionKey],[EffectiveStatus])
SELECT v.ActionKey,'Gated' FROM (VALUES(N'learning.plan.archive'),(N'learning.plan.unarchive')) v(ActionKey)
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] p WITH(UPDLOCK,HOLDLOCK) WHERE p.ActionKey=v.ActionKey);

-- Transactional marker permits crash-gap replay before migration-journal insertion.
IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE major_id=OBJECT_ID(N'[learning].[Plan]') AND minor_id=0
    AND name=N'Nexora.LearningPlan.Lifecycle' AND CONVERT(nvarchar(100),value)<>N'learning-plan-archive-v1')
    THROW 51893, 'Unexpected Plan lifecycle marker: explicit reconciliation is required.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE major_id=OBJECT_ID(N'[learning].[Plan]') AND minor_id=0
    AND name=N'Nexora.LearningPlan.Lifecycle')
    EXEC sys.sp_addextendedproperty @name=N'Nexora.LearningPlan.Lifecycle',@value=N'learning-plan-archive-v1',
        @level0type=N'SCHEMA',@level0name=N'learning',@level1type=N'TABLE',@level1name=N'Plan';
COMMIT TRANSACTION;
GO
