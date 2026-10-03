using System.Globalization;
using System.Text.RegularExpressions;
using Nexora.Application.Career;

namespace Nexora.Infrastructure.Career;

internal static class CareerInput
{
    internal static readonly string[] Stages = ["Saved", "Preparing", "Applied", "Screening", "Interviewing", "Offer", "Accepted", "Rejected", "Withdrawn", "Closed"];
    internal static bool ValidStage(string? value) => value is not null && Stages.Contains(value, StringComparer.Ordinal);
    internal static bool ReturnToProgress(string from, string to) =>
        from is "Accepted" or "Rejected" or "Withdrawn" or "Closed" &&
        to is "Saved" or "Preparing" or "Applied" or "Screening" or "Interviewing" or "Offer";
    internal static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? Literal(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private static bool Fits(string? value, int max) => value is null || value.Length <= max;
    private static bool Url(string? value) => value is null ||
        (value.Length <= 2048 && Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
         uri.Scheme is "http" or "https" && uri.UserInfo.Length == 0 && uri.Host.Length > 0);
    internal static bool Company(CompanyMetadata? input, out CompanyMetadata output)
    {
        output = default!;
        if (input is null || string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200 ||
            !Fits(input.Url, 2048) || !Fits(input.Industry, 200) || !Fits(input.Location, 200) || !Fits(input.Notes, 20000)) return false;
        output = input with { Title = input.Title.Trim(), Url = Optional(input.Url), Industry = Optional(input.Industry), Location = Optional(input.Location), Notes = Literal(input.Notes) };
        return Url(output.Url);
    }
    internal static bool Job(JobMetadata? input, out JobMetadata output)
    {
        output = default!;
        if (input is null || string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200 ||
            !Fits(input.Url, 2048) || !Fits(input.Location, 200) || !Fits(input.EmploymentType, 100) || !Fits(input.SalaryText, 1000) ||
            !Fits(input.Description, 20000) || !Fits(input.Notes, 20000) || !Fits(input.Source, 200) || input.CompanyId == Guid.Empty ||
            input.WorkMode is not (null or "Onsite" or "Hybrid" or "Remote") ||
            !Amount(input.SalaryMin, out var min) || !Amount(input.SalaryMax, out var max) || (min is not null && max is not null && min > max)) return false;
        var currency = Optional(input.Currency);
        if ((currency is not null && !Currencies.Contains(currency)) || ((min is not null || max is not null) && currency is null)) return false;
        output = input with { Title = input.Title.Trim(), Url = Optional(input.Url), Location = Optional(input.Location),
            EmploymentType = Optional(input.EmploymentType), SalaryText = Literal(input.SalaryText), SalaryMin = Exact(min), SalaryMax = Exact(max),
            Currency = currency, Description = Literal(input.Description), Notes = Literal(input.Notes), Source = Optional(input.Source) };
        return Url(output.Url);
    }
    internal static string? Exact(decimal? value) => value?.ToString("0.########", CultureInfo.InvariantCulture);
    internal static bool Amount(string? value, out decimal? amount)
    {
        amount = null;
        if (value is null) return true;
        if (value.Length > 30 || !Regex.IsMatch(value, @"^-?(0|[1-9][0-9]{0,19})(\.[0-9]{1,8})?$", RegexOptions.CultureInvariant) ||
            !decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)) return false;
        amount = parsed; return true;
    }
    internal static bool Instant(string? value, out DateTime? instant)
    {
        instant = null; if (value is null) return true;
        if (value.Length > 64 || !Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant) ||
            !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
        instant = parsed.UtcDateTime; return true;
    }
    private static readonly HashSet<string> Currencies = RegisteredCurrencies();
    private static HashSet<string> RegisteredCurrencies()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try { var code = new RegionInfo(culture.Name).ISOCurrencySymbol; if (Regex.IsMatch(code, "^[A-Z]{3}$", RegexOptions.CultureInvariant)) result.Add(code); }
            catch (ArgumentException) { }
        }
        return result;
    }
}
