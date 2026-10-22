using DevTestEvidenceCapture.Core.Naming;

namespace DevTestEvidenceCapture.Core.Tests.Naming;

public class NamingServiceTests
{
    private readonly NamingService _sut = new();

    [Theory]
    [InlineData("Search: results?", "Search_ results_")]
    [InlineData("a<b>c:d\"e/f\\g|h?i*j", "a_b_c_d_e_f_g_h_i_j")]
    public void SanitizeForFileSystem_ReplacesInvalidCharacters(string input, string expected)
    {
        Assert.Equal(expected, _sut.SanitizeForFileSystem(input));
    }

    [Fact]
    public void SanitizeForFileSystem_StripsTrailingDotsAndSpaces()
    {
        Assert.Equal("Login page", _sut.SanitizeForFileSystem("Login page. . "));
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("PRN")]
    [InlineData("COM1")]
    [InlineData("LPT9")]
    [InlineData("NUL")]
    public void SanitizeForFileSystem_AppendsSuffixForReservedDeviceNames(string reservedName)
    {
        var result = _sut.SanitizeForFileSystem(reservedName);
        Assert.NotEqual(reservedName, result, StringComparer.OrdinalIgnoreCase);
        Assert.StartsWith(reservedName, result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1, "01.png")]
    [InlineData(9, "09.png")]
    [InlineData(42, "42.png")]
    [InlineData(100, "100.png")]
    public void FormatScreenshotFileName_ZeroPadsAndExpandsPastNinetyNine(int sequenceNumber, string expected)
    {
        Assert.Equal(expected, _sut.FormatScreenshotFileName(sequenceNumber));
    }
}
