namespace Nexora.Application.Planner;

public static class PlannerPolicy
{
    public const int MaximumNotesLength = 2000;
    public const int MaximumPlanningRangeDays = 31;

    public static bool IsActiveTask(string taskStatus, string projectStatus) =>
        taskStatus is "NotStarted" or "InProgress" && projectStatus is "NotStarted" or "InProgress";

    public static bool IsValidRange(DateOnly from, DateOnly to) =>
        to >= from && to.DayNumber - from.DayNumber <= MaximumPlanningRangeDays;

    public static string? NormalizeNotes(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
