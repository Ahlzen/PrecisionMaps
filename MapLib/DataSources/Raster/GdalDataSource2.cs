using MapLib.GdalSupport;
using MapLib.Geometry;
using OSGeo.GDAL;
using System.IO;
using System.Text;

namespace MapLib.DataSources.Raster;

public class GdalDataSource2 : BaseRasterDataSource2
{
    public override string Name => "Raster File (using GDAL)";
    
    public string Filename { get; }

    public double[] AffineGeoTransform { get; } // pixel to geo
    public double[] AffineInverseGeoTransform { get; } // geo to pixel

    // NOTE: Either SingleBandData or PixelData should be non-null,
    // depending on the type of data in the source.
  
    public RasterData2 RasterData { get; }
    public override string Srs => RasterData.Srs;
    public override Bounds? Bounds => RasterData.Bounds;

    public GdalDataSource2(string filename)
    {
        Filename = filename;
        using Dataset dataset = OpenDataset(filename);

        // Get pixel <-> geo transforms
        AffineGeoTransform = new double[6];
        AffineInverseGeoTransform = new double[6];
        dataset.GetGeoTransform(AffineGeoTransform);
        Gdal.InvGeoTransform(AffineGeoTransform, AffineInverseGeoTransform);

        // Get size, projection and bounds
        int widthPx = dataset.RasterXSize;
        int heightPx = dataset.RasterYSize;
        int pixelCount = widthPx * heightPx;
        Bounds bounds = Geometry.Bounds.FromCoords([
            new Coord(PixelToGeo(new Coord(0, 0))),
            new Coord(PixelToGeo(new Coord(widthPx - 1, heightPx - 1)))]);
        string srs = GdalUtils.GetSrsAsWkt(dataset);

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
        // Some data sets could (like 8-bit grayscale) could theoretically
        // be either.
        // Then read the raster data and convert to standard format.

        // NOTE: We only support certain common (and useful to us)
        // raster band configurations

        byte[]? imageData = null;
        float[]? singleBandData = null;

        if (rasterCount == 1)
        {
            Band band = dataset.GetRasterBand(1);

            if (bandDataTypes[0] == DataType.GDT_Byte &&
                bandColorInterp[0] == ColorInterp.GCI_GrayIndex)
            {
                // 8-bit grayscale image

                // TODO: Perhaps reading it three times over using ReadRaster(),
                // directly into each of the R, G, B channels, would be more efficient?

                // Read the grayscale band
                byte[] buffer = new byte[pixelCount];
                band.ReadRaster(0, 0, widthPx, heightPx, buffer,
                    widthPx, heightPx, 0, 0);

                // Build ARGB image data
                imageData = new byte[pixelCount * 4];
                for (int pixel = 0; pixel < pixelCount; pixel++) {
                    int offset = pixel * 4;
                    byte gray = buffer[pixel];
                    imageData[offset] = 255; // A
                    imageData[offset + 1] = gray; // R
                    imageData[offset + 2] = gray; // G
                    imageData[offset + 3] = gray; // B
                }
            }
            else if (bandDataTypes[0] == DataType.GDT_Byte &&
                bandColorInterp[0] == ColorInterp.GCI_Undefined)
            {
                // 8-bit raw data

                byte[] buffer = new byte[pixelCount];
                band.ReadRaster(0, 0, widthPx, heightPx, buffer,
                    widthPx, heightPx, 0, 0);

                // Build single-band raw data
                singleBandData = new float[pixelCount];
                for (int pixel = 0; pixel < pixelCount; pixel++)
                    singleBandData[pixel] = (float)buffer[pixel];
            }
            else if (bandDataTypes[0] == DataType.GDT_Float32 &&
                bandColorInterp[0] == ColorInterp.GCI_GrayIndex)
            {
                // 32-bit float raw data

                singleBandData = new float[pixelCount];
                band.ReadRaster(0, 0, widthPx, heightPx, singleBandData,
                    widthPx, heightPx, 0, 0);
            }
            else if (bandDataTypes[0] == DataType.GDT_Byte &&
                bandColorInterp[0] == ColorInterp.GCI_PaletteIndex)
            {
                // 8-bit indexed (256 color) RGB

                byte[] buffer = new byte[pixelCount];
                band.ReadRaster(0, 0, widthPx, heightPx, buffer,
                    widthPx, heightPx, 0, 0);

                // Read color table
                ColorTable colorTable = band.GetColorTable();
                int colorCount = colorTable.GetCount();
                byte[] ctR = new byte[colorCount];
                byte[] ctG = new byte[colorCount];
                byte[] ctB = new byte[colorCount];
                byte[] ctA = new byte[colorCount];
                for (int c = 0; c < colorCount; c++) {
                    ColorEntry entry = colorTable.GetColorEntry(c);
                    ctR[c] = (byte) Math.Min(entry.c1, (short)255);
                    ctG[c] = (byte) Math.Min(entry.c2, (short)255);
                    ctB[c] = (byte) Math.Min(entry.c3, (short)255);
                    ctA[c] = (byte) Math.Min(entry.c4, (short)255);
                }

                // Build ARGB image data
                imageData = new byte[pixelCount * 4];
                for (int pixel = 0; pixel < pixelCount; pixel++) {
                    int offset = pixel * 4;
                    byte colorIndex = buffer[pixel];
                    imageData[offset] = ctA[colorIndex]; // A
                    imageData[offset + 1] = ctR[colorIndex]; // R
                    imageData[offset + 2] = ctG[colorIndex]; // G
                    imageData[offset + 3] = ctB[colorIndex]; // B
                    
                }
            }
            else
                throw new NotSupportedException(
                    "Unsupported raster band configuration.");
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
                    throw new NotSupportedException(
                        "Unsupported raster band configuration.");
                byte[] buffer = new byte[pixelCount];
                band.ReadRaster(0, 0, widthPx, heightPx, buffer,
                    widthPx, heightPx, 0, 0);
                int byteOffset = band.GetRasterColorInterpretation() switch {
                    ColorInterp.GCI_AlphaBand or
                    ColorInterp.GCI_Undefined => 0,
                    ColorInterp.GCI_RedBand => 1,
                    ColorInterp.GCI_GreenBand => 2,
                    ColorInterp.GCI_BlueBand => 3,
                    _ => throw new NotSupportedException(
                        "Unsupported raster band configuration.")
                };
                for (int pixel = 0; pixel < pixelCount; pixel++)
                    imageData[pixel * 4 + byteOffset] = buffer[pixel];
            }
        }
        else
            throw new NotSupportedException(
                "Unsupported raster band configuration.");

        RasterData = new RasterData2(
            srs, bounds, widthPx, heightPx, imageData, singleBandData);
    }

    public string GetRasterBandSummary()
    {
        StringBuilder sb = new();
        using Dataset dataset = OpenDataset(Filename);
        sb.AppendLine(Filename);
        sb.AppendLine($"Size: {dataset.RasterXSize}x{dataset.RasterYSize} px");
        sb.AppendLine("Raster count: " + dataset.RasterCount);
        for (int i = 1; i <= dataset.RasterCount; i++) // NOTE: these are 1-indexed
        {
            Band band = dataset.GetRasterBand(i);
            sb.AppendLine("Band " + i);
            sb.AppendLine(" DataType: " + band.DataType);
            sb.AppendLine(" Size: " + Gdal.GetDataTypeSize(band.DataType));
            sb.AppendLine(" Interpretation: " + band.GetRasterColorInterpretation());
        }
        return sb.ToString();
    }

    public override RasterData2 GetData()
    {
        return RasterData;
    }

    public override RasterData2 GetData(string destSrs)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Opens and returns the dataset at the specified file name.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown on failure.
    /// </exception>
    private Dataset OpenDataset(string filename)
    {
        Dataset dataset = Gdal.Open(filename, Access.GA_ReadOnly);
        if (dataset == null)
            throw new ApplicationException("Failed to open " + filename);
        return dataset;
    }

    /// <summary>
    /// Returns the specified pixel coordinates transformed to
    /// geometric coordinates in the raster's SRS.
    /// </summary>
    private Coord PixelToGeo(Coord c)
        => new Coord(
            AffineGeoTransform[0] + AffineGeoTransform[1] * c.X + AffineGeoTransform[2] * c.Y,
            AffineGeoTransform[3] + AffineGeoTransform[4] * c.X + AffineGeoTransform[5] * c.Y);

    /// <summary>
    /// Returns the specified geometric coordinates in the raster's
    /// SRS transformed to pixel coordinates.
    /// </summary>
    private Coord GeoToPixel(Coord c)
        => new Coord(
            AffineInverseGeoTransform[0] + AffineInverseGeoTransform[1] * c.X + AffineInverseGeoTransform[2] * c.Y,
            AffineInverseGeoTransform[3] + AffineInverseGeoTransform[4] * c.X + AffineInverseGeoTransform[5] * c.Y);

}
