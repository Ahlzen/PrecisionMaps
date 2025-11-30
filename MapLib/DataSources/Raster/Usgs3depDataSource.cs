using MapLib.GdalSupport;
using MapLib.Geometry;
using System.Threading.Tasks;

namespace MapLib.DataSources.Raster;

/// <summary>
/// USGS 3D Elevation Program (3DEP) and
/// National Elevation Dataset (NED).
/// High-resolution elevation data for the USA.
/// </summary>
public class Usgs3depDataSource : BaseTiledRasterDataSource
{
    public override string Name => "USGS 3D Elevation Program (3DEP)";

    public override Srs Srs => Srs.Nad83;

    // Approximate bounds (Lower 48 + AK + HI)
    // TODO: Find actual bounds of 3DEP
    public override Bounds Bounds =>
        new(xmin: -179.8, xmax: -65.7, ymin: 18.3, ymax: 71.9);
    
    public override bool IsBounded => false;

    protected override string CacheSubdirectory => "USGS_3DEP";

    public Usgs3depDataSource(double scaleFactor = 1)
        : base(scaleFactor)
    {
    }

    protected override IEnumerable<string> GetUrlsForFile(string baseFileName) =>
        [$"https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/13/TIFF/current/{baseFileName}/USGS_13_{baseFileName}.tif"];

    protected override IEnumerable<string> GetBaseFileNames(Bounds b)
    {
        // Filename coordinates are for the top left (xmin, ymax).
        // Longitude is inverted since it's denoted W.
        for (int x = b.XDegMin; x < b.XDegMax; x++)
            for (int y = b.YDegMin; y < b.YDegMax; y++)
                yield return $"n{y + 1:D2}w{-x:D3}";
    }
}
