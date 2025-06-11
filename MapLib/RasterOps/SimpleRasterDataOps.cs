// ReSharper disable CompareOfFloatsByEqualityOperator
using MapLib.ColorSpace;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace MapLib.RasterOps;

public static class SimpleRasterDataOps
{
    /// <summary>
    /// Adjusts (stretches or compresses) the data values to fit
    /// within the range specified, by default [0.0, 1.0].
    /// No-data values are preserved.
    /// </summary>
    /// <remarks>
    /// If max is less than min, the resulting data is inverted.
    /// If all source pixels have the same value, the result becomes
    /// the min value.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The raster has no pixels with valid data (only no-data values).
    /// </exception>
    public static SingleBandRasterData Normalize(
        this SingleBandRasterData source, float min = 0, float max = 1)
    {
        source.GetMinMax(out float sourceMin, out float sourceMax);
        float sourceRange = sourceMax - sourceMin;
        float destRange = max - min;
        float scale = (sourceRange == 0) ? 0 : destRange / sourceRange;
        long pixelCount = source.HeightPx * source.WidthPx;
        float[] normalizedData = new float[pixelCount];
        if (source.NoDataValue == null)
        {
            for (long i = 0; i < pixelCount; i++)
            {
                float v = source.SingleBandData[i];
                normalizedData[i] = min + (scale * (v - sourceMin));
            }
        }
        else
        {
            float n = source.NoDataValue.Value;
            for (long i = 0; i < pixelCount; i++)
            {
                float v = source.SingleBandData[i];
                if (v != n)
                    normalizedData[i] = min + (scale * (v - sourceMin));
                else
                    normalizedData[i] = n;
            }
        }
        PrintMinMax(normalizedData, source.NoDataValue, "Normalized: ");
        return source.CloneWithNewData(normalizedData);
    }

    /// <summary>
    /// Clamps (limits) the values to the range [min, max].
    /// No-data values are preserved.
    /// </summary>
    public static SingleBandRasterData Clamp(
        this SingleBandRasterData source, float min, float max)
    {
        long pixelCount = source.HeightPx * source.WidthPx;
        float[] clampedData = new float[pixelCount];
        if (source.NoDataValue == null)
        {
            for (long i = 0; i < pixelCount; i++)
            {
                float v = source.SingleBandData[i];
                clampedData[i] = Math.Min(max, Math.Max(min, v));
            }
        }
        else
        {
            float n = source.NoDataValue.Value;
            for (long i = 0; i < pixelCount; i++)
            {
                float v = source.SingleBandData[i];
                if (v != n)
                    clampedData[i] = Math.Min(max, Math.Max(min, v));
                else
                    clampedData[i] = n;
            }
        }
        PrintMinMax(clampedData, source.NoDataValue, "Clamped: ");
        return source.CloneWithNewData(clampedData);
    }

    /// <summary>
    /// Clamps (limits) values at the top and bottom of the value range.
    /// </summary>
    /// <remarks>
    /// For example, to clamp values below the bottom 20% and above
    /// the top 30% of the value range, use:
    /// bottomFactor = 0.2, topFactor = 0.3
    /// Then, if the original values were within [0, 100], values
    /// below 20 are clamped to 20, values above 70 are clamped
    /// to 70, so the result falls within [20, 70].
    /// BottomFactor + TopFactor must add up to less than 1.
    /// </remarks>
    public static SingleBandRasterData ClampExtremes(
        this SingleBandRasterData source, float bottomFactor, float topFactor)
    {
        source.GetMinMax(out float srcMin, out float srcMax);
        float destMin = srcMin + (srcMax - srcMin) * bottomFactor;
        float destMax = srcMax - (srcMax - srcMin) * topFactor;
        return Clamp(source, destMin, destMax);
    }

    /// <summary>
    /// Very rudimentary hillshade algorithm. Normalize
    /// and turn into image afterward.
    /// </summary>
    public static SingleBandRasterData Hillshade_Basic(
        this SingleBandRasterData source)
    {
        // TODO: Handle no-data pixels better

        long pixelCount = source.HeightPx * source.WidthPx;
        float[] hillshadeData = new float[pixelCount];

        for (int y = 1; y < source.HeightPx; y++)
        {
            for (int x = 1; x < source.WidthPx; x++)
            {
                float from = source.SingleBandData[(y - 1) * source.WidthPx + (x - 1)];
                float to = source.SingleBandData[y * source.WidthPx + x];
                hillshadeData[y * source.WidthPx + x] = to - from;
            }
            // fill in left column
            hillshadeData[y * source.WidthPx] = hillshadeData[y * source.WidthPx + 1]; 
        }
        // fill in top row
        for (int x = 0; x < source.WidthPx; x++)
            hillshadeData[x] = hillshadeData[x + source.WidthPx];

        PrintMinMax(hillshadeData, source.NoDataValue, "Hillshade: ");
        return source.CloneWithNewData(hillshadeData);
    }

    /// <summary>
    /// Offsets (adds or subtracts a fixed amount to each value)
    /// the raster data. No-data values are preserved.
    /// </summary>
    public static SingleBandRasterData Offset(
        this SingleBandRasterData source, float amount)
    {
        long pixelCount = source.HeightPx * source.WidthPx;
        float[] offsetData = new float[pixelCount];
        if (source.NoDataValue == null)
        {
            for (long i = 0; i < pixelCount; i++)
                offsetData[i] = source.SingleBandData[i] + amount;
        }
        else
        {
            float n = source.NoDataValue.Value;
            for (long i = 0; i < pixelCount; i++)
            {
                float v = source.SingleBandData[i];
                if (v != n)
                    offsetData[i] = v + amount;
                else
                    offsetData[i] = n;
            }
        }
        PrintMinMax(offsetData, source.NoDataValue, "Offset: ");
        return source.CloneWithNewData(offsetData);
    }

    /// <summary>
    /// Scales (multiplies each value by the specified factor)
    /// the raster data. No-data values are preserved.
    /// </summary>
    public static SingleBandRasterData Scale(
        this SingleBandRasterData source, float factor)
    {
        long pixelCount = source.HeightPx * source.WidthPx;
        float[] scaledData = new float[pixelCount];
        if (source.NoDataValue == null)
        {
            for (long i = 0; i < pixelCount; i++)
                scaledData[i] = source.SingleBandData[i] * factor;
        }
        else
        {
            float n = source.NoDataValue.Value;
            for (long i = 0; i < pixelCount; i++)
            {
                float v = source.SingleBandData[i];
                if (v != n)
                    scaledData[i] = v * factor;
                else
                    scaledData[i] = n;
            }
        }
        PrintMinMax(scaledData, source.NoDataValue, "Scaled: ");
        return source.CloneWithNewData(scaledData);
    }

    /// <summary>
    /// Creates image raster data by mapping source values
    /// with the supplied gradient.
    /// </summary>
    /// <remarks>
    /// Useful e.g. for hypsometric tints.
    /// </remarks>
    public static ImageRasterData GradientMap(
        this SingleBandRasterData source, Gradient gradient)
    {
        // TODO: Handle no-data pixels better

        long pixelCount = source.HeightPx * source.WidthPx;
        byte[] imageData = new byte[pixelCount * 4];
        for (long i = 0; i < pixelCount; i++)
        {
            float sourceValue = source.SingleBandData[i];
            (float r, float g, float b) rgb = gradient.ColorAt(sourceValue);
            imageData[i * 4 + 0] = (byte)Math.Clamp(rgb.b * 255, 0, 255); // B
            imageData[i * 4 + 1] = (byte)Math.Clamp(rgb.g * 255, 0, 255); // G
            imageData[i * 4 + 2] = (byte)Math.Clamp(rgb.r * 255, 0, 255); // R
            imageData[i * 4 + 3] = 255; // A
        }
        return new ImageRasterData(source.Srs, source.Bounds, source.WidthPx, source.HeightPx, imageData);
    }

    #region Conversion

    /// <summary>
    /// Returns a grayscale image representing the source
    /// data, optionally normalizing it.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The raster has no pixels with valid data (only no-data values),
    /// and the data has to be normalized.
    /// </exception>
    public static ImageRasterData ToImageRasterData(
        this SingleBandRasterData source, bool normalize = true)
    {
        source = normalize ?
            source.Normalize(0, 255) :
            source.Clamp(0, 255);

        long pixelCount = source.HeightPx * source.WidthPx;
        byte[] imageData = new byte[pixelCount * 4];
        long offset = 0;
        if (source.NoDataValue == null)
        {
            for (long i = 0; i < pixelCount; i++)
            {
                byte v = (byte)(source.SingleBandData[i] + .5f); // add .5 since casting truncates (rounds down)
                imageData[offset++] = v; // B
                imageData[offset++] = v; // G
                imageData[offset++] = v; // R
                imageData[offset++] = 255; // A
            }
        }
        else
        {
            float n = source.NoDataValue.Value;
            for (long i = 0; i < pixelCount; i++)
            {
                float f = source.SingleBandData[i];
                byte b = (byte)(f + .5f); // add .5 since casting truncates (rounds down)
                imageData[offset++] = b; // B
                imageData[offset++] = b; // G
                imageData[offset++] = b; // R
                imageData[offset++] = f != n ? (byte)255 : (byte)0; // A
            }
        }

        return new ImageRasterData(source.Srs,
            source.Bounds, source.WidthPx, source.HeightPx,
            imageData);
    }

    #endregion

    #region Helpers

    /// <remarks>For development only</remarks>
    [Conditional("DEBUG")]
    private static void PrintMinMax(float[] data, float? noDataValue, string? prefix)
    {
        RasterStatsExtensions.GetMinMax(data, out float min, out float max, noDataValue);
        Console.WriteLine($"{prefix}Min: {min}, Max: {max}");
    }

    #endregion
}