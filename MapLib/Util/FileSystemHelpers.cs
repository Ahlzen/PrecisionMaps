using System.IO;

namespace MapLib.Util;

public static class FileSystemHelpers
{
    public static string GetTempFilename(string extension)
    {
        string tempFilename = Path.GetTempFileName();
        tempFilename = tempFilename + extension;
        return tempFilename;
    }
}
