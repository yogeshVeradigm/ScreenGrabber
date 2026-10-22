using DevTestEvidenceCapture.Core.Configuration;
using DevTestEvidenceCapture.Core.Metadata;
using DevTestEvidenceCapture.Core.Naming;
using DevTestEvidenceCapture.Core.Session;
using DevTestEvidenceCapture.Core.Storage;
using DevTestEvidenceCapture.Core.Tests.Fakes;

namespace DevTestEvidenceCapture.Core.Tests.Session;

public class RecentTicketNumbersTests : IDisposable
{
    private readonly string _rootFolder = Path.Combine(Path.GetTempPath(), $"dtec-tickets-{Guid.NewGuid():N}");
    private readonly FakeAppConfigurationService _configService;
    private readonly SessionManager _sut;

    public RecentTicketNumbersTests()
    {
        Directory.CreateDirectory(_rootFolder);
        var namingService = new NamingService();
        var metadataService = new ScenarioMetadataService();
        _configService = new FakeAppConfigurationService
        {
            Config = new AppConfiguration { RootFolderPath = _rootFolder, RecentTicketNumbers = { "CAMP-1" } },
        };
        _sut = new SessionManager(new EvidenceStorage(namingService, metadataService), new FakeScreenCaptureService(), metadataService, namingService, _configService);
    }

    [Fact]
    public void Add_MovesExistingTicketToFrontIgnoringCase()
    {
        var result = RecentTicketNumbers.Add(new[] { "CAMP-1", "CAMP-2", "CAMP-3" }, "camp-2");

        Assert.Equal(new[] { "camp-2", "CAMP-1", "CAMP-3" }, result);
    }

    [Fact]
    public void Add_KeepsAtMostTenTickets()
    {
        var existing = Enumerable.Range(1, 10).Select(i => $"CAMP-{i}");

        var result = RecentTicketNumbers.Add(existing, "CAMP-99");

        Assert.Equal(RecentTicketNumbers.Max, result.Count);
        Assert.Equal("CAMP-99", result[0]);
        Assert.DoesNotContain("CAMP-10", result);
    }

    [Fact]
    public async Task StartAsync_RecordsTicketAsMostRecent()
    {
        await _sut.StartAsync("CAMP-2", "Scenario");

        Assert.Equal(new[] { "CAMP-2", "CAMP-1" }, _configService.Config.RecentTicketNumbers);
    }

    // Ticket numbers aren't limited to JIRA keys: any identifier or a plain task name works.
    [Theory]
    [InlineData("AB#1234", "AB#1234")]
    [InlineData("INC0012345", "INC0012345")]
    [InlineData("Fix login bug: mobile", "Fix login bug_ mobile")]
    [InlineData("Release 26.1 smoke test", "Release 26.1 smoke test")]
    public async Task StartAsync_AcceptsAnyTicketOrTaskName(string ticketNumber, string expectedFolderName)
    {
        await _sut.StartAsync(ticketNumber, "Scenario");

        Assert.Equal(expectedFolderName, _sut.Current!.SanitizedTicketFolderName);
        Assert.True(Directory.Exists(Path.Combine(_rootFolder, expectedFolderName, "Scenario")));
        Assert.Equal(ticketNumber, _sut.Current.TicketNumber);
        Assert.Equal(ticketNumber, _configService.Config.RecentTicketNumbers[0]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootFolder))
        {
            Directory.Delete(_rootFolder, recursive: true);
        }
    }
}
