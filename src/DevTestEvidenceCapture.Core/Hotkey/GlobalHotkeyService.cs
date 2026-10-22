using System.Runtime.InteropServices;

namespace DevTestEvidenceCapture.Core.Hotkey;

public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 1;

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private IntPtr _windowHandle;
    private bool _isRegistered;

    public event EventHandler? HotkeyPressed;

    public void AttachWindow(IntPtr windowHandle) => _windowHandle = windowHandle;

    public void Register(string shortcutDescription)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("No window has been associated with the hotkey yet.");
        }

        var (modifiers, virtualKey) = ParseShortcut(shortcutDescription);
        Unregister();

        if (!RegisterHotKey(_windowHandle, HOTKEY_ID, modifiers, virtualKey))
        {
            throw new InvalidOperationException($"Failed to register global hotkey \"{shortcutDescription}\".");
        }

        _isRegistered = true;
    }

    public static bool IsValidShortcut(string shortcutDescription)
    {
        try
        {
            ParseShortcut(shortcutDescription);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public void Unregister()
    {
        if (_isRegistered)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            _isRegistered = false;
        }
    }

    public bool ProcessWindowMessage(int message, IntPtr wParam)
    {
        if (message != WM_HOTKEY || wParam.ToInt32() != HOTKEY_ID)
        {
            return false;
        }

        HotkeyPressed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private static (uint Modifiers, uint VirtualKey) ParseShortcut(string shortcutDescription)
    {
        var parts = shortcutDescription.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            throw new ArgumentException($"\"{shortcutDescription}\" is not a valid shortcut (expected e.g. \"Ctrl+Shift+S\").", nameof(shortcutDescription));
        }

        uint modifiers = 0;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            modifiers |= parts[i].ToLowerInvariant() switch
            {
                "ctrl" or "control" => MOD_CONTROL,
                "alt" => MOD_ALT,
                "shift" => MOD_SHIFT,
                "win" or "windows" => MOD_WIN,
                _ => throw new ArgumentException($"Unknown modifier \"{parts[i]}\" in shortcut \"{shortcutDescription}\".", nameof(shortcutDescription)),
            };
        }

        var virtualKey = ParseVirtualKey(parts[^1]);
        return (modifiers, virtualKey);
    }

    private static uint ParseVirtualKey(string keyName) => keyName.ToLowerInvariant() switch
    {
        "printscreen" or "prtsc" => 0x2C,
        "f1" => 0x70,
        "f2" => 0x71,
        "f3" => 0x72,
        "f4" => 0x73,
        "f5" => 0x74,
        "f6" => 0x75,
        "f7" => 0x76,
        "f8" => 0x77,
        "f9" => 0x78,
        "f10" => 0x79,
        "f11" => 0x7A,
        "f12" => 0x7B,
        _ when keyName.Length == 1 && char.IsLetterOrDigit(keyName[0]) => char.ToUpperInvariant(keyName[0]),
        _ => throw new ArgumentException($"Unknown key \"{keyName}\".", nameof(keyName)),
    };
}
