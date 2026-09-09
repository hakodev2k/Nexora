# Field classification — productivity / calendar

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

[Classification policy and index](../15-field-classification.md). SQL types, nullable CLR mappings and default sensitivity are design specifications, not DTO exposure permissions.

<a id="productivity-project"></a>
## productivity.Project

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Title | nvarchar(200) | string | Private owner |
| Description | nvarchar(max) | string | Private owner |
| StartAt | datetime2(7) | DateTime (UTC only) | Private owner |
| EndAt | datetime2(7) | DateTime (UTC only) | Private owner |
| Status | varchar(64) | string | Private owner |
| Priority | tinyint | byte? | Private owner |
| Color | nvarchar(30) | string? | Private owner |
| Icon | nvarchar(100) | string? | Private owner |
| Notes | nvarchar(max) | string? | Sensitive personal |
| Revision | bigint | long | Private owner |
| TerminalAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |

<a id="productivity-task"></a>
## productivity.Task

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ProjectId | uniqueidentifier | Guid | Private owner |
| Title | nvarchar(200) | string | Private owner |
| Description | nvarchar(max) | string? | Private owner |
| AcceptanceText | nvarchar(max) | string? | Private owner |
| StartAt | datetime2(7) | DateTime (UTC only) | Private owner |
| EndAt | datetime2(7) | DateTime (UTC only) | Private owner |
| Status | varchar(64) | string | Private owner |
| Priority | tinyint | byte? | Private owner |
| Rank | decimal(28,8) | decimal | Private owner |
| Revision | bigint | long | Private owner |
| CalendarUid | nvarchar(255) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |

<a id="productivity-taskchecklistitem"></a>
## productivity.TaskChecklistItem

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| TaskId | uniqueidentifier | Guid | Private owner |
| Text | nvarchar(2000) | string | Private owner |
| Checked | bit | bool | Private owner |
| Position | int | int | Private owner |

<a id="productivity-tag"></a>
## productivity.Tag

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Name | nvarchar(80) | string | Private owner |
| NormalizedName | nvarchar(80) | string | Private owner |

<a id="productivity-projecttag"></a>
## productivity.ProjectTag

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| ProjectId | uniqueidentifier | Guid | Private owner |
| TagId | uniqueidentifier | Guid | Private owner |

<a id="productivity-tasktag"></a>
## productivity.TaskTag

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| TaskId | uniqueidentifier | Guid | Private owner |
| TagId | uniqueidentifier | Guid | Private owner |

<a id="productivity-projectversion"></a>
## productivity.ProjectVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| ProjectId | uniqueidentifier | Guid | Private owner |
| VersionNumber | bigint | long | Private owner |
| SchemaVersion | int | int | Private owner |
| SnapshotJson | nvarchar(max) | string | Private owner |
| Reason | nvarchar(2000) | string? | Sensitive personal |
| SourceVersion | bigint | long? | Private owner |
| CommandKeyHash | binary(32) | byte[] | Secret/credential envelope |

<a id="productivity-taskversion"></a>
## productivity.TaskVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| TaskId | uniqueidentifier | Guid | Private owner |
| VersionNumber | bigint | long | Private owner |
| SchemaVersion | int | int | Private owner |
| SnapshotJson | nvarchar(max) | string | Private owner |
| Reason | nvarchar(2000) | string? | Sensitive personal |
| SourceVersion | bigint | long? | Private owner |
| CommandKeyHash | binary(32) | byte[] | Secret/credential envelope |

<a id="calendar-manualevent"></a>
## calendar.ManualEvent

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Title | nvarchar(200) | string | Private owner |
| Description | nvarchar(max) | string | Private owner |
| IsAllDay | bit | bool | Private owner |
| StartAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| EndAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| StartDate | date | DateOnly? | Private owner |
| EndDateExclusive | date | DateOnly? | Private owner |
| TimeZoneId | nvarchar(100) | string | Private owner |
| Status | varchar(64) | string | Private owner |
| CalendarUid | nvarchar(255) | string | Private owner |
| Revision | bigint | long | Private owner |

<a id="calendar-importeduid"></a>
## calendar.ImportedUid

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Uid | nvarchar(1024) | string | Private owner |
| UidDigest | binary(32) | byte[] | Private owner |
| ManualEventId | uniqueidentifier | Guid | Private owner |
| ImportBatchId | uniqueidentifier | Guid | Private owner |

<a id="calendar-reminder"></a>
## calendar.Reminder

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| SourceResourceId | uniqueidentifier | Guid | Private owner |
| ConfigType | varchar(64) | string | Private owner |
| ExactAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| TimeZoneId | nvarchar(100) | string | Private owner |
| DueAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| SourceRevision | bigint | long | Private owner |
| State | varchar(64) | string | Private owner |

<a id="productivity-plannerpin"></a>
## productivity.PlannerPin

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| TaskResourceId | uniqueidentifier | Guid | Private owner |
| PlanDate | date | DateOnly | Private owner |
| Rank | decimal(28,8) | decimal | Private owner |
| Notes | nvarchar(2000) | string? | Sensitive personal |

<a id="productivity-goal"></a>
## productivity.Goal

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Title | nvarchar(200) | string | Private owner |
| Description | nvarchar(max) | string? | Private owner |
| StartDate | date | DateOnly? | Private owner |
| EndDate | date | DateOnly? | Private owner |
| Status | varchar(64) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| PreArchiveStatus | varchar(64) | string? | Private owner |

<a id="productivity-goaltarget"></a>
## productivity.GoalTarget

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| GoalId | uniqueidentifier | Guid | Private owner |
| Kind | varchar(64) | string | Private owner |
| Title | nvarchar(200) | string | Private owner |
| TargetValue | decimal(28,8) | decimal? | Private owner |
| CurrentValue | decimal(28,8) | decimal? | Private owner |
| BooleanValue | bit | bool? | Private owner |
| Position | int | int | Private owner |
| InitialValue | decimal(28,8) | decimal? | Private owner |

<a id="productivity-goalprogress"></a>
## productivity.GoalProgress

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| TargetId | uniqueidentifier | Guid | Private owner |
| Value | decimal(28,8) | decimal? | Private owner |
| Checked | bit | bool? | Private owner |
| RecordedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| Note | nvarchar(2000) | string? | Private owner |

<a id="productivity-habit"></a>
## productivity.Habit

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| Title | nvarchar(100) | string | Private owner |
| Kind | varchar(64) | string | Private owner |
| TargetCount | int | int? | Private owner |
| State | varchar(64) | string | Private owner |
| TimeZoneId | nvarchar(100) | string | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| Unit | nvarchar(50) | string? | Private owner |
| ReminderLocalTime | time(0) | TimeOnly? | Private owner |
| PreArchiveState | varchar(64) | string? | Private owner |

<a id="productivity-habitschedule"></a>
## productivity.HabitSchedule

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| HabitId | uniqueidentifier | Guid | Private owner |
| EffectiveFrom | date | DateOnly | Private owner |
| EffectiveUntil | date | DateOnly? | Private owner |
| WeekdayMask | tinyint | byte | Private owner |
| TargetCount | int | int? | Private owner |
| Paused | bit | bool | Private owner |

<a id="productivity-habitcheckin"></a>
## productivity.HabitCheckIn

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| HabitId | uniqueidentifier | Guid | Private owner |
| LocalDate | date | DateOnly | Private owner |
| Count | int | int | Private owner |
| Note | nvarchar(1000) | string? | Private owner |
| ScheduleId | uniqueidentifier | Guid | Private owner |

<a id="productivity-timeentry"></a>
## productivity.TimeEntry

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| TaskResourceId | uniqueidentifier | Guid? | Private owner |
| Description | nvarchar(2000) | string? | Private owner |
| StartAt | datetime2(7) | DateTime (UTC only) | Private owner |
| EndAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| State | varchar(64) | string | Private owner |
| Source | varchar(64) | string | Private owner |
| FocusSessionReference | uniqueidentifier | Guid? | Private owner |
| TrashBatchId | uniqueidentifier | Guid? | Private owner |
| ProjectResourceId | uniqueidentifier | Guid? | Private owner |
| Category | nvarchar(100) | string? | Private owner |

<a id="productivity-focussession"></a>
## productivity.FocusSession

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| TaskResourceId | uniqueidentifier | Guid? | Private owner |
| Kind | varchar(64) | string | Private owner |
| State | varchar(64) | string | Private owner |
| PlannedSeconds | int | int | Private owner |
| AccumulatedSeconds | int | int | Private owner |
| RunningSince | datetime2(7) | DateTime (UTC only)? | Private owner |
| CompletedAt | datetime2(7) | DateTime (UTC only)? | Private owner |
| CycleNumber | int | int | Private owner |
| ConvertedTimeEntryResourceId | uniqueidentifier | Guid? | Private owner |
| StopReason | varchar(64) | string? | Sensitive personal |
| ActiveSlot | tinyint | byte? | Private owner |

<a id="productivity-focussegment"></a>
## productivity.FocusSegment

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| SessionId | uniqueidentifier | Guid | Private owner |
| StartAt | datetime2(7) | DateTime (UTC only) | Private owner |
| EndAt | datetime2(7) | DateTime (UTC only) | Private owner |
| Outcome | varchar(64) | string | Private owner |

<a id="productivity-goaltargettask"></a>
## productivity.GoalTargetTask

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| UpdatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| UpdatedByUserId | uniqueidentifier | Guid? | Private owner |
| RowVersion | rowversion | byte[] | Private owner |
| TargetId | uniqueidentifier | Guid | Private owner |
| TaskResourceId | uniqueidentifier | Guid | Private owner |

<a id="productivity-timeentryversion"></a>
## productivity.TimeEntryVersion

| Field | SQL | C# representation | Classification |
| --- | --- | --- | --- |
| Id | uniqueidentifier | Guid | Private owner |
| OwnerId | uniqueidentifier | Guid | Private owner |
| CreatedAt | datetime2(7) | DateTime (UTC only) | Private owner |
| CreatedByUserId | uniqueidentifier | Guid? | Private owner |
| TimeEntryId | uniqueidentifier | Guid | Private owner |
| VersionNumber | bigint | long | Private owner |
| SnapshotJson | nvarchar(max) | string | Private owner |
| Reason | nvarchar(2000) | string? | Sensitive personal |
