using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Util;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace MapLib.DataSources;

public abstract class BaseDataSource<TData> : IHasSrs
{
    public abstract string Name { get; }
    
    public abstract Srs Srs { get; }

    /// <summary>
    /// Bounds of this data set. In source (dataset) SRS. If known/applicable.
    /// </summary>
    /// <remarks>
    /// For a file, this is the extent of the data in the file.
    /// For an online data source, this returns the full applicable
    /// bounds for that source.
    /// </remarks>
    public abstract Bounds? Bounds { get; }

    /// <summary>
    /// Bounds of this data set, in lon/lat WGS84. If known/applicable.
    /// </summary>
    public Bounds? BoundsWgs84 => Bounds?.ToWgs84(Srs);


    /// <summary>
    /// Returns true if this source is strictly bounded (beyond the
    /// entire earth or data set),
    /// e.g. vector data in a file. False otherwise, such as an online
    /// data source like OSM, SRTM or USGS 3DEP.
    /// </summary>
    public abstract bool IsBounded { get; }


    /// <summary>
    /// Return all data from the specified source.
    /// Supported for bounded sources only.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if called on an unbounded source.
    /// </exception>
    public virtual Task<TData> GetData() => GetData(null);

    /// <summary>
    /// See GetData(). Returns data in the specified SRS,
    /// reprojecting/warping if needed, or in the original
    /// SRS if destSrs is null.
    /// </summary>
    public abstract Task<TData> GetData(Srs? destSrs);

    /// <summary>
    /// Return data for (at least) the specified bounds from
    /// this source.
    /// </summary>
    /// <remarks>
    /// May return less if specified bounds are beyond those of
    /// this data source.
    /// May return more if needed (e.g. may not crop/trim a file data source).
    /// </remarks>
    public virtual Task<TData> GetData(Bounds boundsWgs84) => GetData(boundsWgs84, null);

    /// <summary>
    /// See GetData(). Returns data in the specified SRS,
    /// reprojecting/warping as needed, or in the original
    /// SRS if destSrs is null.
    /// </summary>
    public abstract Task<TData> GetData(Bounds boundsWgs84, Srs? destSrs);


    #region Data file caching

    /// <summary>
    /// Download a file to the data cache (if needed) and returns path
    /// to the target file. Unpacks archives and moves files if needed.
    /// </summary>
    /// <param name="url">
    /// URL to download.
    /// E.g. https://path.to.cdn/rasters/relief_50.zip
    /// </param>
    /// <param name="subdirectory">
    /// Optional. Destination directory under the data cache. Created if necessary.
    /// </param>
    /// <param name="targetFileName">
    /// Optional. The file of interest, whose path is returned.
    /// May be different from the file name in the URL, e.g. for archives.
    /// E.g. "relief_50.tif" for the above URL.
    /// If null, this is derived directly from the URL.
    /// </param>
    /// <returns>Full path to the target file in the data cache.</returns>
    /// <exception cref="ApplicationException">
    /// Thrown if download failed or URL was previously marked as not found.
    /// Message contains details.
    /// </exception>
    protected virtual async Task<string> DownloadAndCache(
        string url, string? subdirectory, string? targetFileName = null)
    {
        GetTargetFilePath(url, subdirectory, targetFileName,
            out string destPath, out string destDirectory, out string targetPath);

        if (File.Exists(targetPath))
        {
            // Target exists. We're done!
            return targetPath;
        }

        // Check if the URL has previously been marked as not found
        // (e.g. a tile that is not part of a data set)
        if (UrlMarkedNotFound(targetPath))
        {
            throw new ApplicationException($"Target file marked as not found: {targetPath}");
        }

        // Download the file (this may throw ApplicationException)
        await UrlHelper.DownloadUrl(url, destPath);

        // If archive, unpack it
        if (destPath.EndsWith(".zip"))
        {
            ZipFile.ExtractToDirectory(destPath, destDirectory, true);

            // NOTE: Some unzip into a subdirectory of the same name,
            // in which case we move the files up one level and remove
            // the subdirectory.

            string datasetSubdirectory = destPath.TrimEnd(".zip");
            if (Directory.Exists(datasetSubdirectory))
            {
                MoveFilesUpOneLevel(datasetSubdirectory);
                Directory.Delete(datasetSubdirectory);
            }
        }

        if (!File.Exists(targetPath))
            throw new ApplicationException($"Target file not found: {targetPath}");
        return targetPath;
    }

    protected virtual bool UrlMarkedNotFound(string targetPath)
        => File.Exists(targetPath + ".notfound");
    protected virtual bool UrlMarkedNotFound(string url, string? subdirectory, string? targetFileName = null)
    {
        GetTargetFilePath(url, subdirectory, targetFileName,
            out string destPath, out string destDirectory, out string targetPath);
        return UrlMarkedNotFound(targetPath);
    }

    protected virtual void MarkUrlNotFound(string url, string? subdirectory, string? targetFileName = null)
    {
        GetTargetFilePath(url, subdirectory, targetFileName,
            out string destPath, out string destDirectory, out string targetPath);

        // Create an empty file with .notfound extension to mark that the URL was not found
        targetPath += ".notfound";
        if (!File.Exists(targetPath))
            File.Create(targetPath).Dispose();
    }

    private void GetTargetFilePath(string url, string? subdirectory, string? targetFileName,
        out string destPath, out string destDirectory, out string targetPath)
    {
        // Create destination directory
        destDirectory = subdirectory == null ?
            FileSystemHelpers.SourceCachePath :
            Path.Combine(FileSystemHelpers.SourceCachePath, subdirectory);
        try
        {
            Directory.CreateDirectory(destDirectory);
        }
        catch (Exception ex)
        {
            throw new ApplicationException(
                "Failed to create destination directory \"\": "
                + ex.Message, ex);
        }

        string urlFilename = UrlHelper.GetFilenameFromUrl(url);
        destPath = Path.Combine(destDirectory, urlFilename);
        targetFileName ??= urlFilename;
        targetPath = Path.Combine(destDirectory, targetFileName);
    }

    private static void MoveFilesUpOneLevel(string subdirectoryPath)
    {
        string parentDir = Directory.GetParent(subdirectoryPath)!.FullName;
        foreach (var sourcefile in Directory.GetFiles(subdirectoryPath))
            File.Move(sourcefile,
                Path.Combine(parentDir, Path.GetFileName(sourcefile)),
                true);
    }

    #endregion
}


public abstract class BaseVectorDataSource : BaseDataSource<VectorData>
{
    public async Task<VectorData> ReprojectIfNeeded(VectorData data, Srs? destSrs)
    {
        if (destSrs == null || destSrs == Srs)
            return data;
        else
            return await Reproject(data, destSrs);
    }

    public async Task<VectorData> Reproject(VectorData data, Srs destSrs)
    {
        Transformer transformer = new(this.Srs, destSrs);
        return await Task.FromResult(data.Transform(transformer));
    }
}


public abstract class BaseRasterDataSource : BaseDataSource<RasterData>
{
}