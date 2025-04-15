using System.Data;
using System.Drawing.Imaging;
using System.Drawing;
using System.Text;
using OSGeo.GDAL;
using OSGeo.OSR;
using MapLib.Geometry;

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

    public static Coord PixelToGeo(Dataset ds, Coord pixelCoord)
    {
        double[] adfGeoTransform = new double[6];
        ds.GetGeoTransform(adfGeoTransform);
        return new Coord(
            adfGeoTransform[0] + adfGeoTransform[1] * pixelCoord.X + adfGeoTransform[2] * pixelCoord.Y,
            adfGeoTransform[3] + adfGeoTransform[4] * pixelCoord.X + adfGeoTransform[5] * pixelCoord.Y
        );
    }

    public static Coord GeoToPixel(Dataset ds, Coord geoCoord)
    {
        double[] adfGeoTransform = new double[6];
        ds.GetGeoTransform(adfGeoTransform);
        double[] invTransform = new double[6];
        Gdal.InvGeoTransform(adfGeoTransform, invTransform);
        return new Coord(
            invTransform[0] + invTransform[1] * geoCoord.X + invTransform[2] * geoCoord.Y,
            invTransform[3] + invTransform[4] * geoCoord.X + invTransform[5] * geoCoord.Y
        );
    }

    public static Dataset GetRasterDataset(string filename)
        => Gdal.Open(filename, Access.GA_ReadOnly);

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
        sb.AppendLine("  Upper Left (" + GDALInfoGetPositionSummary(ds, 0.0, 0.0) + ")");
        sb.AppendLine("  Lower Left (" + GDALInfoGetPositionSummary(ds, 0.0, ds.RasterYSize) + ")");
        sb.AppendLine("  Upper Right (" + GDALInfoGetPositionSummary(ds, ds.RasterXSize, 0.0) + ")");
        sb.AppendLine("  Lower Right (" + GDALInfoGetPositionSummary(ds, ds.RasterXSize, ds.RasterYSize) + ")");
        sb.AppendLine("  Center (" + GDALInfoGetPositionSummary(ds, ds.RasterXSize / 2, ds.RasterYSize / 2) + ")");
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
                srs.ExportToXML(out string xml, "");
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
        using Dataset dataSet = GetRasterDataset(filename);
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

    public static Dataset Reproject(Dataset sourceDataset, string destSrs)
    {
        string sourceSrs = GdalUtils.GetSrsAsWkt(sourceDataset);
        using Dataset destDataset = Gdal.AutoCreateWarpedVRT(sourceDataset,
            sourceSrs, destSrs, ResampleAlg.GRA_Lanczos,
            maxerror: 0 // use exact calculations
            );
        return destDataset;
    }


    /// <summary>
    /// Returns the transformed coordinates of the specified raster position
    /// (pixel) in the raster.
    /// </summary>
    public static Coord GDALInfoGetPosition(Dataset ds, double x, double y)
    {
        double[] adfGeoTransform = new double[6];
        double dfGeoX, dfGeoY;
        ds.GetGeoTransform(adfGeoTransform);
        dfGeoX = adfGeoTransform[0] + adfGeoTransform[1] * x + adfGeoTransform[2] * y;
        dfGeoY = adfGeoTransform[3] + adfGeoTransform[4] * x + adfGeoTransform[5] * y;
        return new Coord(dfGeoX, dfGeoY);
    }

    public static string GDALInfoGetPositionSummary(Dataset ds, double x, double y)
    {
        Coord coord = GDALInfoGetPosition(ds, x, y);
        return coord.X.ToString() + ", " + coord.Y.ToString();
    }

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
        Coord c1 = GDALInfoGetPosition(ds, 0, 0);
        Coord c2 = GDALInfoGetPosition(ds, width-1, height-1);
        return Bounds.FromCoords([c1, c2]);
    }

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
}