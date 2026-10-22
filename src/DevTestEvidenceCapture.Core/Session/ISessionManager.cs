using DevTestEvidenceCapture.Core.Naming;

namespace DevTestEvidenceCapture.Core.Session;

public interface ISessionManager
{
    CaptureSession? Current { get; }

    event EventHandler<CaptureFailedEventArgs>? CaptureFailed;

    Task StartAsync(string ticketNumber, string scenarioName);

    /// <summary>
    /// Finds a session left interrupted by an unexpected exit. It is not capturable until
    /// <see cref="AcceptResume"/> is called.
    /// </summary>
    Task<CaptureSession?> TryResumeInterruptedSessionAsync();

    void AcceptResume();

    /// <summary>Discards the offered interrupted session and stops offering it on later launches.</summary>
    Task DeclineResumeAsync();

    Task<Screenshot?> CaptureAsync();

    Task StopAsync();

    Task<RenameResult> RenameAsync(IReadOnlyDictionary<int, string?> sequenceNumberToMeaningfulName);

    Task SkipRenameAsync();
}
