namespace DevTestEvidenceCapture.Core.Naming;

public sealed class RenameResult
{
    public required bool Succeeded { get; init; }
    public IReadOnlyList<int> ConflictingSequenceNumbers { get; init; } = Array.Empty<int>();
    public IReadOnlyList<int> SuccessfullyRenamedSequenceNumbers { get; init; } = Array.Empty<int>();
    public Exception? Error { get; init; }

    public static RenameResult Success() => new() { Succeeded = true };

    public static RenameResult Conflict(IEnumerable<int> conflictingSequenceNumbers) => new()
    {
        Succeeded = false,
        ConflictingSequenceNumbers = conflictingSequenceNumbers.ToList(),
    };

    public static RenameResult PartialFailure(IEnumerable<int> successfullyRenamedSequenceNumbers, Exception error) => new()
    {
        Succeeded = false,
        SuccessfullyRenamedSequenceNumbers = successfullyRenamedSequenceNumbers.ToList(),
        Error = error,
    };
}
