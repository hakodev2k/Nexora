using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Nexora.Application.DeveloperTools;
using Nexora.Application.Identity;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Persistence;

namespace Nexora.Infrastructure.DeveloperTools;

/// <summary>
/// Deterministic, memory-only developer utilities. This slice never executes
/// user code, contacts a provider, persists input/output or reads the browser
/// clipboard. Network tools remain a separate, disabled capability.
/// </summary>
public sealed class LocalToolboxService : IToolboxService
{
    private const int MaxInputBytes = 1024 * 1024;
    private const int MaxOutputBytes = 2 * 1024 * 1024;
    private const int RegexInputBytes = 100 * 1024;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private readonly SqlSelfCapability _capabilities;

    private static readonly IReadOnlyList<ToolboxTool> Tools =
    [
        new("base64", "Base64", "Encoding", "UTF-8 Base64 encode/decode.", "Local", "toolbox.base64.run", true),
        new("url-codec", "URL codec", "Encoding", "Percent-encode/decode text without fetching it.", "Local", "toolbox.url_codec.run", true),
        new("html-codec", "HTML entities", "Encoding", "Encode/decode HTML entities as text.", "Local", "toolbox.html_codec.run", true),
        new("hash", "Hash", "Security", "SHA-256/384/512 and legacy checksum digests.", "Local", "toolbox.hash.run", true),
        new("uuid", "UUID", "Generators", "Generate UUID v4 values with the platform CSPRNG.", "Local", "toolbox.uuid.run", true),
        new("password", "Password", "Generators", "Generate a memory-only random password.", "Local", "toolbox.password.run", true),
        new("json", "JSON formatter", "Data", "Validate and indent JSON without execution.", "Local", "toolbox.json.run", true),
        new("regex", "Regex tester", "Text", "Bounded regex matching against pasted text.", "Local", "toolbox.regex.run", true)
    ];

    public LocalToolboxService(SqlConnectionFactory connections)
    {
        _capabilities = new SqlSelfCapability(connections);
    }

    public IdentityOperationResult<ToolboxCatalog> Catalog(IdentityPrincipal actor)
    {
        return Allowed(actor, "toolbox.catalog.read")
            ? IdentityOperationResult<ToolboxCatalog>.Success(new ToolboxCatalog(Tools))
            : ModuleUnavailable<ToolboxCatalog>();
    }

    public IdentityOperationResult<ToolboxRunResult> Run(IdentityPrincipal actor, ToolboxRunCommand command)
    {
        var code = command.ToolCode?.Trim().ToLowerInvariant() ?? string.Empty;
        var tool = Tools.FirstOrDefault(item => item.Code == code);
        if (tool is null)
            return Failure<ToolboxRunResult>("ToolUnavailable", 404, "Tool is not available in the local toolbox slice.");
        if (!Allowed(actor, tool.ActionKey))
            return ModuleUnavailable<ToolboxRunResult>();
        if (command.Input is null)
            return Failure<ToolboxRunResult>("ValidationFailed", 422, "Tool input is required.");
        if (Encoding.UTF8.GetByteCount(command.Input) > MaxInputBytes)
            return Failure<ToolboxRunResult>("InputTooLarge", 422, "Tool input must be at most 1 MiB UTF-8.");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var (output, warning, errorPath) = code switch
            {
                "base64" => RunBase64(command.Input, command.Options),
                "url-codec" => RunUrlCodec(command.Input, command.Options),
                "html-codec" => RunHtmlCodec(command.Input, command.Options),
                "hash" => RunHash(command.Input, command.Options),
                "uuid" => RunUuid(command.Options),
                "password" => RunPassword(command.Options),
                "json" => RunJson(command.Input, command.Options),
                "regex" => RunRegex(command.Input, command.Options),
                _ => throw new ToolFailureException("ToolUnavailable", "Tool is not available in the local toolbox slice.")
            };
            if (Encoding.UTF8.GetByteCount(output) > MaxOutputBytes)
                return Failure<ToolboxRunResult>("OutputTooLarge", 422, "Tool output exceeds the 2 MiB limit.");
            stopwatch.Stop();
            return IdentityOperationResult<ToolboxRunResult>.Success(
                new ToolboxRunResult(code, output, warning, errorPath, (int)Math.Min(int.MaxValue, stopwatch.ElapsedMilliseconds)),
                200, "ToolRunCompleted");
        }
        catch (ToolFailureException failure)
        {
            return Failure<ToolboxRunResult>(failure.Code, 422, failure.Message);
        }
        catch (RegexMatchTimeoutException)
        {
            return Failure<ToolboxRunResult>("RegexTimeout", 422, "Regex evaluation exceeded the 250 ms safety limit.");
        }
        catch (RegexParseException exception)
        {
            return Failure<ToolboxRunResult>("RegexInvalid", 422, $"Regex pattern is invalid: {exception.Message}");
        }
        catch (JsonException exception)
        {
            return Failure<ToolboxRunResult>("JsonInvalid", 422, $"JSON is invalid at {exception.Path ?? "$"}.");
        }
        catch (FormatException)
        {
            return Failure<ToolboxRunResult>("InputInvalid", 422, "Input is not valid for the selected operation.");
        }
    }

    private bool Allowed(IdentityPrincipal actor, string actionKey) =>
        _capabilities.IsAllowed(actor, "FX32", actionKey);

    private static (string Output, string? Warning, string? ErrorPath) RunBase64(
        string input, IReadOnlyDictionary<string, string>? options)
    {
        var operation = Option(options, "operation", "encode");
        return operation switch
        {
            "encode" => (Convert.ToBase64String(Encoding.UTF8.GetBytes(input)), null, null),
            "decode" => (StrictUtf8.GetString(Convert.FromBase64String(input)), null, null),
            _ => throw new ToolFailureException("ValidationFailed", "Base64 operation must be encode or decode.")
        };
    }

    private static (string Output, string? Warning, string? ErrorPath) RunUrlCodec(
        string input, IReadOnlyDictionary<string, string>? options)
    {
        var operation = Option(options, "operation", "encode");
        return operation switch
        {
            "encode" => (Uri.EscapeDataString(input), null, null),
            "decode" => (Uri.UnescapeDataString(input), null, null),
            _ => throw new ToolFailureException("ValidationFailed", "URL operation must be encode or decode.")
        };
    }

    private static (string Output, string? Warning, string? ErrorPath) RunHtmlCodec(
        string input, IReadOnlyDictionary<string, string>? options)
    {
        var operation = Option(options, "operation", "encode");
        return operation switch
        {
            "encode" => (WebUtility.HtmlEncode(input), null, null),
            "decode" => (WebUtility.HtmlDecode(input), null, null),
            _ => throw new ToolFailureException("ValidationFailed", "HTML operation must be encode or decode.")
        };
    }

    private static (string Output, string? Warning, string? ErrorPath) RunHash(
        string input, IReadOnlyDictionary<string, string>? options)
    {
        var algorithm = Option(options, "algorithm", "SHA-256").ToUpperInvariant();
        using var hash = algorithm switch
        {
            "SHA-256" or "SHA256" => SHA256.Create(),
            "SHA-384" or "SHA384" => SHA384.Create(),
            "SHA-512" or "SHA512" => SHA512.Create(),
            "MD5" => MD5.Create(),
            "SHA-1" or "SHA1" => SHA1.Create(),
            _ => throw new ToolFailureException("ValidationFailed", "Hash algorithm must be SHA-256, SHA-384, SHA-512, MD5 or SHA-1.")
        };
        var warning = algorithm is "MD5" or "SHA-1" ? "Legacy checksum only; do not use for password or signature security." : null;
        return (Convert.ToHexString(hash.ComputeHash(Encoding.UTF8.GetBytes(input))).ToLowerInvariant(), warning, null);
    }

    private static (string Output, string? Warning, string? ErrorPath) RunUuid(
        IReadOnlyDictionary<string, string>? options)
    {
        var count = ParseInt(options, "count", 1, 20);
        var values = Enumerable.Range(0, count).Select(_ => Guid.NewGuid().ToString()).ToArray();
        return (string.Join('\n', values), null, null);
    }

    private static (string Output, string? Warning, string? ErrorPath) RunPassword(
        IReadOnlyDictionary<string, string>? options)
    {
        var length = ParseInt(options, "length", 15, 128);
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*_-";
        var chars = new char[length];
        for (var index = 0; index < chars.Length; index++)
            chars[index] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        return (new string(chars), "Generated locally; output is shown once and is not persisted.", null);
    }

    private static (string Output, string? Warning, string? ErrorPath) RunJson(
        string input, IReadOnlyDictionary<string, string>? options)
    {
        using var document = JsonDocument.Parse(input);
        var indent = Option(options, "indent", "true");
        var output = JsonSerializer.Serialize(document.RootElement,
            new JsonSerializerOptions { WriteIndented = !string.Equals(indent, "false", StringComparison.OrdinalIgnoreCase) });
        return (output, null, null);
    }

    private static (string Output, string? Warning, string? ErrorPath) RunRegex(
        string input, IReadOnlyDictionary<string, string>? options)
    {
        if (Encoding.UTF8.GetByteCount(input) > RegexInputBytes)
            throw new ToolFailureException("InputTooLarge", "Regex sample must be at most 100 KiB UTF-8.");
        var pattern = Option(options, "pattern", string.Empty);
        if (string.IsNullOrWhiteSpace(pattern) || pattern.Length > 10_000)
            throw new ToolFailureException("ValidationFailed", "Regex pattern is required and must be at most 10,000 characters.");
        var optionsValue = RegexOptions.CultureInvariant;
        if (string.Equals(Option(options, "ignoreCase", "false"), "true", StringComparison.OrdinalIgnoreCase))
            optionsValue |= RegexOptions.IgnoreCase;
        var regex = new Regex(pattern, optionsValue, RegexTimeout);
        var matches = regex.Matches(input).Cast<Match>().Take(100)
            .Select(match => new { index = match.Index, length = match.Length, value = match.Value })
            .ToArray();
        var output = JsonSerializer.Serialize(new { count = matches.Length, truncated = regex.Matches(input).Count > matches.Length, matches },
            new JsonSerializerOptions { WriteIndented = true });
        return (output, null, null);
    }

    private static string Option(IReadOnlyDictionary<string, string>? options, string key, string fallback) =>
        options is not null && options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallback;

    private static int ParseInt(IReadOnlyDictionary<string, string>? options, string key, int min, int max)
    {
        var value = Option(options, key, min.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return int.TryParse(value, out var parsed) && parsed is >= min and <= max
            ? parsed
            : throw new ToolFailureException("ValidationFailed", $"{key} must be between {min} and {max}.");
    }

    private static IdentityOperationResult<T> Failure<T>(string code, int status, string title) =>
        IdentityOperationResult<T>.Failure(code, status, title);

    private static IdentityOperationResult<T> ModuleUnavailable<T>() =>
        Failure<T>("ModuleUnavailable", 409, "Developer Toolbox is disabled or unavailable for this user.");

    private sealed class ToolFailureException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }
}
