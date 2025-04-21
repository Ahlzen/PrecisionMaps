using MapLib.FileFormats.Raster;
using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Util;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;

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

    public override RasterData GetData()
    {
        using OSGeo.GDAL.Dataset dataset = GdalUtils.GetRasterDataset(Filename);
        int width = dataset.RasterXSize;
        int height = dataset.RasterYSize;
        Bitmap bitmap = GdalUtils.GetBitmap(dataset, 0, 0, width, height, width, height);
        return new RasterData(Srs, Bounds!.Value, bitmap);
    }

    public override RasterData GetData(string destSrs)
    {
        string filename = Filename;
        if (Srs != destSrs)
        {
            // Reproject source data, and use that file
            filename = GdalUtils.Transform(filename, destSrs);
        }
        
        using Dataset sourceDataset =
            GdalUtils.GetRasterDataset(filename);
        Console.WriteLine(GdalUtils.GetRasterBandSummary(sourceDataset));
        return GetRasterData(sourceDataset);
    }

    private static RasterData GetRasterData(Dataset dataset)
    {
        int width = dataset.RasterXSize;
        int height = dataset.RasterYSize;
        Bitmap bitmap = GdalUtils.GetBitmap(dataset, 0, 0, width, height, width, height);
        string srs = GdalUtils.GetSrsAsWkt(dataset);
        Bounds bounds = GdalUtils.GetBounds(dataset);
        return new RasterData(srs, bounds, bitmap);
    }
}
