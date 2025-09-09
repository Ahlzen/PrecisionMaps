using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.RasterOps;

public static class Hillshade
{
    /// <summary>
    /// Very rudimentary hillshade algorithm.
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

        RasterDataOpsHelpers.PrintDebugInfo(hillshadeData, source.NoDataValue, "Hillshade: ");
        return source.CloneWithNewData(hillshadeData);
    }
}
