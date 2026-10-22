namespace DevTestEvidenceCapture.Core.Storage;

/// <summary>
/// Writes a file via a temporary file in the same folder followed by a rename, so a crash or power
/// loss mid-write leaves either the previous file or the complete new one, never a truncated one.
/// </summary>
public static class AtomicFile
{
    public static async Task WriteAllBytesAsync(string path, byte[] bytes, bool overwrite = true)
    {
        var tempPath = path + ".tmp";
        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await stream.WriteAsync(bytes);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, path, overwrite);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
