using DevTestEvidenceCapture.Core.Configuration;

namespace DevTestEvidenceCapture.Core.Tests.Fakes;

public sealed class FakeAppConfigurationService : IAppConfigurationService
{
    public AppConfiguration Config { get; set; } = new();

    public Task<AppConfiguration> LoadAsync() => Task.FromResult(Config);

    public Task SaveAsync(AppConfiguration config)
    {
        Config = config;
        return Task.CompletedTask;
    }
}
