using MapLib.FileFormats.Raster;
using MapLib.GdalSupport;
using MapLib.Geometry;
using OSGeo.GDAL;
using OSGeo.OSR;
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
        using OSGeo.GDAL.Dataset sourceDataset = GdalUtils.GetRasterDataset(Filename);

        // Reproject source data, if needed
        if (Srs != destSrs)
        {
            string sourceSrs = GdalUtils.GetSrsAsWkt(sourceDataset);
            using Dataset destDataset = Gdal.AutoCreateWarpedVRT(sourceDataset,
                sourceSrs, destSrs, ResampleAlg.GRA_Lanczos,
                maxerror: 0 // use exact calculations
                );
            return GetRasterData(destDataset);

            //Gdal.ReprojectImage(sourceDataset, destDataset,
            //    sourceSrs, destSrs, ResampleAlg.GRA_Lanczos,
            //    WarpMemoryLimit: 0.0, // use default memory limit)
            //    maxerror: 0.0, // use exact calculations
            //    callback: null,
            //    callback_data: null,
            //    options: new string[] { }
            //    );
            //srFrom.ImportFromWkt(dataset.GetProjectionRef());
        }
        else
        {
            return GetRasterData(sourceDataset);
        }
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
