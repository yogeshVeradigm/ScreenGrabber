using System.Text.RegularExpressions;
using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;

namespace DevTestEvidenceCapture.Core.Storage;

public sealed class EvidenceStorage : IEvidenceStorage
{
    private static readonly Regex SequencePrefixPattern = new(@"^(\d+)(?:_.*)?\.png$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly INamingService _namingService;
    private readonly IScenarioMetadataService _metadataService;

    public EvidenceStorage(INamingService namingService, IScenarioMetadataService metadataService)
    {
        _namingService = namingService;
        _metadataService = metadataService;
    }

    public CaptureSession CreateSessionFolder(string rootFolder, string ticketNumber, string scenarioName)
    {
        var sanitizedTicket = _namingService.SanitizeForFileSystem(ticketNumber);
        var sanitizedScenario = _namingService.SanitizeForFileSystem(scenarioName);
        var ticketFolder = Path.Combine(rootFolder, sanitizedTicket);

        var finalScenarioFolderName = sanitizedScenario;
        var folderPath = Path.Combine(ticketFolder, finalScenarioFolderName);
        var suffix = 2;
        while (Directory.Exists(folderPath))
        {
            finalScenarioFolderName = $"{sanitizedScenario} ({suffix})";
            folderPath = Path.Combine(ticketFolder, finalScenarioFolderName);
            suffix++;
        }

        Directory.CreateDirectory(folderPath);

        return new CaptureSession
        {
            TicketNumber = ticketNumber,
            ScenarioName = scenarioName,
            SanitizedTicketFolderName = sanitizedTicket,
            SanitizedScenarioFolderName = finalScenarioFolderName,
            FolderPath = folderPath,
            StartTimestamp = DateTimeOffset.Now,
        };
    }

    public int GetNextSequenceNumber(string sessionFolderPath)
    {
        if (!Directory.Exists(sessionFolderPath))
        {
            return 1;
        }

        var max = 0;
        foreach (var filePath in Directory.EnumerateFiles(sessionFolderPath))
        {
            var match = SequencePrefixPattern.Match(Path.GetFileName(filePath));
            if (match.Success && int.TryParse(match.Groups[1].Value, out var sequenceNumber))
            {
                max = Math.Max(max, sequenceNumber);
            }
        }

        return max + 1;
    }

    public async Task<CaptureSession?> TryFindInterruptedSessionAsync(AppConfiguration config)
    {
        var folderPath = config.LastActiveSessionPath;
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            return null;
        }

        ScenarioMetadata metadata;
        try
        {
            metadata = await _metadataService.ReadAsync(folderPath);
        }
        catch (IOException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }

        if (metadata.SessionEnd is not null)
        {
            return null;
        }

        var session = new CaptureSession
        {
            TicketNumber = metadata.TicketNumber,
            ScenarioName = metadata.ScenarioName,
            SanitizedTicketFolderName = Path.GetFileName(Path.GetDirectoryName(folderPath) ?? string.Empty),
            SanitizedScenarioFolderName = Path.GetFileName(folderPath),
            FolderPath = folderPath,
            StartTimestamp = metadata.SessionStart,
            Status = CaptureSessionStatus.Interrupted,
        };

        foreach (var screenshot in metadata.Screenshots.OrderBy(s => s.SequenceNumber))
        {
            session.Screenshots.Add(new Screenshot
            {
                SequenceNumber = screenshot.SequenceNumber,
                OriginalFileName = _namingService.FormatScreenshotFileName(screenshot.SequenceNumber),
                FinalFileName = screenshot.FileName,
                CaptureTimestamp = screenshot.CapturedAt,
                Note = screenshot.Note,
            });
        }

        return session;
    }
}
