// ReSharper disable CompareOfFloatsByEqualityOperator
using MapLib.Render;

namespace MapLib.RasterOps;

public static class SimpleRasterDataOps
{
    /// <summary>
    /// Returns the min and max value in the raster (excluding
    /// no-data values).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The raster has no pixels with valid data (only no-data values).
    /// </exception>
    public static void GetMinMax(
        this SingleBandRasterData data, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;

        long pixelCount = data.HeightPx * data.WidthPx;

        if (data.NoDataValue == null)
        {
            for (long i = 0; i < pixelCount; i++)
            {
                float v = data.SingleBandData[i];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }
        else
        {
            float n = data.NoDataValue.Value;
            for (long i = 0; i < pixelCount; i++)
            {
                float v = data.SingleBandData[i];
                if (v != n)
                {
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }
        }

        // Special case, where there are no pixels with data
        if (min == float.MaxValue && max == float.MinValue)
            throw new InvalidOperationException("No data");
    }

    /// <summary>
    /// Returns the number of pixels in the raster (excluding
    /// no-data values)
    /// </summary>
    public static long GetPixelCount(this SingleBandRasterData data)
    {
        if (data.NoDataValue == null)
        {
            return data.WidthPx * data.HeightPx;
        }
        else
        {
            long totalPixelCount = data.HeightPx * data.WidthPx;
            long dataPixelCount = 0;
            float n = data.NoDataValue.Value;
            for (long i = 0; i < totalPixelCount; i++)
            {
                float v = data.SingleBandData[i];
                if (v != n)
                    dataPixelCount++;
            }
            return dataPixelCount;
        }
    }

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
    public static SingleBandRasterData GetNormalized(
        this SingleBandRasterData source, float min = 0, float max = 1)
    {
        GetMinMax(source, out float sourceMin, out float sourceMax);
        float sourceRange = sourceMax - sourceMin;
        float destRange = max - min;
        float offset = min - sourceMin;
        float scale = (sourceRange == 0) ? 0 : destRange / sourceRange;
        long pixelCount = source.HeightPx * source.WidthPx;
        float[] normalizedData = new float[pixelCount];
        if (source.NoDataValue == null)
        {
            for (long i = 0; i < pixelCount; i++)
            {
                float v = source.SingleBandData[i];
                normalizedData[i] = offset + v * scale;
            }
        }
        else
        {
            float n = source.NoDataValue.Value;
            for (long i = 0; i < pixelCount; i++)
            {
                float v = source.SingleBandData[i];
                if (v != n)
                    normalizedData[i] = offset + v * scale;
                else
                    normalizedData[i] = n;
            }
        }
        return source.CloneWithNewData(normalizedData);
    }

    /// <summary>
    /// Clamps (limits) the values to the range [min, max].
    /// No-data values are preserved.
    /// </summary>
    public static SingleBandRasterData GetClamped(
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
        return source.CloneWithNewData(clampedData);
    }

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
            source.GetNormalized(0, 255) :
            source.GetClamped(0, 255);

        long pixelCount = source.HeightPx * source.WidthPx;
        byte[] imageData = new byte[pixelCount * 4];
        long offset = 0;
        if (source.NoDataValue == null)
        {
            for (long i = 0; i < pixelCount; i++)
            {
                byte v = (byte)source.SingleBandData[i];
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
                byte b = (byte)f;
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

    /// <summary>
    /// Very rudimentary hillshade algorithm. Normalize
    /// and turn into image afterward.
    /// </summary>
    public static SingleBandRasterData GetHillshade_Basic(
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

        return source.CloneWithNewData(hillshadeData);
    }


    // TODO: Hypsometric tints (basic)
    // TODO: Contour lines (basic)
    // TODO: Levels/contrast/stretch/histogram etc
    // TODO: Low pass filtering (smoothing)
}