namespace DevTestEvidenceCapture.Core.Configuration;

public interface IAppConfigurationService
{
    Task<AppConfiguration> LoadAsync();

    Task SaveAsync(AppConfiguration config);
}
