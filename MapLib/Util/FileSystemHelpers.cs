using System.IO;

namespace MapLib.Util;

public static class FileSystemHelpers
{
    public static string RootTempPath =>
        Path.Combine(Path.GetTempPath(), "MapLib");

    public static string OutputFolder => "Output";
    public static string OutputTempPath =>
        Path.Combine(RootTempPath, OutputFolder);

    public static string DataCacheFolder => "Cache";
    public static string DataCachePath =>
        Path.Combine(RootTempPath, DataCacheFolder);

    public static string TestDataFolder => "TestData";
    public static string TestDataPath =>
        Path.Combine(RootTempPath, TestDataFolder);


    public static string GetTempOutputFileName(
        string extension, string? prefix = null)
    {
        Directory.CreateDirectory(OutputTempPath);
        string tempFileName;
        do
        {
            // Generates temp filename from (end of) GUID instead of
            // Path.GetTempFilename, since the latter always creates
            // an empty file with a '.tmp' file extension.
            string guid = Guid.NewGuid().ToString();
            guid = guid.Substring(guid.Length - 6);
            string filename = (prefix == null ? "" : prefix + "_") + guid + extension;
            tempFileName = Path.Combine(OutputTempPath, filename);
        }
        while (Path.Exists(tempFileName));
        return tempFileName;
    }

    /// <summary>
    /// Adds the given suffix to the filename/path, before any file extension.
    /// </summary>
    /// <remarks>
    /// For example:
    /// Path: c:\path\to\file.txt, suffix: _suffix
    /// c:\path\to\file.txt -> c:\path\to\file_suffix.txt
    /// </remarks>
    public static string AppendToFilename(string path, string? suffix)
        => Path.Combine(
            Path.GetDirectoryName(path) ?? "",
            Path.GetFileNameWithoutExtension(path) + suffix ?? "" +
            Path.GetExtension(path));

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