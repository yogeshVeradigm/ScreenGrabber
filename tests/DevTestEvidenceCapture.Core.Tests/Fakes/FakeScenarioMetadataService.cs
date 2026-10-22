using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.Core.Tests.Fakes;

public sealed class FakeScenarioMetadataService : IScenarioMetadataService
{
    public Dictionary<string, ScenarioMetadata> MetadataByFolderPath { get; } = new();
    public CaptureSession? LastWrittenSession { get; private set; }

    public Task WriteAsync(CaptureSession session)
    {
        LastWrittenSession = session;
        MetadataByFolderPath[session.FolderPath] = new ScenarioMetadata
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
                })
                .ToList(),
        };
        return Task.CompletedTask;
    }

    public Task<ScenarioMetadata> ReadAsync(string sessionFolderPath)
    {
        if (!MetadataByFolderPath.TryGetValue(sessionFolderPath, out var metadata))
        {
            throw new FileNotFoundException($"No fake metadata configured for '{sessionFolderPath}'.");
        }

        return Task.FromResult(metadata);
    }
}
