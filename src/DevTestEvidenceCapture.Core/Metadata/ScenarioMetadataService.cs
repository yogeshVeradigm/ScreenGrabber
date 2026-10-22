using System.Text.Json;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;

namespace DevTestEvidenceCapture.Core.Metadata;

public sealed class ScenarioMetadataService : IScenarioMetadataService
{
    private const string MetadataFileName = "scenario.json";
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public async Task WriteAsync(CaptureSession session)
    {
        var metadata = new ScenarioMetadata
        {
            TicketNumber = session.TicketNumber,
            ScenarioName = session.ScenarioName,
            SessionStart = session.StartTimestamp,
            SessionEnd = session.EndTimestamp,
            ScreenshotCount = session.Screenshots.Count,
            Screenshots = session.Screenshots
                .Select(s => new ScenarioScreenshotMetadata
                {
                    SequenceNumber = s.SequenceNumber,
                    FileName = s.FinalFileName,
                    CapturedAt = s.CaptureTimestamp,
                    Note = s.Note,
                })
                .ToList(),
        };

        var path = Path.Combine(session.FolderPath, MetadataFileName);
        await AtomicFile.WriteAllBytesAsync(path, JsonSerializer.SerializeToUtf8Bytes(metadata, SerializerOptions));
    }

    public async Task<ScenarioMetadata> ReadAsync(string sessionFolderPath)
    {
        var path = Path.Combine(sessionFolderPath, MetadataFileName);
        using var stream = File.OpenRead(path);

        ScenarioMetadata? metadata;
        try
        {
            metadata = await JsonSerializer.DeserializeAsync<ScenarioMetadata>(stream);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"'{path}' is not valid scenario metadata.", ex);
        }

        return metadata ?? throw new InvalidDataException($"'{path}' did not contain valid scenario metadata.");
    }
}
