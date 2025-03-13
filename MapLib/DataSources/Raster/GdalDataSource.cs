using MapLib.FileFormats.Raster;
using MapLib.GdalSupport;
using MapLib.Geometry;
using System.Drawing;

namespace MapLib.DataSources.Raster;

public class GdalDataSource : IRasterDataSource
{
    public string Name => "Raster file (using GDAL)";
    public string Srs { get; }
    public Bounds? BoundsWgs84 { get; }
    public string Filename { get; }

    //private GdalDataReader _reader;

    public GdalDataSource(string filename)
    {
        Filename = filename;
        using OSGeo.GDAL.Dataset dataset = GdalUtils.GetRasterDataset(Filename);
        Srs = GdalUtils.GetSrsAsWkt(dataset);
    }

    public RasterData GetData(Bounds boundsWgs84)
    {
        using OSGeo.GDAL.Dataset dataset = GdalUtils.GetRasterDataset(Filename);
        Transformer transformer = new(Transformer.WktWgs84, Srs);
        Bounds srcBounds = transformer.Transform(boundsWgs84);
        Bitmap bitmap = GdalUtils.GetBitmap(dataset, srcBounds);
        return new RasterData(Srs, srcBounds, bitmap);
    }
}
