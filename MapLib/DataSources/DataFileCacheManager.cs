using System.IO;

namespace MapLib.DataSources;

internal class DataFileCacheManager
{
    public TimeSpan CacheDuration { get; set; }

    public DataFileCacheManager(TimeSpan defaultCacheDuration)
    {
        CacheDuration = defaultCacheDuration;
    }

    /// <summary>
    /// Returns a cached file path that is still valid, or null if none.
    /// </summary>
    public string? GetExistingCachedFile(string baseFileName, string extension)
    {
        string? mostRecentValidFilename = null;
        DateTime? mostRecentDatetime = null;

        foreach (string filename in Directory
            .GetFiles(Path.GetTempPath(), baseFileName + "_*" + extension))
        {
            string datePart = filename
                .Substring(baseFileName.Length + 1, 19) // e.g. "2025-05-16T15_04_55"
                .Replace('_', ':');
            if (!DateTime.TryParse(datePart, out DateTime fileDateTime))
                continue; // not a valid timestamp
            if ((DateTime.Now - fileDateTime) > CacheDuration)
                continue; // expired

            if (mostRecentDatetime == null || fileDateTime > mostRecentDatetime)
            {
                // This file is newer; take note!
                mostRecentValidFilename = filename;
                mostRecentDatetime = fileDateTime;
            }
        }
        return mostRecentValidFilename;
    }

    public string FormatFileName(string baseFileName, string extension)
    {
        string isodate = DateTime.Now
            .ToString("s", System.Globalization.CultureInfo.InvariantCulture)
            .Replace(":", "_"); // windows doesn't allow ":" in filenames
        return Path.Join(Path.GetTempPath(), baseFileName + "_" + isodate + extension);

    }
}
