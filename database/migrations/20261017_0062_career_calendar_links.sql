SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'career.CalendarLink',N'U') IS NULL
BEGIN
    CREATE TABLE [career].[CalendarLink] (
        [Id] uniqueidentifier NOT NULL CONSTRAINT [PK_CareerCalendarLink] PRIMARY KEY,
        [OwnerId] uniqueidentifier NOT NULL, [JobApplicationId] uniqueidentifier NOT NULL, [CalendarEventId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(), [UpdatedAt] datetime2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]),
        [UpdatedByUserId] uniqueidentifier NOT NULL REFERENCES [identity].[User]([Id]), [RowVersion] rowversion NOT NULL,
        CONSTRAINT [FK_CareerCalendarLink_Job] FOREIGN KEY([OwnerId],[JobApplicationId]) REFERENCES [career].[JobApplication]([OwnerId],[Id]),
        CONSTRAINT [FK_CareerCalendarLink_Event] FOREIGN KEY([OwnerId],[CalendarEventId]) REFERENCES [calendar].[Event]([OwnerId],[Id]),
        CONSTRAINT [UQ_CareerCalendarLink_Reference] UNIQUE([OwnerId],[JobApplicationId],[CalendarEventId]),
        CONSTRAINT [UQ_CareerCalendarLink_Owner_Id] UNIQUE([OwnerId],[Id])
    );
END;
INSERT INTO [platform].[Permission]([ActionKey],[EffectiveStatus])
SELECT [ActionKey],'Resolved' FROM (VALUES(N'career.appointment.read'),(N'career.appointment.create_event'),(N'career.appointment.link_event'),(N'career.appointment.unlink_event')) v([ActionKey])
WHERE NOT EXISTS(SELECT 1 FROM [platform].[Permission] p WITH(UPDLOCK,HOLDLOCK) WHERE p.[ActionKey]=v.[ActionKey]);
COMMIT TRANSACTION;
GO
