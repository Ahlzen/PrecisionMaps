using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.RasterOps;

public static class RasterDataOpsHelpers
{
    /// <remarks>For development only</remarks>
    [Conditional("DEBUG")]
    internal static void PrintDebugInfo(float[] data, float? noDataValue, string? prefix)
    {
        RasterStatsExtensions.GetMinMax(data, out float min, out float max, noDataValue);
        Console.WriteLine($"{prefix}Min: {min}, Max: {max}");
    }
}
