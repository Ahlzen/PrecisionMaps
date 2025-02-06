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
    /// Returns a smoothed version of the source geometry.
    /// </summary>
    /// <param name="isClosed">
    /// If true, the first and last points are smoothed as well.
    /// </param>
    /// <param name="iterations">
    /// Fixed number of iterations of smoothing performed.
    /// </param>
    public static Coord[] Smooth(Coord[] source, bool isClosed, int iterations)
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

        //int sourceCoordCount = source.Length;
        //if (isClosed && source[0] == source[^1])
        //    sourceCoordCount--;

        //Coord[] result = isClosed ?
        //        new Coord[2 + (source.Length - 2) * 2] :
        //        new Coord[source.Length * 2];

        Coord[] result = isClosed ?
            new Coord[source.Length * 2 - 1] :
            new Coord[2 + (source.Length - 2) * 2];

        //if (isClosed)
        //{
        //    Coord[] result = new Coord[source.Length * 2 - 1];
        //}
        //else
        //{
        //    Coord[] result = new Coord[2 + (source.Length - 2) * 2];
        //}

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
            // start/endpoint
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
        //}
        //else
        //{
            

        //    // TODO: handle closed polys

        //    result[0] = source[0];
        //    for (int p = 1; p < source.Length - 1; p++)
        //    {
        //        Coord prev = source[p - 1];
        //        Coord curr = source[p];
        //        Coord next = source[p + 1];

        //        Coord p1 = Coord.Lerp(prev, curr, 0.75);
        //        Coord p2 = Coord.Lerp(curr, next, 0.25);

        //        result[p * 2 - 1] = p1;
        //        result[p * 2] = p2;
        //    }
        //    result[^1] = source[^1];

        //    return result;
        //}




    }
}
