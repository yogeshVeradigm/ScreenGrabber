using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.Core.Storage;

public interface IEvidenceStorage
{
    CaptureSession CreateSessionFolder(string rootFolder, string ticketNumber, string scenarioName);

    Task<CaptureSession?> TryFindInterruptedSessionAsync(AppConfiguration config);

    int GetNextSequenceNumber(string sessionFolderPath);
}
