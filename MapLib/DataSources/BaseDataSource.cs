using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Util;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace MapLib.DataSources;

public abstract class BaseDataSource<TData>
{
    public abstract string Name { get; }

    public abstract string Srs { get; }

    /// <summary>
    /// Bounds of full data set. In source (dataset) SRS. If known/applicable.
    /// </summary>
    /// <remarks>
    /// For a file, this is the extent of the data in the file.
    /// For an online data source, this returns the full applicable
    /// bounds for that source.
    /// </remarks>
    public abstract Bounds? Bounds { get; }

    /// <summary>
    /// Bounds of full data set, in lon/lat WGS84. If known/applicable.
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
    public abstract Task<TData> GetData();

    /// <summary>
    /// See GetData(). Returns data in the specified SRS,
    /// reprojecting/warping if needed.
    /// </summary>
    public abstract Task<TData> GetData(string destSrs);

    /// <summary>
    /// Return data for (at least) the specified bounds from
    /// this source.
    /// </summary>
    /// <remarks>
    /// May return less if specified bounds are beyond those of
    /// this data source.
    /// May return more if needed (e.g. may not crop/trim a file data source).
    /// </remarks>
    public abstract Task<TData> GetData(Bounds boundsWgs84);

    /// <summary>
    /// See GetData(). Returns data in the specified SRS,
    /// reprojecting/warping if needed.
    /// </summary>
    public abstract Task<TData> GetData(Bounds boundsWgs84, string destSrs);


    #region Data file caching

    //public static readonly string DataCachePath =
    //;

    /// <summary>
    /// Downloads a file to the data cache, unless it already exists.
    /// </summary>
    /// <param name="url">Source URL</param>
    /// <param name="subdirectory">Optional. Sub-directory under data cache directory.</param>
    /// <param name="filename">Optional. If null, file name is derived from the URL.</param>
    /// <param name="unpackArchives">If archive (e.g. .zip) unpack it.</param>
    /// <returns>Path to downloaded or cached file.</returns>
    /// <exception cref="ApplicationException">
    /// Thrown if download failed. Message contains details.
    /// </exception>
    protected virtual async Task<string> DownloadAndCache(
        string url, string? subdirectory = null, string? filename = null,
        bool unpackArchives = true)
    {
        // TODO: Merge with DataFileCacheManager

        string destDirectory = subdirectory == null ?
            FileSystemHelpers.DataCachePath :
            Path.Combine(FileSystemHelpers.DataCachePath, subdirectory);

        if (!Directory.Exists(destDirectory))
        {
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
        }

        filename ??= UrlHelper.GetFilenameFromUrl(url);
        string destPath = Path.Combine(destDirectory, filename);

        if (File.Exists(destPath))
        {
            // File exists. We're done!
            return destPath;
        }

        await UrlHelper.DownloadUrl(url, destPath);

        // If archive, unpack it
        if (filename.EndsWith(".zip") && unpackArchives)
            ZipFile.ExtractToDirectory(destPath, destDirectory, true);

        return destPath;
    }

    #endregion
}


public abstract class BaseVectorDataSource : BaseDataSource<VectorData>
{
    public override async Task<VectorData> GetData(string destSrs)
        => Reproject(GetData().Result, destSrs);

    public override async Task<VectorData> GetData(Bounds boundsWgs84, string destSrs)
        => Reproject(GetData(boundsWgs84).Result, destSrs);

    public VectorData Reproject(VectorData data, string destSrs)
    {
        Transformer transformer = new(this.Srs, destSrs);
        return data.Transform(transformer);
    }
}

public abstract class BaseRasterDataSource : BaseDataSource<RasterData>
{
}