using System.Text.Json;
using System.Xml.Linq;
using Nexora.Application.DeveloperTools;
using Nexora.Application.Identity;
using Nexora.Infrastructure.DeveloperTools;
using Xunit;

namespace Nexora.UnitTests;

public sealed class ToolboxExtraOfflineTests
{
    private static IdentityOperationResult<ToolboxRunResult> Run(string code, string input, params (string Key, string Value)[] options) =>
        LocalToolboxService.ExecutePure(new(code, input, options.ToDictionary(x => x.Key, x => x.Value)));

    private static ToolboxRunResult Success(IdentityOperationResult<ToolboxRunResult> result)
    {
        Assert.True(result.Succeeded, result.Code + ": " + result.Title);
        Assert.Equal(200, result.StatusCode);
        return Assert.IsType<ToolboxRunResult>(result.Value);
    }

    private static void Rejected(IdentityOperationResult<ToolboxRunResult> result, string code)
    {
        Assert.False(result.Succeeded);
        Assert.Equal(422, result.StatusCode);
        Assert.Equal(code, result.Code);
        Assert.Null(result.Value);
        Assert.DoesNotContain("synthetic-canary", result.Title);
    }

    [Fact]
    public void Xml_preserves_unicode_mixed_content_cdata_comments_and_namespace_without_declaration_claim()
    {
        const string input = "<?xml version=\"1.0\" encoding=\"utf-16\"?><r xmlns=\"urn:synthetic\"><p>Hello <b>日本語 😀</b> world</p><![CDATA[<literal>]]><!--text--></r>";
        var result = Success(Run("xml", input));
        Assert.True(XNode.DeepEquals(XDocument.Parse(input, LoadOptions.PreserveWhitespace),
            XDocument.Parse(result.Output, LoadOptions.PreserveWhitespace)));
        Assert.DoesNotContain("encoding=", result.Output);
        Assert.Contains("DTD", result.Warning);
        Assert.Contains("Hello <b>日本語 😀</b> world", result.Output);
    }

    [Fact]
    public void Xml_pretty_and_compact_are_explicit_and_existing_whitespace_is_preserved()
    {
        Assert.Contains("\n", Success(Run("xml", "<r><a/></r>")).Output);
        Assert.DoesNotContain("\n", Success(Run("xml", "<r><a/></r>", ("indent", "false"))).Output);
        Assert.Contains("  text  ", Success(Run("xml", "<r>  text  </r>")).Output);
    }

    [Theory]
    [InlineData("<!DOCTYPE r [<!ENTITY e 'synthetic-canary'>]><r>&e;</r>")]
    [InlineData("<!DOCTYPE r SYSTEM 'file:///synthetic-canary'><r/>")]
    [InlineData("<!DOCTYPE r [<!ENTITY e SYSTEM 'http://127.0.0.1/synthetic-canary'>]><r>&e;</r>")]
    [InlineData("<synthetic-canary><broken></synthetic-canary>")]
    [InlineData("")]
    public void Xml_rejects_all_dtd_forms_and_malformed_documents_without_echo(string input) =>
        Rejected(Run("xml", input), "XmlInvalid");

    [Fact]
    public void Xml_depth_nodes_attributes_and_output_are_bounded()
    {
        Success(Run("xml", string.Concat(Enumerable.Repeat("<a>", 64)) + string.Concat(Enumerable.Repeat("</a>", 64))));
        Rejected(Run("xml", string.Concat(Enumerable.Repeat("<a>", 65)) + string.Concat(Enumerable.Repeat("</a>", 65))), "XmlLimitExceeded");
        Rejected(Run("xml", "<r>" + string.Concat(Enumerable.Repeat("<a/>", 50_000)) + "</r>"), "XmlLimitExceeded");
        var attributes = "<r " + string.Join(' ', Enumerable.Range(0, 50_000).Select(i => $"a{i}='x'")) + "/>";
        Rejected(Run("xml", attributes), "XmlLimitExceeded");
        var expandedIndent = string.Concat(Enumerable.Repeat("<a>", 63)) + string.Concat(Enumerable.Repeat("<b/>", 20_000)) + string.Concat(Enumerable.Repeat("</a>", 63));
        Rejected(Run("xml", expandedIndent), "OutputTooLarge");
    }

    [Theory]
    [InlineData("0", "seconds", "1970-01-01T00:00:00.000Z")]
    [InlineData("-1", "seconds", "1969-12-31T23:59:59.000Z")]
    [InlineData("-1", "milliseconds", "1969-12-31T23:59:59.999Z")]
    [InlineData("-62135596800", "seconds", "0001-01-01T00:00:00.000Z")]
    [InlineData("253402300799", "seconds", "9999-12-31T23:59:59.000Z")]
    [InlineData("-62135596800000", "milliseconds", "0001-01-01T00:00:00.000Z")]
    [InlineData("253402300799999", "milliseconds", "9999-12-31T23:59:59.999Z")]
    [InlineData("1772953200", "seconds", "2026-03-08T07:00:00.000Z")]
    public void Epoch_uses_explicit_unit_and_exact_utc_even_at_dst_boundary(string input, string unit, string utc)
    {
        using var json = JsonDocument.Parse(Success(Run("datetime", input, ("unit", unit))).Output);
        Assert.Equal(input, json.RootElement.GetProperty("epoch").GetString());
        Assert.Equal(unit, json.RootElement.GetProperty("unit").GetString());
        Assert.Equal(utc, json.RootElement.GetProperty("utc").GetString());
    }

    [Theory]
    [InlineData("253402300800", "seconds")][InlineData("-62135596801", "seconds")]
    [InlineData("253402300800000", "milliseconds")][InlineData("-62135596800001", "milliseconds")]
    [InlineData("9007199254740993", "milliseconds")][InlineData("99999999999999999999999", "seconds")]
    [InlineData("1.5", "seconds")][InlineData("1e3", "seconds")][InlineData("+1", "seconds")]
    [InlineData(" 0", "seconds")][InlineData("0 ", "seconds")][InlineData("", "seconds")]
    [InlineData("synthetic-canary", "seconds")]
    public void Epoch_rejects_lossy_ambiguous_or_out_of_range_inputs(string input, string unit) =>
        Rejected(Run("datetime", input, ("unit", unit)), "DateTimeInvalid");

    [Fact]
    public void Epoch_never_guesses_a_missing_unit() => Rejected(Run("datetime", "0"), "ValidationFailed");

    [Fact]
    public void Csv_preserves_headers_duplicates_unicode_formulas_and_quoted_newlines_as_text()
    {
        var result = Success(Run("csv", "same,same\r\n\"a,\"\"b\"\"\",\"日本語\r\n😀\"\r\n=1+1,@SUM(A1)\r\n", ("header", "true")));
        using var doc = JsonDocument.Parse(result.Output);
        Assert.Equal(new[] { "same", "same" }, doc.RootElement.GetProperty("header").EnumerateArray().Select(x => x.GetString()));
        var rows = doc.RootElement.GetProperty("rows");
        Assert.Equal(2, rows.GetArrayLength());
        Assert.Equal("a,\"b\"", rows[0][0].GetString());
        Assert.Equal("日本語\r\n😀", rows[0][1].GetString());
        Assert.Equal("=1+1", rows[1][0].GetString());
        Assert.Equal("@SUM(A1)", rows[1][1].GetString());
        Assert.Contains("spreadsheet", result.Warning);
    }

    [Theory]
    [InlineData("comma", "a,b,", 3)][InlineData("semicolon", "a;b;", 3)][InlineData("tab", "a\tb\t", 3)]
    public void Csv_delimiter_and_trailing_empty_cell_are_explicit(string delimiter, string input, int columns)
    {
        using var doc = JsonDocument.Parse(Success(Run("csv", input, ("delimiter", delimiter))).Output);
        var row = doc.RootElement.GetProperty("rows")[0];
        Assert.Equal(columns, row.GetArrayLength());
        Assert.Equal("", row[columns - 1].GetString());
    }

    [Theory][InlineData("")][InlineData("\"\"")][InlineData("\r\n")]
    public void Csv_empty_input_and_empty_record_are_distinct(string input)
    {
        using var doc = JsonDocument.Parse(Success(Run("csv", input)).Output);
        Assert.Equal(input.Length == 0 ? 0 : 1, doc.RootElement.GetProperty("rows").GetArrayLength());
    }

    [Theory][InlineData("a\"b,c")][InlineData("\"a\"x,c")][InlineData("\"synthetic-canary")]
    [InlineData("a,b\nc")][InlineData("a,b\nc,d,e")]
    public void Csv_rejects_malformed_quotes_and_ragged_records_without_partial_success(string input) => Rejected(Run("csv", input), "CsvInvalid");

    [Fact]
    public void Csv_enforces_rows_columns_cell_and_total_cell_budgets()
    {
        Success(Run("csv", string.Join('\n', Enumerable.Repeat("x", 1000))));
        Rejected(Run("csv", string.Join('\n', Enumerable.Repeat("x", 1001))), "CsvLimitExceeded");
        Success(Run("csv", string.Join(',', Enumerable.Repeat("x", 100))));
        Rejected(Run("csv", string.Join(',', Enumerable.Repeat("x", 101))), "CsvLimitExceeded");
        Success(Run("csv", new string('x', 16_384)));
        Rejected(Run("csv", new string('x', 16_385)), "CsvLimitExceeded");
        Rejected(Run("csv", string.Join('\n', Enumerable.Repeat(string.Join(',', Enumerable.Repeat("x", 100)), 501))), "CsvLimitExceeded");
    }

    [Theory][InlineData("csv", "encoding", "utf-16")][InlineData("csv", "quote", "single")]
    [InlineData("csv", "delimiter", "|")][InlineData("xml", "indent", "yes")]
    [InlineData("datetime", "unit", "ms")][InlineData("xml", "unknown", "synthetic-canary")]
    [InlineData("jwt", "token", "synthetic-canary")]
    public void New_tools_reject_unknown_or_unsupported_options(string tool, string key, string value) =>
        Rejected(Run(tool, "", (key, value)), "ValidationFailed");

    [Fact]
    public void Jwt_server_only_checks_authority_with_empty_payload_and_never_decodes_tokens()
    {
        var result = Success(Run("jwt", ""));
        Assert.Equal("", result.Output);
        Assert.Contains("Unverified", result.Warning);
        Rejected(Run("jwt", "synthetic-canary"), "ValidationFailed");
    }

    [Fact]
    public void Shared_input_and_option_limits_reject_payloads_before_parsing()
    {
        Rejected(Run("xml", new string('é', 524_289)), "InputTooLarge");
        Rejected(Run("csv", "x", ("delimiter", new string('x', 65))), "ValidationFailed");
        var options = Enumerable.Range(0, 9).Select(x => ("x" + x, "value")).ToArray();
        Rejected(Run("xml", "<r/>", options), "ValidationFailed");
    }
}
