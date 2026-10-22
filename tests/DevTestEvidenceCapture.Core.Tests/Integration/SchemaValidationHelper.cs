using System.Text.Json;

namespace DevTestEvidenceCapture.Core.Tests.Integration;

/// <summary>
/// Validates a scenario.json file's shape against contracts/scenario-metadata.schema.json.
/// Hand-rolled instead of a JSON Schema library: the schema is small, fixed, and internal to
/// this project, so a full schema engine would be an unjustified dependency (Principle I).
/// </summary>
public static class SchemaValidationHelper
{
    public static void AssertValidScenarioMetadata(string scenarioJsonPath)
    {
        using var stream = File.OpenRead(scenarioJsonPath);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        RequireString(root, "ticketNumber");
        RequireString(root, "scenarioName");
        RequireDateTime(root, "sessionStart");
        RequireNullableDateTime(root, "sessionEnd");
        var screenshotCount = RequireInt(root, "screenshotCount");

        if (!root.TryGetProperty("screenshots", out var screenshots) || screenshots.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("'screenshots' must be an array.");
        }

        if (screenshots.GetArrayLength() != screenshotCount)
        {
            throw new InvalidDataException("'screenshotCount' must equal screenshots.length.");
        }

        foreach (var screenshot in screenshots.EnumerateArray())
        {
            RequireInt(screenshot, "sequenceNumber");
            RequireString(screenshot, "fileName");
            RequireDateTime(screenshot, "capturedAt");
        }
    }

    private static void RequireString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"'{propertyName}' must be a string.");
        }
    }

    private static void RequireDateTime(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String || !value.TryGetDateTimeOffset(out _))
        {
            throw new InvalidDataException($"'{propertyName}' must be an ISO 8601 date-time string.");
        }
    }

    private static void RequireNullableDateTime(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            throw new InvalidDataException($"'{propertyName}' must be present (string or null).");
        }

        if (value.ValueKind is JsonValueKind.Null)
        {
            return;
        }

        if (value.ValueKind != JsonValueKind.String || !value.TryGetDateTimeOffset(out _))
        {
            throw new InvalidDataException($"'{propertyName}' must be an ISO 8601 date-time string or null.");
        }
    }

    private static int RequireInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result))
        {
            throw new InvalidDataException($"'{propertyName}' must be an integer.");
        }

        return result;
    }
}
