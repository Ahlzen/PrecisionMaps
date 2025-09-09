// ReSharper disable CompareOfFloatsByEqualityOperator
using MapLib.ColorSpace;
using System.Diagnostics;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Versioning;

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
        RasterDataOpsHelpers.PrintDebugInfo(normalizedData, source.NoDataValue, "Normalized: ");
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
        RasterDataOpsHelpers.PrintDebugInfo(clampedData, source.NoDataValue, "Clamped: ");
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
        RasterDataOpsHelpers.PrintDebugInfo(offsetData, source.NoDataValue, "Offset: ");
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
        RasterDataOpsHelpers.PrintDebugInfo(scaledData, source.NoDataValue, "Scaled: ");
        return source.CloneWithNewData(scaledData);
    }

    /// <summary>
    /// Generate a "stepped" version of the source data with the
    /// specified step interval (and an optional offset).
    /// Values are always rounded down.
    /// </summary>
    /// <remarks>
    /// For example, step 10 offset 3 would generate steps at
    /// -7 for [-7,3)
    ///  3 for [3,13)
    ///  13 for [13,23)
    /// etc.
    /// useful e.g. to generate discrete hypsometric tints.
    /// </remarks>
    public static SingleBandRasterData GenerateSteps(
        this SingleBandRasterData source,
        float stepInterval, float offset = 0)
    {
        long pixelCount = source.HeightPx * source.WidthPx;
        float[] steppedData = new float[pixelCount];
        float nodataValue = source.NoDataValue ?? float.NaN;
        for (long i = 0; i < pixelCount; i++)
        {
            float value = source.SingleBandData[i];
            if (value == nodataValue) {
                steppedData[i] = nodataValue;
            }
            else {
                value -= offset;
                value /= stepInterval;
                value = (float)Math.Floor(value);
                value *= stepInterval;
                value += offset;
                steppedData[i] = value;
            }
        }
        RasterDataOpsHelpers.PrintDebugInfo(steppedData, source.NoDataValue, "Stepped: ");
        return source.CloneWithNewData(steppedData);
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
}