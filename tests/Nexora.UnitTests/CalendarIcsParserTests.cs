using System.Text;
using Nexora.Infrastructure.Transfer;
using Xunit;

namespace Nexora.UnitTests;

public sealed class CalendarIcsParserTests
{
    private static string Event(string fields="",string time="DTSTART:20261003T100000Z\r\nDTEND:20261003T110000Z")=>"BEGIN:VEVENT\r\nUID:Case-Sensitive\r\nSUMMARY:Literal\\, title\r\nDESCRIPTION:Line one\\nLine two\r\n"+time+"\r\n"+fields+"END:VEVENT\r\n";
    private static byte[] Calendar(string entries)=>Encoding.UTF8.GetBytes("BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Synthetic parser tests//EN\r\n"+entries+"END:VCALENDAR\r\n");
    [Fact]public void Mixed_rows_keep_stable_order_and_report_recurrence_duplicate_alarms_and_status()
    {
        var parsed=CalendarIcsParser.Parse(Calendar(Event("STATUS:CANCELLED\r\nBEGIN:VALARM\r\nTRIGGER:-PT15M\r\nEND:VALARM\r\n")+Event()+Event("RRULE:FREQ=DAILY\r\n")+Event().Replace("SUMMARY:Literal\\, title\r\n","")),"UTC");
        Assert.True(parsed.Succeeded);Assert.Equal(new[]{"Valid","Duplicate","Invalid","Invalid"},parsed.Rows.Select(r=>r.Outcome));
        Assert.Equal(new[]{1,2,3,4},parsed.Rows.Select(r=>r.RowNumber));Assert.Equal("Literal, title",parsed.Rows[0].Candidate!.Title);Assert.Equal("Line one\nLine two",parsed.Rows[0].Candidate!.Description);
        Assert.Contains("ReminderIgnored",parsed.Rows[0].Warnings);Assert.Contains("SourceStatusIgnored",parsed.Rows[0].Warnings);Assert.Equal("RecurringUnsupported",parsed.Rows[2].ReasonCode);
    }
    [Theory][InlineData("DTSTART;TZID=America/New_York:20260308T023000\r\nDTEND;TZID=America/New_York:20260308T040000","UnsafeLocalTime")]
    [InlineData("DTSTART;TZID=America/New_York:20261101T013000\r\nDTEND;TZID=America/New_York:20261101T030000","UnsafeLocalTime")]
    [InlineData("DTSTART;TZID=Unknown/Zone:20261003T100000\r\nDTEND:20261003T110000Z","UnknownTimeZone")]
    [InlineData("DTSTART:20261003T100000+0700\r\nDTEND:20261003T110000Z","InvalidTime")]
    [InlineData("DTSTART;VALUE=DATE:20261003\r\nDTEND:20261004T000000Z","MixedTimeRepresentation")]
    public void Unsafe_or_malformed_times_are_row_errors(string time,string reason)
    {var parsed=CalendarIcsParser.Parse(Calendar(Event(time:time)),"UTC");Assert.True(parsed.Succeeded);Assert.Equal(reason,Assert.Single(parsed.Rows).ReasonCode);}
    [Fact]public void Cross_zone_instants_and_floating_account_interpretation_are_exact()
    {
        var timed=Assert.Single(CalendarIcsParser.Parse(Calendar(Event(time:"DTSTART;TZID=Asia/Ho_Chi_Minh:20261003T170000\r\nDTEND:20261003T110000Z")),"America/New_York").Rows);
        Assert.Equal(DateTimeOffset.Parse("2026-10-03T10:00:00Z"),timed.Candidate!.StartAt);Assert.Equal("America/New_York",timed.Candidate.TimeZoneId);
        var floating=Assert.Single(CalendarIcsParser.Parse(Calendar(Event(time:"DTSTART:20261003T170000\r\nDTEND:20261003T180000")),"Asia/Ho_Chi_Minh").Rows);Assert.Contains("FloatingAccountTimeZone",floating.Warnings);Assert.Equal(timed.Candidate.StartAt,floating.Candidate!.StartAt);
    }
    [Theory][InlineData("20111230","20111231","Pacific/Apia")][InlineData("00010101","00010102","UTC")][InlineData("99991230","99991231","UTC")]
    public void Date_only_ranges_never_resolve_midnight_or_shift_days(string first,string last,string zone)
    {var row=Assert.Single(CalendarIcsParser.Parse(Calendar(Event(time:$"DTSTART;VALUE=DATE:{first}\r\nDTEND;VALUE=DATE:{last}")),zone).Rows);Assert.Equal("Valid",row.Outcome);Assert.Equal(first,row.Candidate!.StartDate!.Value.ToString("yyyyMMdd"));Assert.Equal(last,row.Candidate.EndDateExclusive!.Value.ToString("yyyyMMdd"));}
    [Fact]public void Unfolding_handles_utf8_sequence_split_between_physical_lines()
    {
        var bytes=Calendar(Event().Replace("Literal\\, title","AéB"));var marker=Encoding.UTF8.GetBytes("é");var index=bytes.AsSpan().IndexOf(marker);
        var folded=bytes.Take(index+1).Concat(new byte[]{13,10,32}).Concat(bytes.Skip(index+1)).ToArray();var parsed=CalendarIcsParser.Parse(folded,"UTC");Assert.True(parsed.Succeeded);Assert.Equal("AéB",Assert.Single(parsed.Rows).Candidate!.Title);
    }
    [Fact]public void Envelope_encoding_depth_and_bounds_fail_closed_without_partial_rows()
    {
        foreach(var bytes in new[]{Calendar(Event()).Concat(new byte[]{0}).ToArray(),new byte[]{0xff},Calendar(Event()).Skip(1).ToArray(),new byte[CalendarIcsParser.MaxBytes+1],Calendar(Event("BEGIN:X\r\nEND:Y\r\n")),Calendar(string.Concat(Enumerable.Repeat(Event(),CalendarIcsParser.MaxEvents+1)))})
        {var parsed=CalendarIcsParser.Parse(bytes,"UTC");Assert.False(parsed.Succeeded);Assert.Empty(parsed.Rows);}
    }
    [Fact]public void Duplicate_singletons_bad_escapes_and_oversized_uid_are_not_silent_loss()
    {
        foreach(var body in new[]{Event("SUMMARY:Other\r\n"),Event().Replace("Line one\\nLine two","Bad\\x"),Event().Replace("UID:Case-Sensitive","UID:"+new string('u',1025))})
        {var row=Assert.Single(CalendarIcsParser.Parse(Calendar(body),"UTC").Rows);Assert.Equal("Invalid",row.Outcome);Assert.Null(row.Candidate);}
    }
}
