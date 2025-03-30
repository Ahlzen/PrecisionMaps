using MapLib.Geometry;
using System.Drawing;

namespace MapLib;

public class RasterData : GeoData
{
    
    /// <param name="srs">SRS of raster data.</param>
    /// <param name="bounds">Bounds (in source/dataset SRS)</param>
    /// <param name="bitmap">Bitmap containing the raster data/layer.</param>
    public RasterData(string srs, Bounds bounds, Bitmap bitmap)
        : base(srs)
    {
        // TODO: implement properly
        Bounds = bounds;
        Bitmap = bitmap;
    }
    public override Bounds Bounds { get; }
    public Bitmap Bitmap { get; }
}
