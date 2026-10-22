using System.Text.Json;
using DevTestEvidenceCapture.Core.Storage;

namespace DevTestEvidenceCapture.Core.Configuration;

public sealed class AppConfigurationService : IAppConfigurationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _configFilePath;

    public AppConfigurationService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevTestEvidenceCapture",
            "config.json"))
    {
    }

    public AppConfigurationService(string configFilePath)
    {
        _configFilePath = configFilePath;
    }

    public async Task<AppConfiguration> LoadAsync()
    {
        if (!File.Exists(_configFilePath))
        {
            return new AppConfiguration();
        }

        using var stream = File.OpenRead(_configFilePath);
        try
        {
            var config = await JsonSerializer.DeserializeAsync<AppConfiguration>(stream);
            return config ?? new AppConfiguration();
        }
        catch (JsonException)
        {
            // An unreadable config must never stop the app from starting; fall back to defaults and
            // let the next save replace it.
            return new AppConfiguration();
        }
    }

    public async Task SaveAsync(AppConfiguration config)
    {
        var directory = Path.GetDirectoryName(_configFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await AtomicFile.WriteAllBytesAsync(_configFilePath, JsonSerializer.SerializeToUtf8Bytes(config, SerializerOptions));
    }
}
