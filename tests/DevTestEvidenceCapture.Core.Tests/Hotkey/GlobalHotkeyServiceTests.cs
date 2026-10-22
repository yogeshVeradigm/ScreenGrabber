using DevTestEvidenceCapture.Core.Hotkey;

namespace DevTestEvidenceCapture.Core.Tests.Hotkey;

public class GlobalHotkeyServiceTests
{
    [Theory]
    [InlineData("Ctrl+Shift+S", true)]
    [InlineData("Ctrl+Alt+F12", true)]
    [InlineData("Win+Shift+3", true)]
    [InlineData("Ctrl+PrintScreen", true)]
    [InlineData("S", false)]
    [InlineData("Ctrl+Shift", false)]
    [InlineData("Hyper+S", false)]
    [InlineData("Ctrl+Space", false)]
    public void IsValidShortcut_AcceptsOnlySupportedCombinations(string shortcut, bool expected)
    {
        Assert.Equal(expected, GlobalHotkeyService.IsValidShortcut(shortcut));
    }

    [Fact]
    public void Register_BeforeWindowAttached_Throws()
    {
        var sut = new GlobalHotkeyService();

        Assert.Throws<InvalidOperationException>(() => sut.Register("Ctrl+Shift+S"));
    }
}
