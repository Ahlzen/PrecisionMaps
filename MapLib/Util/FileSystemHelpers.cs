using System.IO;

namespace MapLib.Util;

public static class FileSystemHelpers
{
    public static string GetTempFileName(
        string extension, string? prefix = null)
    {
        string tempFileName;
        do
        {
            // Generates temp filename from (end of) GUID instead of
            // Path.GetTempFilename, since the latter always creates
            // an empty file with a '.tmp' file extension.
            string guid = Guid.NewGuid().ToString();
            guid = guid.Substring(guid.Length - 6);
            string filename = (prefix ?? "") + guid + extension;
            tempFileName = Path.Combine(Path.GetTempPath(), filename);
        }
        while (Path.Exists(tempFileName));
        return tempFileName;
    }

    /// <summary>
    /// Attempts to delete a file. Returns true
    /// on success, false otherwise. Does not throw.
    /// </summary>
    public static bool TryDelete(string filename)
    {
        try {
            File.Delete(filename);
            return true;
        }
        catch {
            return false;
        }
        
    }
}