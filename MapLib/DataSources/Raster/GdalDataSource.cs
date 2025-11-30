using MapLib.GdalSupport;
using MapLib.Geometry;
using OSGeo.GDAL;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace MapLib.DataSources.Raster;

public class GdalDataSource : BaseRasterDataSource
{
    public override string Name => "Raster File (using GDAL)";

    public List<string> Filenames { get; }

    private Srs _srs;
    public override Srs Srs => _srs;

    private Bounds _bounds;
    public override Bounds Bounds => _bounds;

    public override bool IsBounded => true; // Probably depends on the source

    /// <summary>
    /// Width of the raster data in the original data source.
    /// NOTE: These properties are updated if reprojected.
    /// </summary>
    public int SourceWidthPx { get; private set; }
    public int SourceHeightPx { get; private set; }

    /// <summary>
    /// Width of the bitmap data that we read the source into.
    /// NOTE: These properties are updated if reprojected.
    /// </summary>
    /// <remarks>
    /// Usually the same as the source, but may be smaller than the source, e.g.
    /// if we read a lower resolution version of a large raster data set.
    /// </remarks>
    public int BitmapWidthPx { get; private set; }
    public int BitmapHeightPx { get; private set; }



    /// <param name="filename">Name/path of input file</param>
    /// <param name="scaleFactor">
    /// Scale at which the data is read.
    /// 1 = full resolution,
    /// <1 = downsampled (e.g. 0.25 = quarter resolution),
    /// null = auto (full resolution, unless too large to read as a single dataset)
    /// </param>
    public GdalDataSource(string filename, double? scaleFactor = null)
        : this([filename], scaleFactor)
    {
    }

    public GdalDataSource(IEnumerable<string> filenames, double? scaleFactor = null)
    {
        Filenames = filenames.ToList();

        if (Filenames.Count == 0)
            throw new ArgumentException("No files.", nameof(filenames));

        Dataset? dataset = Filenames.Count == 1 ?
            GdalUtils.OpenDataset(Filenames[0]) :
            GdalUtils.CreateVrt(filenames); // Multiple files: Create VRT (virtual raster)
        InitPropertiesFromDataset(dataset, scaleFactor);

        // For debugging
        //Console.WriteLine(GdalUtils.GetRasterBandSummary(dataset));

        dataset.Dispose();
    }

    public GdalDataSource(Dataset dataset, double scaleFactor = 1)
    {
        Filenames = new List<string>();
        InitPropertiesFromDataset(dataset, scaleFactor);

        // For debugging
        //Console.WriteLine(GdalUtils.GetRasterBandSummary(dataset));
    }

    [MemberNotNull(nameof(_srs))]
    [MemberNotNull(nameof(_bounds))]
    private void InitPropertiesFromDataset(Dataset dataset, double? scaleFactor)
    {
        _srs = Srs.FromDataset(dataset);
        _bounds = GdalUtils.GetBounds(dataset);

        SourceWidthPx = dataset.RasterXSize;
        SourceHeightPx = dataset.RasterYSize;

        BitmapWidthPx = (int)Math.Round(dataset.RasterXSize * (scaleFactor ?? 1));
        BitmapHeightPx = (int)Math.Round(dataset.RasterYSize * (scaleFactor ?? 1));

        if (scaleFactor == null)
        {
            // If scaleFactor is null (auto), make it progressively smaller until it fits
            scaleFactor = 1;
            while (BitmapDataIsTooLarge(BitmapWidthPx, BitmapHeightPx))
            {
                scaleFactor *= 0.5;
                BitmapWidthPx = (int)Math.Round(dataset.RasterXSize * (scaleFactor ?? 1));
                BitmapHeightPx = (int)Math.Round(dataset.RasterYSize * (scaleFactor ?? 1));
            }
        }
    }

    public override Task<RasterData> GetData(Srs? destSrs = null)
    {
        List<string> filenames = new(Filenames);

        if (destSrs != null && destSrs != Srs)
        {
            // Reproject source data file(s), and use that
            for (int i = 0; i < filenames.Count; i++)
            {
                filenames[i] = GdalUtils.Warp(filenames[i], destSrs);
            }
        }
        using Dataset sourceDataset =
            GdalUtils.OpenDataset(filenames);

        // Update properties again. They may have changed if the
        // data was reprojected:
        InitPropertiesFromDataset(sourceDataset, null);

        Console.WriteLine(GdalUtils.GetRasterInfo(sourceDataset));
        return Task.FromResult(GetRasterData(sourceDataset));
    }

    public override Task<RasterData> GetData(Bounds boundsWgs84, Srs? destSrs = null)
        => GetData(destSrs);

    private bool BitmapDataIsTooLarge(long pixelsX, long pixelsY)
    {
        long pixelCount = pixelsX * pixelsY;
        return pixelCount * 4 > Array.MaxLength;
    }

    private RasterData GetRasterData(Dataset dataset)
    {
        // Get size, projection and bounds
        long pixelCount = BitmapWidthPx * BitmapHeightPx;

        // check if the source is too big
        if (BitmapDataIsTooLarge(BitmapWidthPx, BitmapHeightPx))
            throw new ApplicationException(
                "GDAL Raster is too large. Try using a smaller scale factor.");

        var affineGeoTransform = new double[6];
        dataset.GetGeoTransform(affineGeoTransform);
        Bounds bounds = Geometry.Bounds.FromCoords([
            new Coord(GdalUtils.PixelToGeo(affineGeoTransform,
                new Coord(0, 0))),
            new Coord(GdalUtils.PixelToGeo(affineGeoTransform,
                new Coord(SourceWidthPx - 1, SourceHeightPx - 1)))]);
        Srs srs = Srs.FromDataset(dataset);

        // Get raster band configuration
        int rasterCount = dataset.RasterCount;
        if (rasterCount == 0)
            throw new InvalidOperationException("No raster layers in Dataset");
        List<DataType> bandDataTypes = new(rasterCount);
        List<ColorInterp> bandColorInterp = new(rasterCount);
        for (int i = 1; i <= rasterCount; i++) // NOTE: these are 1-indexed
        {
            Band band = dataset.GetRasterBand(i);
            bandDataTypes.Add(band.DataType);
            bandColorInterp.Add(band.GetColorInterpretation());
        }

        // Determine whether this is an image (e.g. an orthophoto or
        // shaded relief) or raw data (like elevation data or canopy density).
        // Some data sets (like 8-bit grayscale) could theoretically
        // be either.
        // Then read the raster data and convert to standard format.

        // NOTE: We only support certain common (and useful to us)
        // raster band configurations

        byte[]? imageData = null;
        float[]? singleBandData = null;
        float? noDataValue = null;

        if (rasterCount == 1)
        {
            Band band = dataset.GetRasterBand(1);
            band.GetNoDataValue(out double rawNoDataValue, out int hasNoDataValue);
            if (hasNoDataValue == 1)
                noDataValue = (float)rawNoDataValue;

            if (bandDataTypes[0] == DataType.GDT_Byte &&
                bandColorInterp[0] == ColorInterp.GCI_GrayIndex)
            {
                // 8-bit grayscale image

                // TODO: Perhaps reading it three times over using ReadRaster(),
                // directly into each of the R, G, B channels, would be more efficient?

                byte? noDataByte = hasNoDataValue == 1 ? (byte)rawNoDataValue : null;

                // Read the grayscale band
                byte[] buffer = new byte[pixelCount];
                band.ReadRaster(0, 0, SourceWidthPx, SourceHeightPx, buffer,
                    BitmapWidthPx, BitmapHeightPx, 0, 0);

                // Build ARGB image data
                imageData = new byte[pixelCount * 4];
                for (long pixel = 0; pixel < pixelCount; pixel++)
                {
                    long offset = pixel * 4;
                    byte gray = buffer[pixel];
                    imageData[offset + 3] = (gray == noDataByte) ? (byte)0 : (byte)255; // A
                    imageData[offset + 2] = gray; // R
                    imageData[offset + 1] = gray; // G
                    imageData[offset + 0] = gray; // B
                }
            }
            else if (bandDataTypes[0] == DataType.GDT_Byte &&
                bandColorInterp[0] == ColorInterp.GCI_Undefined)
            {
                // 8-bit raw data

                var buffer = new byte[pixelCount];
                band.ReadRaster(0, 0, SourceWidthPx, SourceHeightPx, buffer,
                    BitmapWidthPx, BitmapHeightPx, 0, 0);
                singleBandData = new float[pixelCount];
                for (long pixel = 0; pixel < pixelCount; pixel++)
                    singleBandData[pixel] = (float)buffer[pixel];
            }
            else if (bandDataTypes[0] == DataType.GDT_Int16 &&
                bandColorInterp[0] == ColorInterp.GCI_Undefined)
            {
                // 16-bit raw data

                var buffer = new Int16[pixelCount];
                band.ReadRaster(0, 0, SourceWidthPx, SourceHeightPx, buffer,
                    BitmapWidthPx, BitmapHeightPx, 0, 0);
                singleBandData = new float[pixelCount];
                for (long pixel = 0; pixel < pixelCount; pixel++)
                    singleBandData[pixel] = (float)buffer[pixel];
            }
            else if (bandDataTypes[0] == DataType.GDT_Float32 &&
                bandColorInterp[0] == ColorInterp.GCI_GrayIndex)
            {
                // 32-bit float raw data

                singleBandData = new float[pixelCount];
                band.ReadRaster(0, 0, SourceWidthPx, SourceHeightPx, singleBandData,
                    BitmapWidthPx, BitmapHeightPx, 0, 0);
            }
            else if (bandDataTypes[0] == DataType.GDT_Byte &&
                bandColorInterp[0] == ColorInterp.GCI_PaletteIndex)
            {
                // 8-bit indexed (256 color) RGB

                byte? noDataByte = hasNoDataValue == 1 ? (byte)rawNoDataValue : null;

                byte[] buffer = new byte[pixelCount];
                band.ReadRaster(0, 0, SourceWidthPx, SourceHeightPx, buffer,
                    BitmapWidthPx, BitmapWidthPx, 0, 0);

                // Read color table
                ColorTable colorTable = band.GetColorTable();
                int colorCount = colorTable.GetCount();
                var paletteInterp = colorTable.GetPaletteInterpretation();

                byte[] ctR = new byte[colorCount];
                byte[] ctG = new byte[colorCount];
                byte[] ctB = new byte[colorCount];
                byte[] ctA = new byte[colorCount];
                for (int c = 0; c < colorCount; c++)
                {
                    ColorEntry entry = colorTable.GetColorEntry(c);
                    ctR[c] = (byte)Math.Min(entry.c1, (short)255);
                    ctG[c] = (byte)Math.Min(entry.c2, (short)255);
                    ctB[c] = (byte)Math.Min(entry.c3, (short)255);
                    ctA[c] = (byte)Math.Min(entry.c4, (short)255);
                }

                // Build ARGB image data
                imageData = new byte[pixelCount * 4];
                for (long pixel = 0; pixel < pixelCount; pixel++)
                {
                    long offset = pixel * 4;
                    byte colorIndex = buffer[pixel];
                    imageData[offset + 3] = (colorIndex == noDataByte) ? (byte)0 : ctA[colorIndex]; // A
                    imageData[offset + 2] = ctR[colorIndex]; // R
                    imageData[offset + 1] = ctG[colorIndex]; // G
                    imageData[offset + 0] = ctB[colorIndex]; // B
                }
            }
            else
                ThrowUnsupportedRasterBandConfiguration(dataset);
        }
        else if ((rasterCount == 3 || rasterCount == 4) &&
            bandColorInterp.Contains(ColorInterp.GCI_RedBand) &&
            bandColorInterp.Contains(ColorInterp.GCI_GreenBand) &&
            bandColorInterp.Contains(ColorInterp.GCI_BlueBand) &&
            bandDataTypes[0] == DataType.GDT_Byte)
        {
            // 8/8/8-bit RGB image or
            // 8/8/8/8-bit RGBA or ARGB image

            imageData = new byte[pixelCount * 4];

            // Fill with 255 (fully opaque) in case there is no alpha channel
            Array.Fill<byte>(imageData, 255);

            // Process one band (channel) at a time
            for (int b = 1; b <= rasterCount; b++)
            {
                Band band = dataset.GetRasterBand(b);
                if (band.DataType != DataType.GDT_Byte)
                    ThrowUnsupportedRasterBandConfiguration(dataset);

                band.GetNoDataValue(out double rawNoDataValue, out int hasNoDataValue);
                byte noDataByte = (hasNoDataValue == 1) ? (byte)rawNoDataValue : (byte)0;

                byte[] buffer = new byte[pixelCount];
                band.ReadRaster(0, 0, SourceWidthPx, SourceHeightPx, buffer,
                    BitmapWidthPx, BitmapHeightPx, 0, 0);

                ColorInterp colorInterpretation = band.GetRasterColorInterpretation();
                bool isAlphaBand = false;
                long byteOffset = 0;

                switch (colorInterpretation)
                {
                    case ColorInterp.GCI_Undefined:
                    case ColorInterp.GCI_AlphaBand:
                        {
                            isAlphaBand = true;
                            byteOffset = 3;
                            break;
                        }
                    case ColorInterp.GCI_RedBand:
                        {
                            byteOffset = 2; break;
                        }
                    case ColorInterp.GCI_GreenBand:
                        {
                            byteOffset = 1; break;
                        }
                    case ColorInterp.GCI_BlueBand:
                        {
                            byteOffset = 0; break;
                        }
                    default:
                        ThrowUnsupportedRasterBandConfiguration(dataset);
                        break;
                };

                if (byteOffset == -1) continue;
                if (isAlphaBand && hasNoDataValue == 1)
                {
                    for (long pixel = 0; pixel < pixelCount; pixel++)
                        imageData[pixel * 4 + byteOffset] =
                            buffer[pixel] == noDataByte ? (byte)0 : (byte)255;
                }
                else
                    for (long pixel = 0; pixel < pixelCount; pixel++)
                        imageData[pixel * 4 + byteOffset] = buffer[pixel];
            }
        }
        else
            ThrowUnsupportedRasterBandConfiguration(dataset);

        if (imageData != null)
            return new ImageRasterData(srs, bounds, BitmapWidthPx, BitmapHeightPx, imageData!);
        else
            return new SingleBandRasterData(srs, bounds, BitmapWidthPx, BitmapHeightPx,
                singleBandData!, noDataValue);
    }

    private void ThrowUnsupportedRasterBandConfiguration(Dataset dataset)
    {
        string rasterInfo = GdalUtils.GetRasterBandSummary(dataset);
        throw new NotSupportedException(
            "Unsupported raster band configuration:" + Environment.NewLine +
            rasterInfo);
    }
}
