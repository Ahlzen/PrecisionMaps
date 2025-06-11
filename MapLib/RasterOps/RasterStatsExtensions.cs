namespace MapLib.RasterOps;

public static class RasterStatsExtensions
{
    /// <summary>
    /// Returns the min and max value in the raster (excluding
    /// no-data values).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The raster has no pixels with valid data (only no-data values).
    /// </exception>
    public static void GetMinMax(
        this SingleBandRasterData source, out float min, out float max)
        => GetMinMax(source.SingleBandData, out min, out max, source.NoDataValue);

    internal static void GetMinMax(float[] data, out float min, out float max, float? noDataValue)
    {
        min = float.MaxValue;
        max = float.MinValue;
        long pixelCount = data.Length;
        if (noDataValue == null)
        {
            for (long i = 0; i < pixelCount; i++)
            {
                float v = data[i];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }
        else
        {
            float n = noDataValue.Value;
            for (long i = 0; i < pixelCount; i++)
            {
                float v = data[i];
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
    public static long GetPixelCount(this SingleBandRasterData source)
    {
        if (source.NoDataValue == null)
        {
            return source.WidthPx * source.HeightPx;
        }
        else
        {
            long totalPixelCount = source.HeightPx * source.WidthPx;
            long dataPixelCount = 0;
            float n = source.NoDataValue.Value;
            for (long i = 0; i < totalPixelCount; i++)
            {
                float v = source.SingleBandData[i];
                if (v != n)
                    dataPixelCount++;
            }
            return dataPixelCount;
        }
    }
}