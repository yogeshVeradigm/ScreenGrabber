using DevTestEvidenceCapture.App.Views;
using DevTestEvidenceCapture.Core.Capture;
using DevTestEvidenceCapture.Core.Configuration;

namespace DevTestEvidenceCapture.App.Capture;

public sealed class OverlayRegionSelector : IRegionSelector
{
    private readonly IAppConfigurationService _configService;

    public OverlayRegionSelector(IAppConfigurationService configService)
    {
        _configService = configService;
    }

    public async Task<CaptureSelection?> SelectRegionAsync(byte[] screenPng)
    {
        var config = await _configService.LoadAsync();

        var completion = new TaskCompletionSource<CaptureSelection?>();
        var window = new RegionSelectionWindow(screenPng, config.Annotation);
        window.Closed += (_, _) => completion.TrySetResult(window.Result);
        window.Show();
        var result = await completion.Task;

        await RememberSettingsAsync(window.Settings);
        return result;
    }

    // Reloads before saving so nothing else in config.json is overwritten with stale values.
    private async Task RememberSettingsAsync(AnnotationSettings settings)
    {
        try
        {
            var config = await _configService.LoadAsync();
            config.Annotation = settings;
            await _configService.SaveAsync(config);
        }
        catch (Exception)
        {
            // Failing to remember tool settings must never cost the user their screenshot.
        }
    }
}
