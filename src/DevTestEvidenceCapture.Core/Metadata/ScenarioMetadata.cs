using System.Text.Json.Serialization;

namespace DevTestEvidenceCapture.Core.Metadata;

public sealed class ScenarioMetadata
{
    [JsonPropertyName("ticketNumber")]
    public required string TicketNumber { get; init; }

    [JsonPropertyName("scenarioName")]
    public required string ScenarioName { get; init; }

    [JsonPropertyName("sessionStart")]
    public required DateTimeOffset SessionStart { get; init; }

    [JsonPropertyName("sessionEnd")]
    public DateTimeOffset? SessionEnd { get; init; }

    [JsonPropertyName("screenshotCount")]
    public required int ScreenshotCount { get; init; }

    [JsonPropertyName("screenshots")]
    public required IReadOnlyList<ScenarioScreenshotMetadata> Screenshots { get; init; }
}

public sealed class ScenarioScreenshotMetadata
{
    [JsonPropertyName("sequenceNumber")]
    public required int SequenceNumber { get; init; }

    [JsonPropertyName("fileName")]
    public required string FileName { get; init; }

    [JsonPropertyName("capturedAt")]
    public required DateTimeOffset CapturedAt { get; init; }

    [JsonPropertyName("note")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Note { get; init; }
}
