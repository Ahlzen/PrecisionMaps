using MapLib.Geometry;
using System.Drawing;

namespace MapLib;

public class RasterData : GeoData
{
    // TODO: implement properly
    public RasterData(string srs, Bounds bounds, Bitmap bitmap)
        : base(srs)
    {
        Bounds = bounds;
        Bitmap = bitmap;
    }
    public override Bounds Bounds { get; }
    public Bitmap Bitmap { get; }
}
