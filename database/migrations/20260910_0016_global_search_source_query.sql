SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

/* FX25-S01 local slice: direct, bounded queries over registered source tables.
   No persisted search index, saved query, favorite or recent-item authority is
   enabled by this migration. */
IF OBJECT_ID(N'[platform].[Permission]', N'U') IS NOT NULL
BEGIN
    MERGE [platform].[Permission] AS target
    USING (VALUES (N'discovery.search.query')) AS source ([ActionKey])
    ON target.[ActionKey] = source.[ActionKey]
    WHEN MATCHED AND target.[EffectiveStatus] <> 'Resolved' THEN
        UPDATE SET [EffectiveStatus] = 'Resolved'
    WHEN NOT MATCHED BY TARGET THEN
        INSERT ([ActionKey], [EffectiveStatus]) VALUES (source.[ActionKey], 'Resolved');
END
GO

COMMIT TRANSACTION;
GO
