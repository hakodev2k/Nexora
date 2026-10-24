using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Nexora.Infrastructure.DeveloperTools;

public sealed partial class LocalToolboxService
{
    private static (string Output, string? Warning, string? ErrorPath) RunOffline(
        string code, string input, IReadOnlyDictionary<string, string>? options)
    {
        ValidateOfflineOptions(code, options);
        return code switch
        {
            "xml" => FormatXml(input, options),
            "datetime" => ConvertEpoch(input, options),
            "csv" => ViewCsv(input, options),
            // Payload-free authority check only. Tokens belong exclusively in the browser decoder.
            "jwt" when input.Length == 0 => (string.Empty, "Unverified: decode is not signature verification. Decode only in the browser.", null),
            "jwt" => throw new ToolFailureException("ValidationFailed", "JWT authority checks require empty input; decode only in the browser."),
            _ => throw new ToolFailureException("ToolUnavailable", "Tool is unavailable.")
        };
    }

    private static void ValidateOfflineOptions(string code, IReadOnlyDictionary<string, string>? options)
    {
        if (options is null) return;
        if (options.Count > 8) throw new ToolFailureException("ValidationFailed", "Too many tool options.");
        foreach (var (key, value) in options)
        {
            if (value is null || value.Length > 64 || !((code, key, value) switch
                {
                    ("xml", "indent", "true" or "false") => true,
                    ("datetime", "unit", "seconds" or "milliseconds") => true,
                    ("csv", "delimiter", "comma" or "semicolon" or "tab") => true,
                    ("csv", "header", "true" or "false") => true,
                    ("csv", "quote", "double") => true,
                    ("csv", "encoding", "utf-8") => true,
                    _ => false
                }))
                throw new ToolFailureException("ValidationFailed", "Unsupported tool option or value.");
        }
    }

    private static (string, string?, string?) FormatXml(string input, IReadOnlyDictionary<string, string>? options)
    {
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxInputBytes,
                MaxCharactersFromEntities = MaxInputBytes,
                IgnoreWhitespace = false,
                CloseInput = true
            };
            // Validate shape before constructing a tree; attributes count toward the node budget.
            using (var reader = XmlReader.Create(new StringReader(input), settings))
            {
                var nodes = 0;
                while (reader.Read())
                {
                    nodes += 1 + reader.AttributeCount;
                    if (reader.Depth >= 64 || nodes > 50_000)
                        throw new ToolFailureException("XmlLimitExceeded", "XML exceeds depth 64 or 50,000 nodes including attributes.");
                }
            }
            using var source = XmlReader.Create(new StringReader(input), settings);
            var document = XDocument.Load(source, LoadOptions.PreserveWhitespace);
            document.Declaration = null;
            // Pretty-printing a mixed-content tree can insert significant text
            // beside elements/CDATA. Preserve its character data verbatim.
            var mixedContent = document.Descendants().Any(element =>
                element.Elements().Any() && element.Nodes().Any(node => node is XText));
            using var stream = new BoundedXmlStream();
            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = true,
                Indent = !mixedContent && Option(options, "indent", "true") == "true",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Entitize,
                CloseOutput = false
            }))
                document.Save(writer);
            return (StrictUtf8.GetString(stream.ToArray()),
                "Formatting preserves supplied whitespace and mixed content; lexical layout may change. DTD/external entities are prohibited.", null);
        }
        catch (XmlException error)
        {
            throw new ToolFailureException("XmlInvalid", $"XML is invalid or prohibited at line {error.LineNumber}, column {error.LinePosition}.");
        }
    }

    private sealed class BoundedXmlStream : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count)
        {
            Check(count);
            base.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Check(buffer.Length);
            base.Write(buffer);
        }

        private void Check(int count)
        {
            if (Length + count > MaxOutputBytes)
                throw new ToolFailureException("OutputTooLarge", "Tool output exceeds the 2 MiB limit.");
        }
    }

    private static (string, string?, string?) ConvertEpoch(string input, IReadOnlyDictionary<string, string>? options)
    {
        var unit = Option(options, "unit", string.Empty);
        if (unit is not ("seconds" or "milliseconds"))
            throw new ToolFailureException("ValidationFailed", "Select an explicit epoch unit: seconds or milliseconds.");
        var digits = input.AsSpan();
        if (digits.Length > 0 && digits[0] == '-') digits = digits[1..];
        if (digits.Length == 0 || digits.ContainsAnyExceptInRange('0', '9') ||
            !long.TryParse(input, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var epoch))
            throw new ToolFailureException("DateTimeInvalid", "Epoch must be a signed integer within the supported UTC range.");
        try
        {
            var instant = unit == "seconds" ? DateTimeOffset.FromUnixTimeSeconds(epoch) : DateTimeOffset.FromUnixTimeMilliseconds(epoch);
            return (JsonSerializer.Serialize(new
            {
                unit,
                epoch = epoch.ToString(CultureInfo.InvariantCulture),
                utc = instant.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)
            }, new JsonSerializerOptions { WriteIndented = true }), null, null);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ToolFailureException("DateTimeInvalid", "Epoch is outside UTC years 0001 through 9999 for the selected unit.");
        }
    }

    private static (string, string?, string?) ViewCsv(string input, IReadOnlyDictionary<string, string>? options)
    {
        var delimiter = Option(options, "delimiter", "comma") switch { "semicolon" => ';', "tab" => '\t', _ => ',' };
        var rows = new List<string[]>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        var closed = false;
        var cells = 0;
        var width = -1;

        void Invalid() => throw new ToolFailureException("CsvInvalid", $"Malformed CSV at record {rows.Count + 1}; check quotes and column count.");
        void AddCell()
        {
            if (row.Count >= 100 || ++cells > 50_000)
                throw new ToolFailureException("CsvLimitExceeded", "CSV exceeds 100 columns or 50,000 cells.");
            row.Add(cell.ToString());
            cell.Clear();
            closed = false;
        }
        void AddRow()
        {
            AddCell();
            if (rows.Count >= 1000) throw new ToolFailureException("CsvLimitExceeded", "CSV exceeds 1,000 records including header.");
            if (width >= 0 && row.Count != width) Invalid();
            width = row.Count;
            rows.Add(row.ToArray());
            row.Clear();
        }

        for (var index = 0; index < input.Length; index++)
        {
            var character = input[index];
            if (quoted)
            {
                if (character == '"')
                {
                    if (index + 1 < input.Length && input[index + 1] == '"') { cell.Append('"'); index++; }
                    else { quoted = false; closed = true; }
                }
                else cell.Append(character);
            }
            else if (character == delimiter) AddCell();
            else if (character is '\r' or '\n')
            {
                AddRow();
                if (character == '\r' && index + 1 < input.Length && input[index + 1] == '\n') index++;
            }
            else if (closed) Invalid();
            else if (character == '"')
            {
                if (cell.Length != 0) Invalid();
                quoted = true;
            }
            else cell.Append(character);
            if (cell.Length > 16_384)
                throw new ToolFailureException("CsvLimitExceeded", "CSV cell exceeds 16,384 UTF-16 code units.");
        }
        if (quoted) Invalid();
        if (input.Length > 0 && (row.Count > 0 || cell.Length > 0 || closed || input[^1] == delimiter)) AddRow();
        var hasHeader = Option(options, "header", "false") == "true" && rows.Count > 0;
        return (JsonSerializer.Serialize(new { header = hasHeader ? rows[0] : null, rows = hasHeader ? rows.Skip(1).ToArray() : rows.ToArray() },
            new JsonSerializerOptions { WriteIndented = true }),
            "CSV values, including formula prefixes (=, +, -, @), are literal text. JSON display only; pasting into a spreadsheet can execute formulas.", null);
    }
}
