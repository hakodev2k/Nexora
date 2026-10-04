using System.Text;
using Nexora.Application.Transfer;
using Nexora.Infrastructure.Transfer;
using Xunit;

namespace Nexora.UnitTests;

public sealed class CalendarIcsSerializerTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-10-04T10:00:00Z");
    private static CalendarExportEvent Event() => new("stable-private-opaque@nexora.local", "Literal, Unicode 日本語 😀", "First; line\\\nSecond line",
        "Manual", "Scheduled", Start, Start.AddHours(1), null, null, null, null);
    [Fact] public void Utf8_folding_text_and_utc_dates_round_trip_without_sensitive_metadata()
    {
        var source = Event() with { Description = string.Concat(Enumerable.Repeat("日本語😀,;\\\n", 40)) + "Literal end" };
        var result = CalendarIcsSerializer.Serialize([source], "America/New_York", Start);
        Assert.Null(result.Reason); var text = new UTF8Encoding(false, true).GetString(result.Content!);
        foreach (var line in text.Split("\r\n")) Assert.InRange(Encoding.UTF8.GetByteCount(line), 0, 75);
        Assert.EndsWith("END:VCALENDAR\r\n", text); Assert.DoesNotContain("VALARM", text); Assert.DoesNotContain("OWNER", text);
        var parsed = CalendarIcsParser.Parse(result.Content!, "Asia/Ho_Chi_Minh"); Assert.True(parsed.Succeeded);
        var row = Assert.Single(parsed.Rows); Assert.Equal("Valid", row.Outcome); Assert.Equal(source.Title, row.Candidate!.Title);
        Assert.Equal(source.Description, row.Candidate.Description); Assert.Equal(source.StartAt, row.Candidate.StartAt); Assert.Equal(source.EndAt, row.Candidate.EndAt);
    }
    [Fact] public void All_day_skipped_civil_day_keeps_exclusive_dates_and_stable_uid()
    {
        var item = Event() with { StartDate = new(2011,12,30), EndDateExclusive = new(2011,12,31) };
        var first = CalendarIcsSerializer.Serialize([item], "Pacific/Apia", Start); Assert.Null(first.Reason);
        var row = Assert.Single(CalendarIcsParser.Parse(first.Content!, "UTC").Rows); Assert.Equal(item.StartDate, row.Candidate!.StartDate); Assert.Equal(item.EndDateExclusive, row.Candidate.EndDateExclusive);
        var second = Encoding.UTF8.GetString(CalendarIcsSerializer.Serialize([item], "UTC", Start.AddDays(1)).Content!);
        Assert.Contains("UID:" + item.CalendarUid, second); Assert.Contains("DTEND;VALUE=DATE:20111231", second);
    }
    [Theory] [InlineData("NotStarted")] [InlineData("InProgress")] [InlineData("Completed")] [InlineData("Skipped")]
    public void Task_business_status_and_safe_metadata_are_preserved(string status)
    {
        var item = Event() with { SourceKind = "Task", Status = status, TaskPriority = "P1", ProjectTitle = "Owned project, label" };
        var text = Encoding.UTF8.GetString(CalendarIcsSerializer.Serialize([item], "UTC", Start).Content!);
        Assert.Contains("X-NEXORA-STATUS:" + status, text); Assert.Contains("X-NEXORA-PRIORITY:P1", text); Assert.Contains("X-NEXORA-PROJECT:Owned project\\, label", text);
        Assert.DoesNotContain("\r\nSTATUS:", text); Assert.DoesNotContain("TASKID", text);
    }
    [Fact] public void Saved_subsecond_instants_are_rejected_not_rounded()
    { Assert.Equal("SourcePrecisionUnsupported", CalendarIcsSerializer.Serialize([Event() with { StartAt = Start.AddTicks(1) }], "UTC", Start).Reason); }
    [Theory] [InlineData("unsafe\u0000text")] [InlineData("unsafe\u001btext")]
    public void Unsafe_controls_or_unpaired_unicode_never_become_content(string text)
    { Assert.Equal("SourceCompatibilityUnavailable", CalendarIcsSerializer.Serialize([Event() with { Title = text }], "UTC", Start).Reason); }
    [Fact] public void Runtime_unpaired_surrogate_is_rejected_before_unicode_enumeration()
    { Assert.Equal("SourceCompatibilityUnavailable", CalendarIcsSerializer.Serialize([Event() with { Title = "bad" + new string((char)0xd800,1) }], "UTC", Start).Reason); }
    [Fact] public void Optional_task_priority_is_omitted_without_fabricating_a_value()
    {
        var result = CalendarIcsSerializer.Serialize([Event() with { SourceKind="Task", Status="Completed", ProjectTitle="Owned project", TaskPriority=null }],"UTC",Start);
        Assert.Null(result.Reason); Assert.DoesNotContain("X-NEXORA-PRIORITY",Encoding.UTF8.GetString(result.Content!));
    }
    [Fact] public void Duplicate_uid_and_final_byte_bounds_fail_without_partial_calendar()
    {
        Assert.Null(CalendarIcsSerializer.Serialize([Event(), Event()], "UTC", Start).Content);
        var items = Enumerable.Range(0,100).Select(i => Event() with { CalendarUid = "safe-" + i + "@nexora.local", Description = new string('a', 20000) }).ToArray();
        Assert.Equal("ExportLimitExceeded", CalendarIcsSerializer.Serialize(items, "UTC", Start).Reason);
        var empty = CalendarIcsSerializer.Serialize([], "UTC", Start); Assert.Null(empty.Reason); Assert.DoesNotContain("BEGIN:VEVENT", Encoding.UTF8.GetString(empty.Content!));
    }
}
