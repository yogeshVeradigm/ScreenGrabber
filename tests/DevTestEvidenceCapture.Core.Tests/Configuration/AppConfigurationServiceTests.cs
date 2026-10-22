using DevTestEvidenceCapture.Core.Configuration;

namespace DevTestEvidenceCapture.Core.Tests.Configuration;

public class AppConfigurationServiceTests : IDisposable
{
    private readonly string _configFilePath = Path.Combine(Path.GetTempPath(), $"dtec-config-{Guid.NewGuid():N}.json");

    [Fact]
    public async Task LoadAsync_WhenFileMissing_ReturnsDefaults()
    {
        var sut = new AppConfigurationService(_configFilePath);

        var config = await sut.LoadAsync();

        Assert.Null(config.RootFolderPath);
        Assert.Equal("Ctrl+Shift+S", config.CaptureShortcut);
        Assert.Null(config.LastActiveSessionPath);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsValues()
    {
        var sut = new AppConfigurationService(_configFilePath);
        var original = new AppConfiguration
        {
            RootFolderPath = @"C:\Evidence",
            CaptureShortcut = "Ctrl+Alt+F12",
            LastActiveSessionPath = @"C:\Evidence\TICKET-1\Scenario",
        };

        await sut.SaveAsync(original);
        var loaded = await sut.LoadAsync();

        Assert.Equal(original.RootFolderPath, loaded.RootFolderPath);
        Assert.Equal(original.CaptureShortcut, loaded.CaptureShortcut);
        Assert.Equal(original.LastActiveSessionPath, loaded.LastActiveSessionPath);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsAnnotationSettings()
    {
        var sut = new AppConfigurationService(_configFilePath);
        var original = new AppConfiguration
        {
            Annotation = new AnnotationSettings { HighlighterSize = 32, ShapeStrokeWidth = 6, ShapeFilled = true, ShapeColor = "#0078D4" },
        };

        await sut.SaveAsync(original);
        var loaded = await sut.LoadAsync();

        Assert.Equal(32, loaded.Annotation.HighlighterSize);
        Assert.Equal(6, loaded.Annotation.ShapeStrokeWidth);
        Assert.True(loaded.Annotation.ShapeFilled);
        Assert.Equal("#0078D4", loaded.Annotation.ShapeColor);
    }

    [Fact]
    public async Task LoadAsync_ConfigWrittenBeforeAnnotationSettingsExisted_UsesAnnotationDefaults()
    {
        await File.WriteAllTextAsync(_configFilePath, """{ "RootFolderPath": "C:\\Evidence", "CaptureShortcut": "Ctrl+Shift+S" }""");
        var sut = new AppConfigurationService(_configFilePath);

        var config = await sut.LoadAsync();

        Assert.Equal(20, config.Annotation.HighlighterSize);
        Assert.Equal(3, config.Annotation.ShapeStrokeWidth);
        Assert.False(config.Annotation.ShapeFilled);
        Assert.Equal("#FF0000", config.Annotation.ShapeColor);
    }

    public void Dispose()
    {
        if (File.Exists(_configFilePath))
        {
            File.Delete(_configFilePath);
        }
    }
}
