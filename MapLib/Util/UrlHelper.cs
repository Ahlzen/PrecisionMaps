using System.IO;
using System.Net.Http;

namespace MapLib.Util;

public static class UrlHelper
{
    /// <summary>
    /// Downloads the URL to the specified destination file.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown on error. InnerException and message contain
    /// more details.
    /// </exception>
    public static async void DownloadUrl(string url, string destFilename)
    {
        try
        {
            using HttpClient httpClient = new();
            using var stream = httpClient.GetStreamAsync(url);
            using var fs = new FileStream(destFilename, FileMode.OpenOrCreate);
            await stream.Result.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                $"Failed to download \"{url}\": {ex.Message}", ex);
        }
    }
}
