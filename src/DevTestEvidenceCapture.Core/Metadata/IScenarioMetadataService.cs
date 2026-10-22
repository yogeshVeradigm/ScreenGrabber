using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.Core.Metadata;

public interface IScenarioMetadataService
{
    Task WriteAsync(CaptureSession session);

    Task<ScenarioMetadata> ReadAsync(string sessionFolderPath);
}
