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
    public static Coord[] Smooth_Adaptive(Coord[] source, bool isClosed,
        double maxAngleDegrees)
    {
        if (source.Length < 3) return source;
        if (isClosed && source[0] != source[^1])
            throw new InvalidOperationException(
                "First and last point must be the same for closed paths.");

        double maxAngleRadians = maxAngleDegrees / (180 / Math.PI);

        // Since we're smoothing both sides, the effective max angle
        // is double that of the argument
        maxAngleRadians *= 2;

        // This might not be a very efficient implementation

        List<Coord> dst, src = source.ToList();

        while (true)
        {
            bool anyPointsSmoothed = false;

            Debug.WriteLine($"Starting new iteration, {src.Count} source points.");

            dst = new List<Coord>(src.Count);
            dst.Add(src[0]);
            for (int p = 1; p < src.Count - 1; p++)
            {
                Coord prev = src[p - 1];
                Coord curr = src[p];
                Coord next = src[p + 1];
                
                Coord d1 = curr - prev;
                Coord d2 = next - curr;
                
                double angleRadians = Math.Acos(
                    (d1*d2) / (Coord.Length(d1) * Coord.Length(d2)));

                if (angleRadians > maxAngleRadians)
                {
                    Coord p1 = Coord.Lerp(prev, curr, 0.75);
                    Coord p2 = Coord.Lerp(curr, next, 0.25);
                    dst.Add(p1);
                    dst.Add(p2);
                    anyPointsSmoothed = true;
                }
                else
                {
                    // use coord as is
                    dst.Add(src[p]);
                }
            }
            dst.Add(src[^1]);

            if (isClosed)
            {
                // Smooth start/end of ring
                Coord prevS = src[^2];
                Coord currS = src[0];
                Coord nextS = src[1];

                Coord d1 = currS - prevS;
                Coord d2 = nextS - currS;
                double angleRadians = Math.Acos(
                    (d1 * d2) / (Coord.Length(d1) * Coord.Length(d2)));

                if (angleRadians > maxAngleRadians)
                {
                    Coord p1S = Coord.Lerp(prevS, currS, 0.75);
                    Coord p2S = Coord.Lerp(currS, nextS, 0.25);
                    dst[0] = p2S;
                    dst[^1] = p2S;
                    dst[^2] = p1S;
                    anyPointsSmoothed = true;
                }
            }

            // When we go through a full iteration without
            // any smoothing, we're done:
            if (!anyPointsSmoothed) break;

            src = dst;
        }
        return dst.ToArray();
    }


    // Helpers

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
            Coord prev = source[p - 1];
            Coord curr = source[p];
            Coord next = source[p + 1];
            Coord p1 = Coord.Lerp(prev, curr, 0.75);
            Coord p2 = Coord.Lerp(curr, next, 0.25);
            result[p * 2 - 1] = p1;
            result[p * 2] = p2;
        }
        result[^1] = source[^1];

        if (isClosed)
        {
            // Smooth start/end of ring
            Coord prevS = source[^2];
            Coord currS = source[0];
            Coord nextS = source[1];
            Coord p1S = Coord.Lerp(prevS, currS, 0.75);
            Coord p2S = Coord.Lerp(currS, nextS, 0.25);
            result[0] = p2S;
            result[^1] = p2S;
            result[^2] = p1S;
        }

        return result;
    }
}
