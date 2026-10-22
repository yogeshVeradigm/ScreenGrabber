using System.Text;
using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.Core.Naming;

public sealed class NamingService : INamingService
{
    private static readonly char[] InvalidChars = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public string SanitizeForFileSystem(string rawName)
    {
        var builder = new StringBuilder(rawName.Length);
        foreach (var c in rawName)
        {
            builder.Append(char.IsControl(c) || InvalidChars.Contains(c) ? '_' : c);
        }

        var sanitized = builder.ToString().TrimEnd('.', ' ');
        if (sanitized.Length == 0)
        {
            sanitized = "_";
        }

        if (ReservedDeviceNames.Contains(sanitized))
        {
            sanitized += "_";
        }

        return sanitized;
    }

    public string FormatScreenshotFileName(int sequenceNumber) => $"{sequenceNumber:D2}.png";

    public RenameResult ApplyRenames(CaptureSession session, IReadOnlyDictionary<int, string?> sequenceNumberToMeaningfulName)
    {
        // The numeric prefix is always preserved and unique per screenshot (FR-007), so two
        // screenshots' *final filenames* can never actually collide with each other. The
        // meaningful conflict to guard against (FR-017) is two screenshots being given the same
        // descriptive name, which would be confusing even though the files themselves wouldn't
        // clash — so conflicts are detected on the sanitized meaningful name, not the final path.
        var sanitizedMeaningfulNames = new Dictionary<int, string>();
        foreach (var screenshot in session.Screenshots)
        {
            if (sequenceNumberToMeaningfulName.TryGetValue(screenshot.SequenceNumber, out var meaningfulName)
                && !string.IsNullOrWhiteSpace(meaningfulName))
            {
                sanitizedMeaningfulNames[screenshot.SequenceNumber] = SanitizeForFileSystem(meaningfulName.Trim());
            }
        }

        var conflicts = sanitizedMeaningfulNames
            .GroupBy(kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Select(kvp => kvp.Key))
            .Distinct()
            .ToList();

        if (conflicts.Count > 0)
        {
            return RenameResult.Conflict(conflicts);
        }

        var proposedFinalNames = new Dictionary<int, string>();
        foreach (var screenshot in session.Screenshots)
        {
            if (!sanitizedMeaningfulNames.TryGetValue(screenshot.SequenceNumber, out var sanitizedMeaningfulName))
            {
                proposedFinalNames[screenshot.SequenceNumber] = screenshot.OriginalFileName;
                continue;
            }

            var prefix = Path.GetFileNameWithoutExtension(screenshot.OriginalFileName);
            var extension = Path.GetExtension(screenshot.OriginalFileName);
            proposedFinalNames[screenshot.SequenceNumber] = $"{prefix}_{sanitizedMeaningfulName}{extension}";
        }

        var renamedSoFar = new List<int>();
        foreach (var screenshot in session.Screenshots)
        {
            var finalFileName = proposedFinalNames[screenshot.SequenceNumber];
            if (finalFileName == screenshot.FinalFileName)
            {
                continue;
            }

            var currentPath = Path.Combine(session.FolderPath, screenshot.FinalFileName);
            var newPath = Path.Combine(session.FolderPath, finalFileName);
            try
            {
                File.Move(currentPath, newPath);
            }
            catch (Exception ex)
            {
                return RenameResult.PartialFailure(renamedSoFar, ex);
            }

            screenshot.FinalFileName = finalFileName;
            renamedSoFar.Add(screenshot.SequenceNumber);
        }

        return RenameResult.Success();
    }
}
