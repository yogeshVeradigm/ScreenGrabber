using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.Core.Naming;

public interface INamingService
{
    string SanitizeForFileSystem(string rawName);

    string FormatScreenshotFileName(int sequenceNumber);

    RenameResult ApplyRenames(CaptureSession session, IReadOnlyDictionary<int, string?> sequenceNumberToMeaningfulName);
}
