using System.Windows;
using System.Windows.Input;
using DevTestEvidenceCapture.Core.Hotkey;

namespace DevTestEvidenceCapture.App.Views;

/// <summary>Records a key combination to use as the global capture shortcut.</summary>
public partial class ShortcutWindow : Window
{
    private string? _candidate;

    public ShortcutWindow(string currentShortcut)
    {
        InitializeComponent();
        ShortcutText.Text = currentShortcut;
        HintText.Text = "Current shortcut shown above. Esc cancels.";
    }

    /// <summary>The chosen shortcut, e.g. <c>Ctrl+Shift+S</c>; <c>null</c> if cancelled.</summary>
    public string? Shortcut { get; private set; }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (Keyboard.Modifiers == ModifierKeys.None && key == Key.Escape)
        {
            Close();
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.None && key == Key.Enter && _candidate is not null)
        {
            Accept();
            return;
        }

        Record(key);
    }

    // Windows often delivers Print Screen only as a key-up.
    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Snapshot)
        {
            e.Handled = true;
            Record(Key.Snapshot);
        }
    }

    private void Record(Key key)
    {
        var modifiers = DescribeModifiers(Keyboard.Modifiers);
        var keyName = DescribeKey(key);

        if (keyName is null)
        {
            // Only modifiers so far (or an unsupported key): show progress, nothing to save yet.
            ShortcutText.Text = modifiers.Count > 0 ? string.Join("+", modifiers) + "+..." : ShortcutText.Text;
            return;
        }

        if (modifiers.Count == 0)
        {
            HintText.Text = "Add at least one of Ctrl, Alt, Shift or Win.";
            return;
        }

        var shortcut = string.Join("+", modifiers.Append(keyName));
        if (!GlobalHotkeyService.IsValidShortcut(shortcut))
        {
            HintText.Text = $"\"{shortcut}\" can't be used as a shortcut.";
            return;
        }

        _candidate = shortcut;
        ShortcutText.Text = shortcut;
        HintText.Text = "Press Enter or click Save to use this shortcut.";
        SaveButton.IsEnabled = true;
    }

    private static List<string> DescribeModifiers(ModifierKeys modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        return parts;
    }

    // Names understood by GlobalHotkeyService; null for keys it doesn't support.
    private static string? DescribeKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => key.ToString(),
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        >= Key.F1 and <= Key.F12 => key.ToString(),
        Key.Snapshot => "PrintScreen",
        _ => null,
    };

    private void Accept()
    {
        Shortcut = _candidate;
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) => Accept();

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
