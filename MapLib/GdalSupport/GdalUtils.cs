using System.Data;
using System.Drawing.Imaging;
using System.Drawing;
using System.Text;
using OSGeo.GDAL;
using OSGeo.OSR;
using MapLib.Geometry;
using MapLib.Util;

namespace MapLib.GdalSupport;

/// <remarks>
/// Parts based on examples from:
/// https://trac.osgeo.org/gdal/browser/trunk/gdal/swig/csharp/apps/
/// </remarks>
public static class GdalUtils
{
    static GdalUtils()
    {
        Initialize();
    }

    public static void Initialize()
    {
        GdalConfiguration.ConfigureGdal();
        GdalConfiguration.ConfigureOgr();
    }

    /// <summary>
    /// Opens and returns the dataset at the specified file name.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown on failure.
    /// </exception>
    public static Dataset OpenDataset(string filename)
    {
        Dataset dataset = Gdal.Open(filename, Access.GA_ReadOnly);
        if (dataset == null)
            throw new ApplicationException("Failed to open " + filename);
        return dataset;
    }

    #region Raster <-> Pixel coordinate transformation

    /// <summary>
    /// Returns the specified pixel coordinates transformed to
    /// geometric coordinates in the raster's SRS.
    /// </summary>
    public static Coord PixelToGeo(Dataset ds, Coord pixelCoord)
    {
        double[] affineGeoTransform = new double[6];
        ds.GetGeoTransform(affineGeoTransform);
        return PixelToGeo(affineGeoTransform, pixelCoord);
    }
    public static Coord PixelToGeo(Dataset ds, double x, double y)
        => PixelToGeo(ds, new Coord(x, y));
    public static Coord PixelToGeo(double[] affineGeoTransform, Coord pixelCoord)
        => new Coord(
            affineGeoTransform[0] + affineGeoTransform[1] * pixelCoord.X + affineGeoTransform[2] * pixelCoord.Y,
            affineGeoTransform[3] + affineGeoTransform[4] * pixelCoord.X + affineGeoTransform[5] * pixelCoord.Y);

    /// <summary>
    /// Returns the specified geometric coordinates in the raster's SRS
    /// transformed to pixel coordinates.
    /// </summary>
    public static Coord GeoToPixel(Dataset ds, Coord geoCoord)
    {
        double[] affineGeoTransform = new double[6];
        double[] inverseTransform = new double[6];
        ds.GetGeoTransform(affineGeoTransform);
        Gdal.InvGeoTransform(affineGeoTransform, inverseTransform);
        return GeoToPixel(inverseTransform, geoCoord);
    }
    public static Coord GeoToPixel(double[] inverseTransform, Coord geoCoord)
        => new Coord(
            inverseTransform[0] + inverseTransform[1] * geoCoord.X + inverseTransform[2] * geoCoord.Y,
            inverseTransform[3] + inverseTransform[4] * geoCoord.X + inverseTransform[5] * geoCoord.Y);

    /// <summary>
    /// Returns the bounds, in the source (dataset) SRS.
    /// </summary>
    /// <remarks>
    /// Bounds are calculated by including all four corners.
    /// TODO: Should we calculate differently and/or sample more
    /// positions along the edges?
    /// </remarks>
    public static Bounds GetBounds(Dataset ds)
    {
        int width = ds.RasterXSize;
        int height = ds.RasterYSize;
        Coord c1 = PixelToGeo(ds, 0, 0);
        Coord c2 = PixelToGeo(ds, width - 1, height - 1);
        return Bounds.FromCoords([c1, c2]);
    }

    #endregion

    #region Info and reporting

    public static string GetRasterInfo(string filename)
    {
        using Dataset ds = Gdal.Open(filename, Access.GA_ReadOnly);
        if (ds == null) throw new ApplicationException("Failed to open " + filename);
        return GetRasterInfo(ds);
    }
    public static string GetRasterInfo(Dataset ds)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Raster dataset parameters:");
        sb.AppendLine("  Projection: " + ds.GetProjectionRef());
        sb.AppendLine("  RasterCount: " + ds.RasterCount);
        sb.AppendLine("  RasterSize (" + ds.RasterXSize + "," + ds.RasterYSize + ")");

        // Get metadata
        string[] metadata = ds.GetMetadata("");
        if (metadata.Length > 0)
        {
            sb.AppendLine("  Metadata:");
            for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
            {
                sb.AppendLine("    " + iMeta + ":  " + metadata[iMeta]);
            }
            sb.AppendLine("");
        }

        // Report "IMAGE_STRUCTURE" metadata.
        metadata = ds.GetMetadata("IMAGE_STRUCTURE");
        if (metadata.Length > 0)
        {
            sb.AppendLine("  Image Structure Metadata:");
            for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
            {
                sb.AppendLine("    " + iMeta + ":  " + metadata[iMeta]);
            }
            sb.AppendLine("");
        }

        // Report geolocation.
        metadata = ds.GetMetadata("GEOLOCATION");
        if (metadata.Length > 0)
        {
            sb.AppendLine("  Geolocation:");
            for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
            {
                sb.AppendLine("    " + iMeta + ":  " + metadata[iMeta]);
            }
            sb.AppendLine("");
        }

        // Report corners
        sb.AppendLine("Corner Coordinates:");
        sb.AppendLine("  Upper Left (" + PixelToGeo(ds, 0.0, 0.0) + ")");
        sb.AppendLine("  Lower Left (" + PixelToGeo(ds, 0.0, ds.RasterYSize) + ")");
        sb.AppendLine("  Upper Right (" + PixelToGeo(ds, ds.RasterXSize, 0.0) + ")");
        sb.AppendLine("  Lower Right (" + PixelToGeo(ds, ds.RasterXSize, ds.RasterYSize) + ")");
        sb.AppendLine("  Center (" + PixelToGeo(ds, ds.RasterXSize / 2, ds.RasterYSize / 2) + ")");
        sb.AppendLine("");

        // Report projection.
        string projection = ds.GetProjectionRef();
        if (projection != null)
        {
            SpatialReference srs = new SpatialReference(null);
            if (srs.ImportFromWkt(ref projection) == 0)
            {
                string wkt;
                srs.ExportToPrettyWkt(out wkt, 0);
                double units = srs.GetAngularUnits();
                string unitsName = srs.GetAngularUnitsName();
                string linearUnitsName = srs.GetLinearUnitsName();
                sb.AppendLine("Coordinate System is:");
                sb.AppendLine(wkt);
            }
            else
            {
                sb.AppendLine("Coordinate System is:");
                sb.AppendLine(projection);
            }
        }
        return sb.ToString();
    }

    public static string GetRasterBandSummary(Dataset dataset)
    {
        StringBuilder sb = new();
        sb.AppendLine(string.Join(", ", dataset.GetFileList()));
        sb.AppendLine($"Size: {dataset.RasterXSize}x{dataset.RasterYSize} px");
        sb.AppendLine("Raster count: " + dataset.RasterCount);
        for (int i = 1; i <= dataset.RasterCount; i++) // NOTE: these are 1-indexed
        {
            Band band = dataset.GetRasterBand(i);
            sb.AppendLine("Band " + i);
            sb.AppendLine(" DataType: " + band.DataType);
            sb.AppendLine(" Size: " + Gdal.GetDataTypeSize(band.DataType));
            sb.AppendLine(" Interpretation: " + band.GetRasterColorInterpretation());

            band.GetNoDataValue(out double noDataValue, out int hasNoDataValue);
            sb.AppendLine(" Has no-data value: " + hasNoDataValue);
            //if (hasNoDataValue == 1)
            sb.AppendLine(" No-data value: " + noDataValue);
        }
        return sb.ToString();
    }

    #endregion
    
    #region Spatial Reference Systems

    public static SpatialReference GetSpatialReference(Dataset rasterDataSet)
    {
        // TODO: add error handling
        string wkt = rasterDataSet.GetProjectionRef();
        SpatialReference srs = new SpatialReference(null);
        srs.ImportFromWkt(ref wkt);
        return srs;
    }

    public static string GetSrsAsWkt(string filename)
    {
        using Dataset dataSet = OpenDataset(filename);
        return GetSrsAsWkt(dataSet);
    }
    public static string GetSrsAsWkt(Dataset rasterDataset)
    {
        string projection = rasterDataset.GetProjectionRef();
        if (projection == null)
            throw new ApplicationException("Could not determine projection from GDAL Dataset.");

        SpatialReference srs = new SpatialReference(null);
        if (srs.ImportFromWkt(ref projection) == 0)
        {
            string wkt;
            srs.ExportToPrettyWkt(out wkt, 0);
            return wkt;
        }
        else
        {
            return projection;
        }
    }

    #endregion

    #region Reprojection / Warping

    /// <summary>
    /// Reprojects (warps) the source file, saves and returns
    /// the resulting file path.
    /// </summary>
    public static string Warp(string sourceFilename, string destSrs)
    {
        // Get source SRS
        using Dataset sourceDataset = GdalUtils.OpenDataset(sourceFilename);
        string sourceSrs = GdalUtils.GetSrsAsWkt(sourceDataset);

        // Warp
        GDALWarpAppOptions appOptions = new GDALWarpAppOptions(new string[] {
                "-s_srs", sourceSrs,
                "-t_srs", destSrs,
                "-r", "lanczos",
                "-of", "gtiff",
                "-srcnodata", "-9999",
                "-dstnodata", "-9999",
            });
        string destFilename = FileSystemHelpers.GetTempOutputFileName(".tif", "warped");
        using Dataset result = Gdal.Warp(destFilename,
            new Dataset[] { sourceDataset },
            appOptions,
            callback: null,
            callback_data: null);

        return destFilename;
    }

    /// <summary>
    /// Reprojects (warps) the source dataset and returns the result.
    /// </summary>
    /// <remarks>
    /// Does not support NODATA (transparency) in result. Use other overloads
    /// for that.
    /// </remarks>
    public static Dataset Warp(Dataset sourceDataset, string destSrs)
    {
        string sourceSrs = GdalUtils.GetSrsAsWkt(sourceDataset);
        using Dataset destDataset = Gdal.AutoCreateWarpedVRT(sourceDataset,
            sourceSrs, destSrs, ResampleAlg.GRA_Lanczos,
            maxerror: 0 // use exact calculations
            );
        return destDataset;
    }

    #endregion

    #region Bitmap builders (TODO: make obsolete)

    public static Bitmap GetBitmap(string filename, Bounds areaProjected,
        int? imageWidth = null, int? imageHeight = null)
    {
        using Dataset ds = Gdal.Open(filename, Access.GA_ReadOnly);
        if (ds == null) throw new ApplicationException("Failed to open " + filename);
        return GetBitmap(ds, areaProjected, imageWidth, imageHeight);
    }

    public static Bitmap GetBitmap(Dataset ds, Bounds areaProjected,
        int? imageWidth = null, int? imageHeight = null)
    {
        Coord bottomLeft = GeoToPixel(ds, areaProjected.BottomLeft);
        Coord topRight = GeoToPixel(ds, areaProjected.TopRight);

        int xOffset = (int)Math.Min(bottomLeft.X, topRight.X);
        int yOffset = (int)Math.Min(bottomLeft.Y, topRight.Y);
        int width = (int)Math.Abs(topRight.X - bottomLeft.X);
        int height = (int)Math.Abs(topRight.Y - bottomLeft.Y);

        // Use native resolution from raster data if not specified
        return GetBitmap(ds, xOffset, yOffset, width, height,
            imageWidth ?? width, imageHeight ?? height);
    }

    public static Bitmap GetBitmap(string filename, int xOffset, int yOffset,
        int width, int height, int imageWidth, int imageHeight)
    {
        using (Dataset ds = Gdal.Open(filename, Access.GA_ReadOnly))
        {
            if (ds == null) throw new ApplicationException("Failed to open " + filename);
            return GetBitmap(ds, xOffset, yOffset, width, height, imageWidth, imageHeight);
        }
    }
    
    public static Bitmap GetBitmap(Dataset ds, int xOffset, int yOffset,
        int width, int height, int imageWidth, int imageHeight)
    {
        if (ds.RasterCount == 0)
            throw new InvalidOperationException("No raster layers in Dataset");

        int[] bandMap = new int[4] { 1, 1, 1, 1 };
        int channelCount = 1;
        bool hasAlpha = false;
        bool isIndexed = false;
        int channelSize = 8;
        ColorTable? ct = null;
        // Evaluate the bands and find out a proper image transfer format
        for (int i = 0; i < ds.RasterCount; i++)
        {
            Band band = ds.GetRasterBand(i + 1);
            if (Gdal.GetDataTypeSize(band.DataType) > 8)
                channelSize = 16;
            switch (band.GetRasterColorInterpretation())
            {
                case ColorInterp.GCI_AlphaBand:
                    channelCount = 4;
                    hasAlpha = true;
                    bandMap[3] = i + 1;
                    break;
                case ColorInterp.GCI_BlueBand:
                    if (channelCount < 3)
                        channelCount = 3;
                    bandMap[0] = i + 1;
                    break;
                case ColorInterp.GCI_RedBand:
                    if (channelCount < 3)
                        channelCount = 3;
                    bandMap[2] = i + 1;
                    break;
                case ColorInterp.GCI_GreenBand:
                    if (channelCount < 3)
                        channelCount = 3;
                    bandMap[1] = i + 1;
                    break;
                case ColorInterp.GCI_PaletteIndex:
                    ct = band.GetRasterColorTable();
                    isIndexed = true;
                    bandMap[0] = i + 1;
                    break;
                case ColorInterp.GCI_GrayIndex:
                    isIndexed = true;
                    bandMap[0] = i + 1;
                    break;
                default:
                    // we create the bandmap using the dataset ordering by default
                    if (i < 4 && bandMap[i] == 0)
                    {
                        if (channelCount < i)
                            channelCount = i;
                        bandMap[i] = i + 1;
                    }
                    break;
            }
        }

        // find out the pixel format based on the gathered information
        PixelFormat pixelFormat;
        DataType dataType;
        int pixelSpace;

        if (isIndexed)
        {
            pixelFormat = PixelFormat.Format8bppIndexed;
            dataType = DataType.GDT_Byte;
            pixelSpace = 1;
        }
        else
        {
            if (channelCount == 1)
            {
                if (channelSize > 8)
                {
                    pixelFormat = PixelFormat.Format16bppGrayScale;
                    dataType = DataType.GDT_Int16;
                    pixelSpace = 2;
                }
                else
                {
                    pixelFormat = PixelFormat.Format24bppRgb;
                    channelCount = 3;
                    dataType = DataType.GDT_Byte;
                    pixelSpace = 3;
                }
            }
            else
            {
                if (hasAlpha)
                {
                    if (channelSize > 8)
                    {
                        pixelFormat = PixelFormat.Format64bppArgb;
                        dataType = DataType.GDT_UInt16;
                        pixelSpace = 8;
                    }
                    else
                    {
                        pixelFormat = PixelFormat.Format32bppArgb;
                        dataType = DataType.GDT_Byte;
                        pixelSpace = 4;
                    }
                    channelCount = 4;
                }
                else
                {
                    if (channelSize > 8)
                    {
                        pixelFormat = PixelFormat.Format48bppRgb;
                        dataType = DataType.GDT_UInt16;
                        pixelSpace = 6;
                    }
                    else
                    {
                        pixelFormat = PixelFormat.Format24bppRgb;
                        dataType = DataType.GDT_Byte;
                        pixelSpace = 3;
                    }
                    channelCount = 3;
                }
            }
        }

        // Create a Bitmap to store the GDAL image in
        Bitmap bitmap = new Bitmap(imageWidth, imageHeight, pixelFormat);

        if (isIndexed)
        {
            // setting up the color table
            if (ct != null)
            {
                int iCol = ct.GetCount();
                ColorPalette pal = bitmap.Palette;
                for (int i = 0; i < iCol; i++)
                {
                    ColorEntry ce = ct.GetColorEntry(i);
                    pal.Entries[i] = Color.FromArgb(ce.c4, ce.c1, ce.c2, ce.c3);
                }
                bitmap.Palette = pal;
            }
            else
            {
                // grayscale
                ColorPalette pal = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                    pal.Entries[i] = Color.FromArgb(255, i, i, i);
                bitmap.Palette = pal;
            }
        }

        // Use GDAL raster reading methods to read the image data directly into the Bitmap
        BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, imageWidth, imageHeight),
            ImageLockMode.ReadWrite, pixelFormat);

        try
        {
            int stride = bitmapData.Stride;
            nint buf = bitmapData.Scan0;

            ds.ReadRaster(xOffset, yOffset, width, height, buf, imageWidth, imageHeight, dataType,
                channelCount, bandMap, pixelSpace, stride, 1);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    #endregion
}