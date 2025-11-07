using MapLib.GdalSupport;
using MapLib.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace MapLib.DataSources.Raster;

/// <summary>
/// SRTM (Shuttle Radar Topography Mission) global elevation data source.
/// </summary>
/// <remarks>
/// This downloads SRTM3 (3 arcsec; ~90m) global data.
/// Appears to be SRTM version 2.1 (~2005; fewer artifacts but still some voids).
/// 
/// NOTE: Data from the mirror at kurviger.de (seems to be the most
/// reliable source atm)
/// 
/// Data format:
///   Elevation is in meters
///   16-bit signed integer
///   Nodata value: -32768
/// 
/// TODO: Support SRTM1 for USA (available from same mirror)
/// TODO: Support SRTM1 global (depending on mirrors)
/// TODO: Support a later and void-filled version like v3 (depending on mirrors)
/// </remarks>
public class SrtmDataSource : BaseRasterDataSource
{
    public override string Name => "SRTM";

    public override Srs Srs => Srs.Wgs84;

    public override Bounds? Bounds =>
        new(xmin: -180, xmax: 180, ymin: -54, ymax: 60);

    public override bool IsBounded => false;

    private string Subdirectory => "SRTM3";

    public double ScaleFactor { get; set; }

    public SrtmDataSource(double scaleFactor = 1)
    {
        ScaleFactor = scaleFactor;
    }


    public override Task<RasterData> GetData()
    {
        throw new InvalidOperationException(
            "SRTM: Must specify bounds.");
    }

    public override Task<RasterData> GetData(Srs destSrs)
    {
        throw new NotImplementedException(
            "SRTM: Must specify bounds.");
    }

    public override async Task<RasterData> GetData(Bounds boundsWgs84)
    {
        List<string> localFiles = new();
        foreach (string tileBasename in GetSrtm3BaseNames(boundsWgs84))
        {
            foreach (string url in GetPossibleUrlsForSrtm3Base(tileBasename))
            {
                try
                {
                    string filePath = await DownloadAndCache(url, Subdirectory);
                    Console.WriteLine("Including file: " + filePath);
                    localFiles.Add(filePath);
                    break;
                }
                catch (ApplicationException ex)
                {
                    // Download failed. This is expected, since the dataset
                    // only has coverage over land, and we try several URLs
                    // for each tile (see below).
                }

                // Not found among all possible urls for this tile. Mark as not found.
                MarkUrlNotFound(url, Subdirectory);
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

    // Files are organized by area. Also, some end in
    // ".hgt.zip" and some in just "hgt.zip". Without a full index,
    // we have to try all combinations until we find a match (if any)
    private static string[] Areas =
        ["Africa", "Australia", "Eurasia", "Islands", "North_America", "South_America"];
    private static string[] Extensions = [".hgt.zip", "hgt.zip"];
    private IEnumerable<string> GetPossibleUrlsForSrtm3Base(string baseName)
    {
        // Link format (kurviger SRTM mirror):
        // https://srtm.kurviger.de/SRTM3/Eurasia/N00E073.hgt.zip
        foreach (string area in Areas)
            foreach (string extension in Extensions)
                yield return $"https://srtm.kurviger.de/SRTM3/{area}/{baseName}{extension}";
    }        

    private IEnumerable<string> GetSrtm3BaseNames(Bounds b)
    {
        // Name refers to bottom left coordinates. Format: "N04E072".
        for (int x = b.XDegMin; x < b.XDegMax; x++)
            for (int y = b.YDegMin; y < b.YDegMax; y++)
                yield return (y < 0 ? "S" : "N") +
                    Math.Abs(y).ToString("D2") +
                    (x < 0 ? "W" : "E") +
                    Math.Abs(x).ToString("D3");
    }
}
