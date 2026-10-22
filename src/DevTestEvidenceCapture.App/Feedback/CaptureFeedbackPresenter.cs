using System.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace DevTestEvidenceCapture.App.Feedback;

public sealed class CaptureFeedbackPresenter
{
    private static readonly TimeSpan FlashDuration = TimeSpan.FromMilliseconds(150);

    public void PlayConfirmationSound() => SystemSounds.Asterisk.Play();

    public void ShowFlash()
    {
        var flash = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
            ShowInTaskbar = false,
            Topmost = true,
            ResizeMode = ResizeMode.NoResize,
            Left = 0,
            Top = 0,
            Width = SystemParameters.PrimaryScreenWidth,
            Height = SystemParameters.PrimaryScreenHeight,
            IsHitTestVisible = false,
        };

        flash.Show();

        var timer = new DispatcherTimer { Interval = FlashDuration };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            flash.Close();
        };
        timer.Start();
    }
}
