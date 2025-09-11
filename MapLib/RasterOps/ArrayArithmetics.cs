using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.RasterOps;

/// <summary>
/// Arithmetic operations on arrays of floats.
/// </summary>
/// <remarks>
/// Arrays are not mutated, all operations return new arrays.
/// </remarks>
public static class ArrayArithmetics
{
    #region Arithmetic operations with constant

    /// <summary>
    /// Adds a constant c to elements in a and returns the result.
    /// </summary>
    public static float[] Add(float[] a, float c)
    {
        var result = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
            result[i] = a[i] + c;
        return result;
    }
    public static float[] Subtract(float[] a, float c) => Add(a, -c);

    /// <summary>
    /// Multiplies elements in a by a constant c and returns the result.
    /// </summary>
    public static float[] Multiply(float[] a, float c)
    {
        var result = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
            result[i] = a[i] * c;
        return result;
    }
    public static float[] Negate(float[] a) => Multiply(a, -1);

    #endregion

    #region Arithmetic operations with two arrays

    /// <summary>
    /// Adds arrays a and be element-wise and returns the result (a+b).
    /// </summary>
    /// <remarks>
    /// Arrays must be of the same length.
    /// </remarks>
    public static float[] Add(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Arrays must be of the same length");

        var result = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
            result[i] = a[i] + b[i];
        return result;
    }

    /// <summary>
    /// Subtracts array b from a element-wise and returns the result (a-b).
    /// </summary>
    /// <remarks>
    /// Arrays must be of the same length.
    /// </remarks>
    public static float[] Subtract(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Arrays must be of the same length");

        var result = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
            result[i] = a[i] - b[i];
        return result;
    }

    /// <summary>
    /// Multiplies arrays a and b element-wise and returns the result (a*b).
    /// </summary>
    /// <remarks>
    /// Elements must be of the same length.
    /// </remarks>
    public static float[] Multiply(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Arrays must be of the same length");

        var result = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
            result[i] = a[i] * b[i];
        return result;
    }

    #endregion
}
