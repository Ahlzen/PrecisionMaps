using MapLib.FileFormats.Raster;
using MapLib.GdalSupport;
using MapLib.Geometry;
using System.Drawing;

namespace MapLib.DataSources.Raster;

public class GdalDataSource : BaseRasterDataSource
{
    public override string Name => "Raster file (using GDAL)";
    public string Filename { get; }

    public override string Srs { get; }
    public override Bounds? Bounds { get; }
    
    public int WidthPx { get; }
    public int HeightPx { get; }
    
    public GdalDataSource(string filename)
    {
        Filename = filename;
        using OSGeo.GDAL.Dataset dataset = GdalUtils.GetRasterDataset(Filename);
        
        Srs = GdalUtils.GetSrsAsWkt(dataset);
        Bounds = GdalUtils.GetBounds(dataset);
        WidthPx = dataset.RasterXSize;
        HeightPx = dataset.RasterYSize;
    }

    //public RasterData GetData(Bounds boundsWgs84)
    //{
    //    using OSGeo.GDAL.Dataset dataset = GdalUtils.GetRasterDataset(Filename);
    //    Transformer transformer = new(Transformer.WktWgs84, Srs);
    //    Bounds srcBounds = transformer.Transform(boundsWgs84);
    //    Bitmap bitmap = GdalUtils.GetBitmap(dataset, srcBounds);
    //    return new RasterData(Srs, srcBounds, bitmap);
    //}

    public override RasterData GetData()
    {
        using OSGeo.GDAL.Dataset dataset = GdalUtils.GetRasterDataset(Filename);
        int width = dataset.RasterXSize;
        int height = dataset.RasterYSize;
        Bitmap bitmap = GdalUtils.GetBitmap(dataset, 0, 0, width, height, width, height);
        return new RasterData(Srs, Bounds!.Value, bitmap);
    }
}
