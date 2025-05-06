using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.RasterOps;

public static class SimpleRasterDataOps
{
    public static void ComputeMinMax(
        SingleBandRasterData data, out float min, out float max)
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

    public static long ComputePixelCount(SingleBandRasterData data)
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
}