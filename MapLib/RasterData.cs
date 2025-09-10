using MapLib.GdalSupport;
using MapLib.Geometry;
using OSGeo.GDAL;

namespace MapLib;

public abstract class RasterData : GeoData
{
    public override Bounds Bounds { get; }

    public int WidthPx { get; }
    public int HeightPx { get; }

    public RasterData(Srs srs, Bounds bounds, int widthPx, int heightPx) : base(srs)
    {
        Bounds = bounds;
        WidthPx = widthPx;
        HeightPx = heightPx;
    }

    public double[] GetGeoTransform()
        => GdalUtils.GetGeoTransform(Bounds, WidthPx, HeightPx);

    public abstract Dataset ToInMemoryGdalDataset();
}
