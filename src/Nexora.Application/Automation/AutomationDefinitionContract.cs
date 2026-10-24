using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Nexora.Application.Automation;

public sealed record AutomationContractReference(string Key, int Version);

public sealed record AutomationTaskCreateInputs(
    Guid ProjectId, string Title, string? Description,
    DateTimeOffset StartAt, DateTimeOffset EndAt, string Priority);

public sealed record AutomationDefinitionStep(
    int Id, AutomationContractReference Action, AutomationTaskCreateInputs Inputs);

public sealed record AutomationDefinitionShape(
    int SchemaVersion, AutomationContractReference Trigger,
    ImmutableArray<AutomationDefinitionStep> Steps);

public sealed record AutomationShapeResult(
    bool Accepted, string Code, AutomationDefinitionShape? Definition);

// Shape acceptance deliberately does not validate current permissions, resources,
// SQL version state, scheduling, runtime adapters or approval to execute.
public static class AutomationDefinitionContract
{
    public const int MaximumUtf8Bytes = 64 * 1024;
    public const int MaximumSteps = 20;

    public static AutomationShapeResult Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaximumUtf8Bytes || HasUnpairedSurrogates(json) ||
            Encoding.UTF8.GetByteCount(json) > MaximumUtf8Bytes)
            return Reject("DefinitionSizeInvalid");

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                MaxDepth = 8,
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });
            var root = document.RootElement;
            if (!Fields(root, ["schemaVersion", "trigger", "steps"]) ||
                !Integer(root.GetProperty("schemaVersion"), out var schema) || schema != 1)
                return Reject("DefinitionSchemaInvalid");

            if (!Reference(root.GetProperty("trigger"), out var trigger) ||
                AutomationContractRegistry.FindTrigger(trigger!.Key, trigger.Version) is null)
                return Reject("TriggerUnsupported");

            var steps = root.GetProperty("steps");
            if (steps.ValueKind != JsonValueKind.Array ||
                steps.GetArrayLength() is < 1 or > MaximumSteps)
                return Reject("StepCountInvalid");

            var ids = new HashSet<int>();
            var parsed = ImmutableArray.CreateBuilder<AutomationDefinitionStep>();
            foreach (var step in steps.EnumerateArray())
            {
                if (!Fields(step, ["id", "action", "inputs"]) ||
                    !Integer(step.GetProperty("id"), out var id) || id <= 0 || !ids.Add(id))
                    return Reject("StepSchemaInvalid");
                if (!Reference(step.GetProperty("action"), out var action) ||
                    AutomationContractRegistry.FindAction(action!.Key, action.Version) is null)
                    return Reject("ActionUnsupported");
                if (!TaskInputs(step.GetProperty("inputs"), out var inputs))
                    return Reject("ActionInputsInvalid");
                parsed.Add(new(id, action!, inputs!));
            }
            return new(true, "DefinitionShapeAccepted", new(schema, trigger!, parsed.ToImmutable()));
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return Reject("DefinitionJsonInvalid");
        }
    }

    public static bool SupportsVerifiedDryRun(AutomationDefinitionShape definition) =>
        definition.SchemaVersion == 1 && !definition.Steps.IsDefaultOrEmpty &&
        definition.Steps.Length <= MaximumSteps &&
        AutomationContractRegistry.FindTrigger(definition.Trigger.Key, definition.Trigger.Version)
            is { ExecutionAvailable: true, VerifiedSimulation: true } &&
        definition.Steps.All(step =>
            AutomationContractRegistry.FindAction(step.Action.Key, step.Action.Version)
                is { ExecutionAvailable: true, VerifiedSimulation: true });

    private static AutomationShapeResult Reject(string code) => new(false, code, null);

    private static bool Integer(JsonElement value, out int result)
    {
        result = 0;
        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out result);
    }

    private static bool Reference(JsonElement value, out AutomationContractReference? result)
    {
        result = null;
        if (!Fields(value, ["key", "version"]) ||
            value.GetProperty("key").ValueKind != JsonValueKind.String ||
            !Integer(value.GetProperty("version"), out var version) || version <= 0)
            return false;
        var key = value.GetProperty("key").GetString();
        if (string.IsNullOrEmpty(key) || key.Length > 200) return false;
        result = new(key, version);
        return true;
    }

    private static bool TaskInputs(JsonElement value, out AutomationTaskCreateInputs? result)
    {
        result = null;
        if (!Fields(value, ["projectId", "title", "startAt", "endAt", "priority"], ["description"]) ||
            value.GetProperty("projectId").ValueKind != JsonValueKind.String ||
            !Guid.TryParse(value.GetProperty("projectId").GetString(), out var projectId) ||
            projectId == Guid.Empty || !Text(value.GetProperty("title"), MaximumUtf8Bytes, out var title) ||
            string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200 ||
            !Instant(value.GetProperty("startAt"), out var start) ||
            !Instant(value.GetProperty("endAt"), out var end) || end <= start ||
            value.GetProperty("priority").ValueKind != JsonValueKind.String)
            return false;
        var priority = value.GetProperty("priority").GetString();
        if (priority is not ("P0" or "P1" or "P2" or "P3")) return false;
        string? description = null;
        if (value.TryGetProperty("description", out var supplied) && supplied.ValueKind != JsonValueKind.Null &&
            !Text(supplied, 4000, out description)) return false;
        result = new(projectId, title!.Trim(), description, start, end, priority);
        return true;
    }

    private static bool Instant(JsonElement value, out DateTimeOffset result)
    {
        result = default;
        return value.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParseExact(value.GetString(), "O", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out result);
    }

    private static bool Text(JsonElement value, int maximum, out string? result)
    {
        result = null;
        if (value.ValueKind != JsonValueKind.String) return false;
        var text = value.GetString()!;
        if (text.Length > maximum) return false;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsControl(text[i])) return false;
            if (char.IsHighSurrogate(text[i]))
            {
                if (++i >= text.Length || !char.IsLowSurrogate(text[i])) return false;
            }
            else if (char.IsLowSurrogate(text[i])) return false;
        }
        result = text;
        return true;
    }

    private static bool HasUnpairedSurrogates(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]))
            {
                if (++i >= text.Length || !char.IsLowSurrogate(text[i])) return true;
            }
            else if (char.IsLowSurrogate(text[i])) return true;
        }
        return false;
    }

    private static bool Fields(JsonElement value, string[] required, string[]? optional = null)
    {
        if (value.ValueKind != JsonValueKind.Object) return false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            if (!seen.Add(property.Name) ||
                (!required.Contains(property.Name, StringComparer.Ordinal) &&
                 !(optional?.Contains(property.Name, StringComparer.Ordinal) ?? false)))
                return false;
        }
        return required.All(seen.Contains);
    }
}
