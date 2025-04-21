﻿using MapLib.GdalSupport;
using MapLib.Geometry;
using OSGeo.GDAL;
using System.IO;
using System.Text;

namespace MapLib.DataSources.Raster;

public class GdalDataSource2 : BaseRasterDataSource2
{
    public override string Name => "Raster File (using GDAL)";
    
    public string Filename { get; }

    //public double[] AffineGeoTransform { get; } // pixel to geo
    //public double[] AffineInverseGeoTransform { get; } // geo to pixel

    // NOTE: Either SingleBandData or PixelData should be non-null,
    // depending on the type of data in the source.

    public override string Srs { get; }
    public override Bounds? Bounds { get; }

    public int WidthPx { get; }
    public int HeightPx { get; }

    public GdalDataSource2(string filename)
    {
        using Dataset dataset = OpenDataset(filename);

        Filename = filename;
        Srs = GdalUtils.GetSrsAsWkt(dataset);
        Bounds = GdalUtils.GetBounds(dataset);
        WidthPx = dataset.RasterXSize;
        HeightPx = dataset.RasterYSize;


        

        //// Get size, projection and bounds
        //int widthPx = dataset.RasterXSize;
        //int heightPx = dataset.RasterYSize;
        //int pixelCount = widthPx * heightPx;
        //var affineGeoTransform = new double[6];
        //dataset.GetGeoTransform(affineGeoTransform);
        //Bounds bounds = Geometry.Bounds.FromCoords([
        //    new Coord(PixelToGeo(new Coord(0, 0))),
        //    new Coord(PixelToGeo(new Coord(widthPx - 1, heightPx - 1)))]);
        //string srs = GdalUtils.GetSrsAsWkt(dataset);

        //// Get raster band configuration
        //int rasterCount = dataset.RasterCount;
        //if (rasterCount == 0)
        //    throw new InvalidOperationException("No raster layers in Dataset");
        //List<DataType> bandDataTypes = new(rasterCount);
        //List<ColorInterp> bandColorInterp = new(rasterCount);
        //for (int i = 1; i <= rasterCount; i++) // NOTE: these are 1-indexed
        //{
        //    Band band = dataset.GetRasterBand(i);
        //    bandDataTypes.Add(band.DataType);
        //    bandColorInterp.Add(band.GetColorInterpretation());
        //}

        //// Determine whether this is an image (e.g. an orthophoto or
        //// shaded relief) or raw data (like elevation data or canopy density).
        //// Some data sets could (like 8-bit grayscale) could theoretically
        //// be either.
        //// Then read the raster data and convert to standard format.

        //// NOTE: We only support certain common (and useful to us)
        //// raster band configurations

        //byte[]? imageData = null;
        //float[]? singleBandData = null;

        //if (rasterCount == 1)
        //{
        //    Band band = dataset.GetRasterBand(1);

        //    if (bandDataTypes[0] == DataType.GDT_Byte &&
        //        bandColorInterp[0] == ColorInterp.GCI_GrayIndex)
        //    {
        //        // 8-bit grayscale image

        //        // TODO: Perhaps reading it three times over using ReadRaster(),
        //        // directly into each of the R, G, B channels, would be more efficient?

        //        // Read the grayscale band
        //        byte[] buffer = new byte[pixelCount];
        //        band.ReadRaster(0, 0, widthPx, heightPx, buffer,
        //            widthPx, heightPx, 0, 0);

        //        // Build ARGB image data
        //        imageData = new byte[pixelCount * 4];
        //        for (int pixel = 0; pixel < pixelCount; pixel++) {
        //            int offset = pixel * 4;
        //            byte gray = buffer[pixel];
        //            imageData[offset] = 255; // A
        //            imageData[offset + 1] = gray; // R
        //            imageData[offset + 2] = gray; // G
        //            imageData[offset + 3] = gray; // B
        //        }
        //    }
        //    else if (bandDataTypes[0] == DataType.GDT_Byte &&
        //        bandColorInterp[0] == ColorInterp.GCI_Undefined)
        //    {
        //        // 8-bit raw data

        //        byte[] buffer = new byte[pixelCount];
        //        band.ReadRaster(0, 0, widthPx, heightPx, buffer,
        //            widthPx, heightPx, 0, 0);

        //        // Build single-band raw data
        //        singleBandData = new float[pixelCount];
        //        for (int pixel = 0; pixel < pixelCount; pixel++)
        //            singleBandData[pixel] = (float)buffer[pixel];
        //    }
        //    else if (bandDataTypes[0] == DataType.GDT_Float32 &&
        //        bandColorInterp[0] == ColorInterp.GCI_GrayIndex)
        //    {
        //        // 32-bit float raw data

        //        singleBandData = new float[pixelCount];
        //        band.ReadRaster(0, 0, widthPx, heightPx, singleBandData,
        //            widthPx, heightPx, 0, 0);
        //    }
        //    else if (bandDataTypes[0] == DataType.GDT_Byte &&
        //        bandColorInterp[0] == ColorInterp.GCI_PaletteIndex)
        //    {
        //        // 8-bit indexed (256 color) RGB

        //        byte[] buffer = new byte[pixelCount];
        //        band.ReadRaster(0, 0, widthPx, heightPx, buffer,
        //            widthPx, heightPx, 0, 0);

        //        // Read color table
        //        ColorTable colorTable = band.GetColorTable();
        //        int colorCount = colorTable.GetCount();
        //        byte[] ctR = new byte[colorCount];
        //        byte[] ctG = new byte[colorCount];
        //        byte[] ctB = new byte[colorCount];
        //        byte[] ctA = new byte[colorCount];
        //        for (int c = 0; c < colorCount; c++) {
        //            ColorEntry entry = colorTable.GetColorEntry(c);
        //            ctR[c] = (byte) Math.Min(entry.c1, (short)255);
        //            ctG[c] = (byte) Math.Min(entry.c2, (short)255);
        //            ctB[c] = (byte) Math.Min(entry.c3, (short)255);
        //            ctA[c] = (byte) Math.Min(entry.c4, (short)255);
        //        }

        //        // Build ARGB image data
        //        imageData = new byte[pixelCount * 4];
        //        for (int pixel = 0; pixel < pixelCount; pixel++) {
        //            int offset = pixel * 4;
        //            byte colorIndex = buffer[pixel];
        //            imageData[offset] = ctA[colorIndex]; // A
        //            imageData[offset + 1] = ctR[colorIndex]; // R
        //            imageData[offset + 2] = ctG[colorIndex]; // G
        //            imageData[offset + 3] = ctB[colorIndex]; // B
                    
        //        }
        //    }
        //    else
        //        throw new NotSupportedException(
        //            "Unsupported raster band configuration.");
        //}
        //else if ((rasterCount == 3 || rasterCount == 4) &&
        //    bandColorInterp.Contains(ColorInterp.GCI_RedBand) &&
        //    bandColorInterp.Contains(ColorInterp.GCI_GreenBand) &&
        //    bandColorInterp.Contains(ColorInterp.GCI_BlueBand) &&
        //    bandDataTypes[0] == DataType.GDT_Byte)
        //{
        //    // 8/8/8-bit RGB image or
        //    // 8/8/8/8-bit RGBA or ARGB image

        //    imageData = new byte[pixelCount * 4];

        //    // Fill with 255 (fully opaque) in case there is no alpha channel
        //    Array.Fill<byte>(imageData, 255);
            
        //    // Process one band (channel) at a time
        //    for (int b = 1; b <= rasterCount; b++)
        //    {
        //        Band band = dataset.GetRasterBand(b);
        //        if (band.DataType != DataType.GDT_Byte)
        //            throw new NotSupportedException(
        //                "Unsupported raster band configuration.");
        //        byte[] buffer = new byte[pixelCount];
        //        band.ReadRaster(0, 0, widthPx, heightPx, buffer,
        //            widthPx, heightPx, 0, 0);
        //        int byteOffset = band.GetRasterColorInterpretation() switch {
        //            ColorInterp.GCI_AlphaBand or
        //            ColorInterp.GCI_Undefined => 0,
        //            ColorInterp.GCI_RedBand => 1,
        //            ColorInterp.GCI_GreenBand => 2,
        //            ColorInterp.GCI_BlueBand => 3,
        //            _ => throw new NotSupportedException(
        //                "Unsupported raster band configuration.")
        //        };
        //        for (int pixel = 0; pixel < pixelCount; pixel++)
        //            imageData[pixel * 4 + byteOffset] = buffer[pixel];
        //    }
        //}
        //else
        //    throw new NotSupportedException(
        //        "Unsupported raster band configuration.");

        //RasterData = new RasterData2(
        //    srs, bounds, widthPx, heightPx, imageData, singleBandData);
    }

    private RasterData2 GetRasterData(Dataset dataset)
    {
        // Get size, projection and bounds
        int widthPx = dataset.RasterXSize;
        int heightPx = dataset.RasterYSize;
        int pixelCount = widthPx * heightPx;
        var affineGeoTransform = new double[6];
        dataset.GetGeoTransform(affineGeoTransform);
        Bounds bounds = Geometry.Bounds.FromCoords([
            new Coord(PixelToGeo(affineGeoTransform, new Coord(0, 0))),
            new Coord(PixelToGeo(affineGeoTransform, new Coord(widthPx - 1, heightPx - 1)))]);
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
                for (int pixel = 0; pixel < pixelCount; pixel++)
                {
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
                for (int pixel = 0; pixel < pixelCount; pixel++)
                {
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
                int byteOffset = band.GetRasterColorInterpretation() switch
                {
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

        return new RasterData2(
            srs, bounds, widthPx, heightPx, imageData, singleBandData);
    }



    public override RasterData2 GetData()
    {
        using Dataset dataset = OpenDataset(Filename);
        return GetRasterData(dataset);
    }

    public override RasterData2 GetData(string destSrs)
    {
        string filename = Filename;
        if (Srs != destSrs)
        {
            // Reproject source data, and use that file
            filename = GdalUtils.Transform(filename, destSrs);
        }
        using Dataset sourceDataset =
            GdalUtils.GetRasterDataset(filename);
        Console.WriteLine(GdalUtils.GetRasterInfo(sourceDataset));
        return GetRasterData(sourceDataset);
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
    private Coord PixelToGeo(double[] affineGeoTransform, Coord c)
        => new Coord(
            affineGeoTransform[0] + affineGeoTransform[1] * c.X + affineGeoTransform[2] * c.Y,
            affineGeoTransform[3] + affineGeoTransform[4] * c.X + affineGeoTransform[5] * c.Y);
}
