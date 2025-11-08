using MapLib.GdalSupport;
using MapLib.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.DataSources.Raster;

/// <summary>
/// Base class for online raster data sources distributed as tiles in
/// a grid, such as USGS 3DEP/NED, SRTM, etc.
/// </summary>
public abstract class BaseTiledRasterDataSource : BaseRasterDataSource
{
    public double ScaleFactor { get; set; }

    /// <summary>
    /// Subdirectory under which downloaded files are cached.
    /// </summary>
    protected abstract string CacheSubdirectory { get; }

    /// <param name="scaleFactor">
    /// If less than 1, the raster is scaled when read. Useful
    /// to avoid rasters that are too large or too slow to reproject.
    /// </param>
    public BaseTiledRasterDataSource(double scaleFactor = 1)
    {
        ScaleFactor = scaleFactor;
    }

    public override Task<RasterData> GetData()
    {
        throw new InvalidOperationException(
            "GetData(): Must specify bounds.");
    }

    public override Task<RasterData> GetData(Srs destSrs)
    {
        throw new NotImplementedException(
            "GetData(): Must specify bounds.");
    }

    public override async Task<RasterData> GetData(Bounds boundsWgs84)
    {
        List<string> localFiles = new();
        foreach (string baseFilename in GetBaseFileNames(boundsWgs84))
        {
            bool foundTile = false;
            string lastUrl = "";
            foreach (string url in GetUrlsForFile(baseFilename))
            {
                lastUrl = url;
                try
                {
                    string filePath = await DownloadAndCache(url, CacheSubdirectory);
                    Console.WriteLine("Including file: " + filePath);
                    localFiles.Add(filePath);
                    foundTile = true;
                    break;
                }
                catch (ApplicationException ex)
                {
                    // Download failed.
                    // This is expected, since the dataset
                    // * May only have coverage over land or a certain area, and
                    // * For some sources, we need to try several URLs for each tile.
                }
            }

            if (!foundTile)
            {
                // Not found among all possible urls for this tile.
                // Mark as not found.
                MarkUrlNotFound(lastUrl, CacheSubdirectory);
            }
        }

        // Read data using GDAL
        GdalDataSource gdalSource = new GdalDataSource(localFiles, ScaleFactor);
        RasterData data = await gdalSource.GetData(boundsWgs84);
        return data;
    }

    public override Task<RasterData> GetData(Bounds boundsWgs84, Srs destSrs)
    {
        throw new NotImplementedException();
    }


    /// <summary>
    /// Returns the file names of all tiles within the specified bounds.
    /// </summary>
    protected abstract IEnumerable<string> GetBaseFileNames(Bounds b);

    /// <summary>
    /// Returns one or more URLs for the specified base file name.
    /// </summary>
    /// <remarks>
    /// In most cases this returns a single tile URL.
    /// For some data sources we may need to try multiple URLs,
    /// in which case this returns multiple URLs to try in order.
    /// </remarks>
    protected abstract IEnumerable<string> GetUrlsForFile(string baseFileName);
}
