using MapLib.Geometry;
using System.Drawing;

namespace MapLib;

public class RasterData : GeoData
{
    // TODO: implement properly
    public RasterData(Bounds bounds, Bitmap bitmap)
    {
        Bounds = bounds;
        Bitmap = bitmap;
    }
    public override Bounds Bounds { get; }
    public Bitmap Bitmap { get; }
}
