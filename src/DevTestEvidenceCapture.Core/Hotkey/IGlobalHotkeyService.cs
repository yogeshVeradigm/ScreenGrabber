namespace DevTestEvidenceCapture.Core.Hotkey;

public interface IGlobalHotkeyService
{
    event EventHandler? HotkeyPressed;

    /// <summary>Sets the window that will receive WM_HOTKEY messages. Call before <see cref="Register"/>.</summary>
    void AttachWindow(IntPtr windowHandle);

    /// <summary>
    /// Registers <paramref name="shortcutDescription"/> (e.g. <c>Ctrl+Shift+S</c>) against the attached
    /// window, replacing any currently registered shortcut.
    /// </summary>
    void Register(string shortcutDescription);

    void Unregister();

    /// <summary>
    /// Feeds a raw window message into this service. The App project's WPF message hook
    /// (HwndSource.AddHook, which Core cannot reference) calls this for every message; it
    /// returns true if the message was this service's WM_HOTKEY and has been handled.
    /// </summary>
    bool ProcessWindowMessage(int message, IntPtr wParam);
}
