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
public class SrtmDataSource : BaseTiledRasterDataSource
{
    public override string Name => "Shuttle Radio Topography Mission (SRTM)";

    public override Srs Srs => Srs.Wgs84;

    public override Bounds? Bounds =>
        new(xmin: -180, xmax: 180, ymin: -54, ymax: 60);

    public override bool IsBounded => false;

    protected override string CacheSubdirectory => "SRTM3";

    public SrtmDataSource(double scaleFactor = 1)
        : base(scaleFactor)
    {
    }

    // Files are organized by area. Also, some end in
    // ".hgt.zip" and some in just "hgt.zip". Without a full index,
    // we have to try all combinations until we find a match (if any)
    private static string[] Areas =
        ["Africa", "Australia", "Eurasia", "Islands", "North_America", "South_America"];
    private static string[] Extensions = [".hgt.zip", "hgt.zip"];
    protected override IEnumerable<string> GetUrlsForFile(string baseName)
    {
        // Link format (kurviger SRTM mirror):
        // https://srtm.kurviger.de/SRTM3/Eurasia/N00E073.hgt.zip
        foreach (string area in Areas)
            foreach (string extension in Extensions)
                yield return $"https://srtm.kurviger.de/SRTM3/{area}/{baseName}{extension}";
    }

    protected override IEnumerable<string> GetBaseFileNames(Bounds b)
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
