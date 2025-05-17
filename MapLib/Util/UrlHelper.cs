using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

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
    public static async Task DownloadUrl(string url, string destFilename)
    {
        try
        {
            Console.WriteLine("Downloading: " + url);

            using HttpClient httpClient = new();
            using var stream = httpClient.GetStreamAsync(url);
            using var fs = new FileStream(destFilename, FileMode.OpenOrCreate);
            await stream.Result.CopyToAsync(fs);
        }
        catch (Exception ex)
        {
            // If download failed, we might have created a file.
            // In that case it should be deleted:
            if (File.Exists(destFilename))
                FileSystemHelpers.TryDelete(destFilename);

            throw new ApplicationException(
                $"Failed to download \"{url}\": {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Returns the filename from an URL. 
    /// </summary>
    /// <remarks>
    /// For example:
    /// https://prd-tnm.s3.amazonaws.com/index.html?prefix=StagedProducts/
    /// ->
    /// index.html
    /// </remarks>
    public static string GetFilenameFromUrl(string url)
    {
        Uri uri = new Uri(url);
        string[] segments = uri.Segments;
        if (segments.Any())
            return segments[^1];
        else
            return url;
    }
}
