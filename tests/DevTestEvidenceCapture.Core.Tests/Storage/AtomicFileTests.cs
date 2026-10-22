using DevTestEvidenceCapture.Core.Storage;

namespace DevTestEvidenceCapture.Core.Tests.Storage;

public class AtomicFileTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"dtec-atomic-{Guid.NewGuid():N}");

    public AtomicFileTests()
    {
        Directory.CreateDirectory(_folder);
    }

    [Fact]
    public async Task WriteAllBytesAsync_ReplacesExistingFileAndLeavesNoTempFile()
    {
        var path = Path.Combine(_folder, "scenario.json");
        await File.WriteAllTextAsync(path, "old");

        await AtomicFile.WriteAllBytesAsync(path, "new"u8.ToArray());

        Assert.Equal("new", await File.ReadAllTextAsync(path));
        Assert.Single(Directory.EnumerateFiles(_folder));
    }

    [Fact]
    public async Task WriteAllBytesAsync_WithoutOverwrite_KeepsExistingFileAndCleansUp()
    {
        var path = Path.Combine(_folder, "01.png");
        await File.WriteAllTextAsync(path, "existing evidence");

        await Assert.ThrowsAsync<IOException>(() => AtomicFile.WriteAllBytesAsync(path, "new"u8.ToArray(), overwrite: false));

        Assert.Equal("existing evidence", await File.ReadAllTextAsync(path));
        Assert.Single(Directory.EnumerateFiles(_folder));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }
}
