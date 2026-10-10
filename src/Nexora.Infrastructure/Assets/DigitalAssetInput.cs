using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using Nexora.Application.Assets;

namespace Nexora.Infrastructure.Assets;

internal static class DigitalAssetInput
{
    internal static bool Kind(string? value) => value is "Domain" or "Hosting" or "Vps" or "Certificate" or "License" or "OnlineService";
    private static readonly HashSet<string> Currencies = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
        .Select(c => { try { return new RegionInfo(c.Name).ISOCurrencySymbol; } catch (ArgumentException) { return ""; } })
        .Where(c => c.Length == 3).ToHashSet(StringComparer.Ordinal);
    internal static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool Length(string? text, int max) => text is null || text.Length <= max;
    internal static bool Amount(string? amount, string? currency, out decimal? number)
    {
        number = null;
        if (amount is null) return currency is null;
        if (currency is null || !Currencies.Contains(currency) || !Regex.IsMatch(amount, @"^(?:0|[1-9]\d{0,19})(?:\.\d{1,8})?$", RegexOptions.CultureInvariant) ||
            !decimal.TryParse(amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)) return false;
        number = parsed; return true;
    }
    internal static bool Instant(string? text, out DateTime? instant)
    {
        instant = null; if (text is null) return true;
        if (text.Length > 64 || !Regex.IsMatch(text, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant) ||
            !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
        instant = parsed.UtcDateTime; return true;
    }
    private static string? NormalizeInstant(string? input) => input is null ? null : DateTimeOffset.Parse(input, CultureInfo.InvariantCulture).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    internal static bool Domain(string? input, out string? ascii, out string? unicode)
    {
        ascii = unicode = null;
        if (input is null) return true;
        if (string.IsNullOrWhiteSpace(input) || input.Length > 253 || input.Any(char.IsWhiteSpace) || input.IndexOfAny(['/', ':', '@', '*', '\\', '?', '#']) >= 0) return false;
        try
        {
            var map = new IdnMapping { UseStd3AsciiRules = true };
            ascii = map.GetAscii(input.Normalize(NormalizationForm.FormC)).ToLowerInvariant();
            unicode = map.GetUnicode(ascii);
            return ascii.Length <= 253 && unicode.Length <= 253 && ascii.Split('.').All(label => label.Length is > 0 and <= 63 && label[0] != '-' && label[^1] != '-');
        }
        catch (ArgumentException) { return false; }
    }
    private static bool Url(string? text) => text is null || (text.Length <= 2048 && Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
        uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo) && !string.IsNullOrEmpty(uri.Host));
    private static bool Bigint(string? text) => text is null || (text.Length <= 19 && Regex.IsMatch(text, @"^(?:0|[1-9]\d*)$", RegexOptions.CultureInvariant) && long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _));
    internal static bool Normalize(DigitalAssetMetadata? input, out DigitalAssetMetadata? result)
    {
        result = null;
        if (input is null || string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200 || !Kind(input.Kind) || input.Details is null ||
            !Length(input.Provider, 200) || !Length(input.RenewalCycle, 100) || !Length(input.Notes, 20000) || !Amount(input.Cost, input.Currency, out _)) return false;
        var d = input.Details;
        if (new object?[] { d.Domain, d.Hosting, d.Vps, d.Certificate, d.License, d.OnlineService }.Count(v => v is not null) != 1) return false;
        DigitalAssetDetails? detail = null;
        switch (input.Kind)
        {
            case "Domain" when d.Domain is { } v:
                if (v.Name is null || !Domain(v.Name, out var ascii, out _) || !Length(v.Registrar, 200) || !Length(v.NameserverNotes, 20000)) return false;
                detail = new(Domain: v with { Name = ascii!, Registrar = Blank(v.Registrar), NameserverNotes = Blank(v.NameserverNotes) }); break;
            case "Hosting" when d.Hosting is { } v:
                if (!Length(v.Plan, 200) || !Length(v.Region, 100) || !Url(v.ControlPanelUrl) || !Bigint(v.StorageLimitBytes) || !Domain(v.DomainName, out var name, out _)) return false;
                detail = new(Hosting: v with { Plan = Blank(v.Plan), Region = Blank(v.Region), DomainName = name }); break;
            case "Vps" when d.Vps is { } v:
                if (!Domain(v.HostName, out var vpsName, out _) || !Length(v.IpAddress, 45) || (v.IpAddress is not null && !IPAddress.TryParse(v.IpAddress, out _)) ||
                    v.CpuCount is <= 0 || !Bigint(v.MemoryMiB) || !Length(v.OperatingSystem, 200) || !Length(v.Plan, 200) || !Length(v.Region, 100)) return false;
                detail = new(Vps: v with { HostName = vpsName, IpAddress = v.IpAddress is null ? null : IPAddress.Parse(v.IpAddress).ToString(),
                    OperatingSystem = Blank(v.OperatingSystem), Plan = Blank(v.Plan), Region = Blank(v.Region) }); break;
            case "Certificate" when d.Certificate is { } v:
                if (string.IsNullOrWhiteSpace(v.Subject) || v.Subject.Length > 500 || !Length(v.Issuer, 500) || !Length(v.Fingerprint, 200) ||
                    !Instant(v.NotBefore, out var before) || !Instant(v.NotAfter, out var after) || (before is not null && after is not null && before >= after) ||
                    !Domain(v.HostName, out var certificateName, out _) || v.SubjectAlternativeNames is { Count: > 100 }) return false;
                var sans = new List<string>();
                foreach (var san in v.SubjectAlternativeNames ?? [])
                {
                    if (string.IsNullOrWhiteSpace(san) || san.Length > 253 || san.Any(char.IsControl)) return false;
                    string normalized;
                    try { normalized = san.Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant(); }
                    catch (ArgumentException) { return false; }
                    if (normalized.Length is 0 or > 253) return false;
                    if (sans.Contains(normalized, StringComparer.Ordinal)) return false;
                    sans.Add(normalized);
                }
                if (Encoding.Unicode.GetByteCount(JsonSerializer.Serialize(sans)) > 60000) return false;
                detail = new(Certificate: v with { Subject = v.Subject.Trim(), Issuer = Blank(v.Issuer), Fingerprint = Blank(v.Fingerprint),
                    HostName = certificateName, NotBefore = NormalizeInstant(v.NotBefore), NotAfter = NormalizeInstant(v.NotAfter), SubjectAlternativeNames = sans }); break;
            case "License" when d.License is { } v:
                if (string.IsNullOrWhiteSpace(v.Product) || v.Product.Length > 200 || v.Seats is <= 0 || !Length(v.Vendor, 200) || !Length(v.Edition, 200)) return false;
                detail = new(License: v with { Product = v.Product.Trim(), Vendor = Blank(v.Vendor), Edition = Blank(v.Edition) }); break;
            case "OnlineService" when d.OnlineService is { } v:
                if (!Url(v.ServiceUrl) || !Length(v.Plan, 200)) return false;
                detail = new(OnlineService: v with { Plan = Blank(v.Plan) }); break;
        }
        if (detail is null) return false;
        result = input with { Title = input.Title.Trim(), Provider = Blank(input.Provider), RenewalCycle = Blank(input.RenewalCycle), Notes = Blank(input.Notes), Details = detail };
        return true;
    }
}
