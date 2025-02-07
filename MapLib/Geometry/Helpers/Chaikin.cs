using System.Diagnostics;

namespace MapLib.Geometry.Helpers;

/// <summary>
/// Implements line/polygon smoothing using Chaikin's algorithm.
/// </summary>
/// <remarks>
/// https://www.educative.io/answers/what-is-chaikins-algorithm
/// </remarks>
public static class Chaikin
{
    /// <summary>
    /// Returns a version of the source line/ring, with each vertex
    /// smoothed (subdivided) a fixed number of times.
    /// </summary>
    /// <param name="isClosed">
    /// If true, the first and last points are smoothed as well.
    /// </param>
    /// <param name="iterations">
    /// Fixed number of iterations of smoothing performed.
    /// </param>
    public static Coord[] Smooth_Fixed(Coord[] source, bool isClosed, int iterations)
    {
        Coord[] result = source;
        for (int i= 0; i < iterations; i++)
            result = Smooth_Iteration(result, isClosed);
        return result;
    }
    private static Coord[] Smooth_Iteration(Coord[] source, bool isClosed)
    {
        if (source.Length < 3) return source;
        if (isClosed && source[0] != source[^1])
            throw new InvalidOperationException(
                "First and last point must be the same for closed paths.");

        Coord[] result = isClosed ?
            new Coord[source.Length * 2 - 1] :
            new Coord[2 + (source.Length - 2) * 2];

        result[0] = source[0];
        for (int p = 1; p < source.Length - 1; p++)
        {
            Subdivide(source[p - 1], source[p], source[p + 1], out Coord p1, out Coord p2);
            result[p * 2 - 1] = p1;
            result[p * 2] = p2;
        }
        result[^1] = source[^1];

        if (isClosed)
        {
            // Smooth start/end of ring
            Subdivide(source[^2], source[0], source[1], out Coord p1, out Coord p2);
            result[0] = p2;
            result[^1] = p2;
            result[^2] = p1;
        }

        return result;
    }


    /// <summary>
    /// Returns a version of the source line/ring, with each vertex
    /// smoothed (subdivided) until no angles exceed the specified threshold.
    /// </summary>
    /// <param name="isClosed">
    /// If true, the first and last points are smoothed as well.
    /// </param>
    /// <param name="maxAngleDegrees">
    /// Max bend angle allowed at each vertex in the final result.
    /// </param>
    public static Coord[] Smooth_Adaptive(Coord[] sourceArray, bool isClosed,
        double maxAngleDegrees)
    {
        if (sourceArray.Length < 3) return sourceArray;
        if (isClosed && sourceArray[0] != sourceArray[^1])
            throw new InvalidOperationException(
                "First and last point must be the same for closed paths.");

        double maxAngleRadians = maxAngleDegrees / (180 / Math.PI);

        // Since we're smoothing both sides, the effective max angle
        // is double that of the argument
        maxAngleRadians *= 2;

        // NOTE: This allocates a list (that may be extended) for
        // each iteration. Can probably be made more efficient.

        List<Coord> dst, src = sourceArray.ToList();

        while (true)
        {
            bool anyPointsSmoothed = false;

            Debug.WriteLine($"Starting new iteration, {src.Count} source points.");

            dst = new List<Coord>(src.Count);
            dst.Add(src[0]);
            for (int p = 1; p < src.Count - 1; p++)
            {
                double angleRadians = Angle(src[p-1], src[p], src[p+1]);
                if (angleRadians > maxAngleRadians)
                {
                    Subdivide(src[p-1], src[p], src[p+1], out Coord p1, out Coord p2);
                    dst.Add(p1);
                    dst.Add(p2);
                    anyPointsSmoothed = true;
                }
                else
                {
                    dst.Add(src[p]); // use coord as is
                }
            }
            dst.Add(src[^1]);

            if (isClosed)
            {
                // Smooth start/end of ring
                double angleRadians = Angle(src[^2], src[0], src[1]);
                if (angleRadians > maxAngleRadians)
                {
                    Subdivide(src[^2], src[0], src[1], out Coord p1, out Coord p2);
                    dst[0] = p2;
                    dst[^1] = p2;
                    dst[^2] = p1;
                    anyPointsSmoothed = true;
                }
            }
            if (!anyPointsSmoothed) break; // we're done!
            src = dst;
        }
        return dst.ToArray();
    }


    // Helpers

    private static void Subdivide(Coord prev, Coord curr, Coord next, out Coord p1, out Coord p2)
    {
        p1 = Coord.Lerp(prev, curr, 0.75);
        p2 = Coord.Lerp(curr, next, 0.25);
    }

    /// <returns>
    /// Angle of deflection in radians at point curr, along a line
    /// (prev, curr, next).
    /// </returns>
    public static double Angle(Coord prev, Coord curr, Coord next)
    {
        Coord d1 = curr - prev;
        Coord d2 = next - curr;
        return Coord.Angle(d1, d2);
    }
}
