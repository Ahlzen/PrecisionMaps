// Uncomment to generate more detailed raster data info
#define VERBOSE

using MapLib.Geometry;
using MapLib.Util;
using OSGeo.GDAL;
using OSGeo.OSR;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace MapLib.GdalSupport;

/// <remarks>
/// Parts based on examples from:
/// https://trac.osgeo.org/gdal/browser/trunk/gdal/swig/csharp/apps/
/// </remarks>
public static class GdalUtils
{
    private static bool _isInitialized = false;
    private static object _initializationLock = new();

    static GdalUtils()
    {
        EnsureInitialized();
    }

    public static void EnsureInitialized()
    {
        if (_isInitialized)
            return;
        lock (_initializationLock)
        {
            if (_isInitialized)
                return;
            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Opens and returns the dataset at the specified file name.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown on failure.
    /// </exception>
    public static Dataset OpenDataset(string filename) => OpenDataset([filename]);

    /// <summary>
    /// Opens and returns the dataset at the specified file name. If
    /// multiple file names specified, a VRT (virtual raster) dataset
    /// is created on-the-fly.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown on failure.
    /// </exception>
    public static Dataset OpenDataset(IEnumerable<string> filenames)
    {
        if (!filenames.Any())
            throw new ArgumentException("No files.", nameof(filenames));

        if (filenames.Count() > 1)
        {
            // Create VRT
            return CreateVrt(filenames);
        }

        string filename = filenames.Single();
        Dataset dataset = Gdal.Open(filename, Access.GA_ReadOnly);
        if (dataset == null)
            throw new ApplicationException("Failed to open " + filename);
        return dataset;
    }

    /// <summary>
    /// Creates an in-memory GDAL dataset from the specified
    /// (single-band) raw data.
    /// </summary>
    public static Dataset CreateInMemoryDataset(
        float[] data, int width, int height,
        double[] geoTransform,
        Srs srs,
        float? noDataValue)
    {
        // Get the MEM driver
        Driver memDriver = Gdal.GetDriverByName("MEM");
        if (memDriver == null)
            throw new Exception("MEM driver not available.");

        // Create the in-memory dataset
        Dataset dataset = memDriver.Create(
            "", width, height, bands: 1, DataType.GDT_Float32, options: null);
        if (dataset == null)
            throw new Exception("Failed to create in-memory dataset.");
        dataset.SetGeoTransform(geoTransform);
        dataset.SetProjection(srs.GetWkt());

        // Create raster band
        Band band = dataset.GetRasterBand(1);
        if (noDataValue.HasValue)
            band.SetNoDataValue(noDataValue.Value);

        // Write the buffer to the raster band
        band.WriteRaster(0, 0, width, height, data, width, height, 0, 0);

        band.FlushCache();
        dataset.FlushCache();
        return dataset;
    }

    /// <summary>
    /// Creates an in-memory GDAL dataset from the specified 
    /// RGB or RGBA raw data.
    /// </summary>
    public static Dataset CreateInMemoryDataset(
        byte[] r, byte[] g, byte[] b, byte[]? a,
        int width, int height,
        double[] geoTransform,
        Srs srs)
    {
        // Get the MEM driver
        Driver memDriver = Gdal.GetDriverByName("MEM");
        if (memDriver == null)
            throw new Exception("MEM driver not available.");

        // Create the in-memory dataset
        int bandCount = a == null ? 3 : 4;
        Dataset dataset = memDriver.Create(
            "", width, height, bands: bandCount, DataType.GDT_Byte, options: null);
        if (dataset == null)
            throw new Exception("Failed to create in-memory dataset.");
        dataset.SetGeoTransform(geoTransform);
        dataset.SetProjection(srs.GetWkt());

        // Create and fill raster bands
        Band rBand = dataset.GetRasterBand(1);
        rBand.SetColorInterpretation(ColorInterp.GCI_RedBand);
        rBand.WriteRaster(0, 0, width, height, r, width, height, 0, 0);
        Band gBand = dataset.GetRasterBand(2);
        gBand.SetColorInterpretation(ColorInterp.GCI_GreenBand);
        gBand.WriteRaster(0, 0, width, height, g, width, height, 0, 0);
        Band bBand = dataset.GetRasterBand(3);
        bBand.SetColorInterpretation(ColorInterp.GCI_BlueBand);
        bBand.WriteRaster(0, 0, width, height, b, width, height, 0, 0);
        Band? aBand = null;
        if (a != null) {
            aBand = dataset.GetRasterBand(4);
            aBand.SetColorInterpretation(ColorInterp.GCI_AlphaBand);
            aBand.WriteRaster(0, 0, width, height, a, width, height, 0, 0);
        }
        
        rBand.FlushCache();
        gBand.FlushCache();
        bBand.FlushCache();
        aBand?.FlushCache();
        dataset.FlushCache();
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

    /// <summary>
    /// Returns geo transform from the specified raster parameters.
    /// </summary>
    public static double[] GetGeoTransform(
        Bounds rasterBounds, int widthPx, int heightPx)
    {
        double originX = rasterBounds.XMin;
        double originY = rasterBounds.YMax;
        double pixelWidth = (rasterBounds.XMax - rasterBounds.XMin) / widthPx;
        double pixelHeight = (rasterBounds.YMin - rasterBounds.YMax) / heightPx; // usually negative
        
        double[] geoTransform = [
            originX, pixelWidth, 0,
            originY, 0, pixelHeight];
        return geoTransform;
    }

    #endregion

    #region Info and reporting

    public static string GetRasterInfo(string filename)
    {
        StringBuilderEx sb = new();
        AppendRasterInfo(filename, sb);
        return sb.ToString();
    }
    public static string GetRasterInfo(Dataset dataset)
    {
        StringBuilderEx sb = new();
        AppendRasterInfo(dataset, sb);
        return sb.ToString();
    }
    public static void AppendRasterInfo(string filename, StringBuilderEx sb)
    {
        using Dataset dataset = Gdal.Open(filename, Access.GA_ReadOnly);
        if (dataset == null) throw new ApplicationException("Failed to open " + filename);
        AppendRasterInfo(dataset, sb);
    }
    public static void AppendRasterInfo(Dataset ds, StringBuilderEx sb)
    {
        #if !VERBOSE
        sb.AppendLine($"Raster Dataset. Count: {ds.RasterCount}, Size: {ds.RasterXSize}x{ds.RasterYSize}");
        #else
        sb.AppendLine($"Raster dataset parameters:");
        sb.Indent();
        sb.AppendLine("Projection: " + ds.GetProjectionRef());
        sb.AppendLine("RasterCount: " + ds.RasterCount);
        sb.AppendLine("RasterSize (" + ds.RasterXSize + "," + ds.RasterYSize + ")");
        
        // Get metadata
        string[] metadata = ds.GetMetadata("");
        if (metadata.Length > 0)
        {
            sb.AppendLine("Metadata:");
            sb.Indent();
            for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
                sb.AppendLine(iMeta + ": " + metadata[iMeta]);
            sb.Unindent();
            sb.AppendLine();
        }

        // Report "IMAGE_STRUCTURE" metadata.
        metadata = ds.GetMetadata("IMAGE_STRUCTURE");
        if (metadata.Length > 0)
        {
            sb.AppendLine("Image Structure Metadata:");
            sb.Indent();
            for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
                sb.AppendLine(iMeta + ":  " + metadata[iMeta]);
            sb.Unindent();
            sb.AppendLine();
        }

        // Report geolocation.
        metadata = ds.GetMetadata("GEOLOCATION");
        if (metadata.Length > 0)
        {
            sb.AppendLine("Geolocation:");
            sb.Indent();
            for (int iMeta = 0; iMeta < metadata.Length; iMeta++)
                sb.AppendLine("    " + iMeta + ":  " + metadata[iMeta]);
            sb.Unindent();
            sb.AppendLine();
        }
        sb.Unindent();

        // Report corners
        sb.AppendLine("Corner Coordinates:");
        sb.Indent();
        sb.AppendLine("Upper Left (" + PixelToGeo(ds, 0.0, 0.0) + ")");
        sb.AppendLine("Lower Left (" + PixelToGeo(ds, 0.0, ds.RasterYSize) + ")");
        sb.AppendLine("Upper Right (" + PixelToGeo(ds, ds.RasterXSize, 0.0) + ")");
        sb.AppendLine("Lower Right (" + PixelToGeo(ds, ds.RasterXSize, ds.RasterYSize) + ")");
        sb.AppendLine("Center (" + PixelToGeo(ds, ds.RasterXSize / 2, ds.RasterYSize / 2) + ")");
        sb.Unindent();
        sb.AppendLine();

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
        #endif
    }

    public static string GetRasterBandSummary(Dataset dataset)
    {
        StringBuilderEx sb = new();
        AppendRasterBandSummary(dataset, sb);
        return sb.ToString();
    }
    public static void AppendRasterBandSummary(Dataset dataset, StringBuilderEx sb)
    {
        sb.AppendLine(string.Join(", ", dataset.GetFileList()));
#if !VERBOSE
        sb.Indent();
        for (int i = 1; i <= dataset.RasterCount; i++) // NOTE: these are 1-indexed
        {
            Band band = dataset.GetRasterBand(i);
            sb.AppendLine($"Band {i}: {band.DataType}, {Gdal.GetDataTypeSize(band.DataType)}, {band.GetRasterColorInterpretation()}");
        }
        sb.Unindent();
#else
        sb.AppendLine($"Size: {dataset.RasterXSize}x{dataset.RasterYSize} px");
        sb.AppendLine("Raster count: " + dataset.RasterCount);
        for (int i = 1; i <= dataset.RasterCount; i++) // NOTE: these are 1-indexed
        {
            Band band = dataset.GetRasterBand(i);
            sb.AppendLine("Band " + i);
            sb.Indent();
            sb.AppendLine("DataType: " + band.DataType);
            sb.AppendLine("Size: " + Gdal.GetDataTypeSize(band.DataType));
            sb.AppendLine("Interpretation: " + band.GetRasterColorInterpretation());

            band.GetNoDataValue(out double noDataValue, out int hasNoDataValue);
            sb.AppendLine("Has no-data value: " + hasNoDataValue);
            sb.AppendLine("No-data value: " + noDataValue);
            sb.Unindent();
        }
#endif
    }

#endregion
    
    #region Reprojection / Warping

    /// <summary>
    /// Reprojects (warps) the source file, saves and returns
    /// the resulting file path.
    /// </summary>
    public static string Warp(string sourceFilename, Srs destSrs)
    {
        // First, check if there's an existing matching warped file
        string destFilePath = GetWarpDestFilePath(sourceFilename, destSrs);
        if (File.Exists(destFilePath))
            return destFilePath;
        Directory.CreateDirectory(FileSystemHelpers.WarpCachePath);

        // Get source SRS
        using Dataset sourceDataset = OpenDataset(sourceFilename);
        using Srs sourceSrs = Srs.FromDataset(sourceDataset);

        // Warp
        string[] warpParams = [
            "-s_srs", sourceSrs.GetWkt(),
            "-t_srs", destSrs.GetWkt(),
            "-r", "lanczos",
            "-of", "gtiff",
            "-srcnodata", "-9999",
            "-dstnodata", "-9999",
            "-multi",
        ];
        GDALWarpAppOptions appOptions = new(warpParams);
        Debug.WriteLine("Warp params: " + string.Join(Environment.NewLine, warpParams));
        Debug.WriteLine($"Source area: " + sourceSrs.BoundsLatLon);

        using Dataset result = Gdal.Warp(destFilePath,
            [sourceDataset],
            appOptions,
            callback: null,
            callback_data: null);

        using Srs resultSrs = Srs.FromDataset(result);
        Debug.WriteLine($"Result area: " + resultSrs.BoundsLatLon);

        return destFilePath;
    }

    private static string GetWarpDestFilePath(string sourceFilename, Srs destSrs)
    {
        // NOTE: We calculate checksums, not based on file contents, but
        // based on file metadata (path, last modified, and destination SRS).
        // Those checksums will determine whether we need to re-warp the file.
        string baseFilename = Path.GetFileNameWithoutExtension(sourceFilename);
        string sourcePathHash = GetShortHash(Path.GetFullPath(sourceFilename));
        string srsHash = GetSrsSummary(destSrs);
        string timestamp = File.GetLastWriteTime(sourceFilename).ToString("s").Replace(':', '_');
        string destFilePath = Path.Combine(
            FileSystemHelpers.WarpCachePath,
            $"{baseFilename}_{sourcePathHash}_{srsHash}_{timestamp}.tif");
        return destFilePath;
    }

    private static string GetSrsSummary(Srs srs)
    {
        // If we know the EPSG code, use that
        if (srs.Epsg != null)
            return "EPSG" + srs.Epsg.Value;

        // otherwise use a hash of the WKT
        return GetShortHash(srs.GetWkt());
    }

    private static string GetShortHash(string input)
    {
        // Based on: https://stackoverflow.com/questions/9837732/calculate-a-checksum-for-a-string
        string hash;
        using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
        {
            hash = BitConverter.ToString(
              md5.ComputeHash(Encoding.UTF8.GetBytes(input))
            ).Replace("-", "");
        }
        return hash.Substring(0, 8);
    }

    /// <summary>
    /// Reprojects (warps) the source dataset and returns the result.
    /// </summary>
    /// <remarks>
    /// Does not support NODATA (transparency) in result. Use other overloads
    /// for that.
    /// </remarks>
    public static Dataset Warp(Dataset sourceDataset, Srs destSrs)
    {
        string sourceWkt = Srs.FromDataset(sourceDataset).GetWkt();
        string destWkt = destSrs.GetWkt();
        using Dataset destDataset = Gdal.AutoCreateWarpedVRT(sourceDataset,
            sourceWkt, destWkt, ResampleAlg.GRA_Lanczos,
            maxerror: 0 // use exact calculations
            );
        return destDataset;
    }

    #endregion

    #region VRT support

    public static Dataset CreateVrt(IEnumerable<string> filenames)
    {
        Dataset? vrt = Gdal.wrapper_GDALBuildVRT_names(
            "dest", // doesn't matter; we're not writing this to disk
            filenames.ToArray(),
            new GDALBuildVRTOptions([]),
            null, null);
        if (vrt == null)
            throw new ApplicationException("Failed to create VRT: " +
                Gdal.GetLastErrorMsg());
        vrt.FlushCache();
        return vrt;
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

}
    #endregion