using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.RasterOps;

/// <summary>
/// Mechanism to adjust the levels in the specified
/// raster through an input->output function or table.
/// </summary>
/// <remarks>
/// Input values are assumed to be in the range [0,1].
/// Output values are interpolated from a table of
/// input->output values. This data can be supplied
/// either directly or computed from a transfer function.
/// </remarks>
public class LevelAdjustment
{
    public const int DefaultTableLength = 256;

    /// <summary>
    /// Table of input->output mapping data.
    /// </summary>
    private float[] _tableData;

    public LevelAdjustment(float[] tableData)
    {
        _tableData = tableData;
    }

    public LevelAdjustment(
        Func<float, float> transferFunction,
        int tableLength = DefaultTableLength)
    {
        _tableData = new float[tableLength];
        for (int n = 0; n < tableLength; n++)
        {
            float input = n / (tableLength-1.0f);
            float output = transferFunction(input);
            _tableData[n] = output;
        }
    }

    // TODO: Support nodata

    /// <summary>
    /// Returns a clone of the input data with the
    /// adjustment function/table applied.
    /// </summary>
    public float[] Apply(float[] sourceData)
    {
        int tableLength = _tableData.Length;
        int lastTableIndex = tableLength - 1;
        int dataLength = sourceData.Length;
        float lookupScale = (float)_tableData.Length + 0.5f;
        float[] output = new float[dataLength];
        for (long d = 0; d < dataLength; d++)
        {
            float input = sourceData[d];
            float scaled = lookupScale * input;
            int tableIndex = (int)scaled;
            if (tableIndex < 0)
            {
                output[d] = _tableData[0];
            }
            else if (tableIndex >= lastTableIndex)
            {
                output[d] = _tableData[lastTableIndex];
            }
            else
            {
                float remainder = scaled - tableIndex;
                float leftValue = _tableData[tableIndex];
                float rightValue = _tableData[tableIndex + 1];
                output[d] = Lerp(leftValue, rightValue, remainder);
            }
        }
        return output;
    }

    private float Lerp(float a, float b, float distance) =>
        (1 - distance) * a + distance * b;


    // General functions

    /// <summary>
    /// 
    /// </summary>
    /// <param name="scale"></param>
    /// <param name="tableLength"></param>
    /// <returns></returns>
    public static LevelAdjustment Identity(
        int tableLength = DefaultTableLength)
        => new LevelAdjustment((n) => n, tableLength);

    public static LevelAdjustment Scale(
        float scale, int tableLength = DefaultTableLength)
        => new LevelAdjustment((n) => scale * n, tableLength);

    public static LevelAdjustment Quantize(
        int stepCount, int tableLength = DefaultTableLength * 4)
        => new LevelAdjustment(
            (n) => (int)(n * stepCount + 0.5) / (float)stepCount,
            tableLength);


    // Image-adjustment functions

    /// <summary>
    /// Adjust midpoint level up or down without stretching
    /// or clamping the histogram
    /// (e.g. for darkening or lightening an image).
    /// </summary>
    /// <param name="midpointLevel">
    /// Expected output value when input is 0.5 (midpoint).
    /// </param>
    /// <param name="tableLength"></param>
    /// <returns></returns>
    /// <remarks>
    /// Expected input values within [0,1] and produces
    /// output values in [0,1].
    /// MidpointLevel = 0.5 -> no change.
    /// MidpointLevel < 0.5 -> smaller values (darken image).
    /// MidpointLevel > 0.5 -> larger values (brighter image).
    /// </remarks>
    public static LevelAdjustment AdjustMidpoint(
        float midpointLevel, int tableLength = DefaultTableLength)
    {
        double exponent = Math.Log(midpointLevel) / Math.Log(0.5);
        return new LevelAdjustment(
            (n) => (float)Math.Pow(n, exponent),
            tableLength);
    }

    /// <summary>
    /// Scale input levels up or down, around a specified midpoint,
    /// and clamp the result to [0,1].
    /// (e.g. for darkening or lightening an image).
    /// </summary>
    /// <remarks>
    /// Using midpoint=0.0 adjusts image lightness.
    /// Using midpoint=0.5 adjusts image contrast.
    /// </remarks>
    public static LevelAdjustment ScaleAndClamp(
        float midpoint, float scale, int tableLength = DefaultTableLength)
        => new LevelAdjustment((n) => Math.Clamp(
            midpoint + (n - midpoint) * scale, 0.0f, 1.0f), tableLength);


}
