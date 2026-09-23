/*
   Forward-compatible field-width alignment for the local Productivity slice.
   The existing Project [Name] column is the physical compatibility mapping for
   the product Title field. Widening is safe for existing local rows; the API
   remains authoritative for required/non-blank descriptions and the 1..200
   title bound. Task/Event title columns are intentionally not narrowed here:
   narrowing existing data would be destructive; their 1..200 contract is
   enforced before writes until a reviewed expand/contract cleanup exists.
*/
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.[object_id] = c.[object_id]
    INNER JOIN sys.schemas s ON s.[schema_id] = t.[schema_id]
    WHERE s.[name] = N'productivity'
      AND t.[name] = N'Project'
      AND c.[name] = N'Name'
      AND c.[max_length] > 0
      AND c.[max_length] < 400
)
    ALTER TABLE [productivity].[Project] ALTER COLUMN [Name] nvarchar(200) NOT NULL;

IF EXISTS
(
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.[object_id] = c.[object_id]
    INNER JOIN sys.schemas s ON s.[schema_id] = t.[schema_id]
    WHERE s.[name] = N'productivity'
      AND t.[name] = N'Project'
      AND c.[name] = N'Description'
      AND c.[max_length] > 0
      AND c.[max_length] < 40000
)
    ALTER TABLE [productivity].[Project] ALTER COLUMN [Description] nvarchar(20000) NULL;

IF EXISTS
(
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.[object_id] = c.[object_id]
    INNER JOIN sys.schemas s ON s.[schema_id] = t.[schema_id]
    WHERE s.[name] = N'productivity'
      AND t.[name] = N'ProjectHistory'
      AND c.[name] = N'Name'
      AND c.[max_length] > 0
      AND c.[max_length] < 400
)
    ALTER TABLE [productivity].[ProjectHistory] ALTER COLUMN [Name] nvarchar(200) NOT NULL;

IF EXISTS
(
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.[object_id] = c.[object_id]
    INNER JOIN sys.schemas s ON s.[schema_id] = t.[schema_id]
    WHERE s.[name] = N'productivity'
      AND t.[name] = N'ProjectHistory'
      AND c.[name] = N'Description'
      AND c.[max_length] > 0
      AND c.[max_length] < 40000
)
    ALTER TABLE [productivity].[ProjectHistory] ALTER COLUMN [Description] nvarchar(20000) NULL;

IF EXISTS
(
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.[object_id] = c.[object_id]
    INNER JOIN sys.schemas s ON s.[schema_id] = t.[schema_id]
    WHERE s.[name] = N'calendar'
      AND t.[name] = N'Event'
      AND c.[name] = N'Description'
      AND c.[max_length] > 0
      AND c.[max_length] < 40000
)
    ALTER TABLE [calendar].[Event] ALTER COLUMN [Description] nvarchar(20000) NULL;

COMMIT TRANSACTION;
GO

