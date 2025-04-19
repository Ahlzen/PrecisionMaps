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
        //using OSGeo.GDAL.Dataset sourceDataset = GdalUtils.GetRasterDataset(Filename);

        string filename = Filename;
        if (Srs != destSrs)
        {
            //string sourceSrs = GdalUtils.GetSrsAsWkt(sourceDataset);
            //using Dataset destDataset = Gdal.AutoCreateWarpedVRT(sourceDataset,
            //    sourceSrs, destSrs, ResampleAlg.GRA_Lanczos,
            //    maxerror: 0 // use exact calculations
            //    );
            //return GetRasterData(destDataset);

            // Reproject source data, and use that file
            filename = Transform(filename, destSrs);
        }
        
        using Dataset sourceDataset =
            GdalUtils.GetRasterDataset(filename);

        Console.WriteLine(GdalUtils.GetRasterInfo(sourceDataset));

        return GetRasterData(sourceDataset);
    }

    //public string Transform(string )

    /// <summary>
    /// Reprojects (warps) the source file, saves and returns
    /// the resulting file path.
    /// </summary>
    public static string Transform(string sourceFilename, string destSrs)
    {
        // Get source SRS
        using Dataset sourceDataset = GdalUtils.GetRasterDataset(sourceFilename);
        string sourceSrs = GdalUtils.GetSrsAsWkt(sourceDataset);

        // Warp
        GDALWarpAppOptions appOptions = new GDALWarpAppOptions(new string[] {
                "-s_srs", sourceSrs,
                "-t_srs", destSrs,
                "-r", "lanczos",
                "-of", "gtiff"
            });
        string destFilename = FileSystemHelpers.GetTempFileName(".tif", "warped");
        using Dataset result = Gdal.Warp(destFilename,
            new Dataset[] { sourceDataset },
            appOptions,
            callback: null,
            callback_data: null);

        return destFilename;
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
