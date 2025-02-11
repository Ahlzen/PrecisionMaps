namespace MapLib.Geometry.Helpers;

/// <summary>
/// Implements line simplification using the Visvalingam-Whyatt algorithm.
/// </summary>
/// <remarks>
/// https://en.wikipedia.org/wiki/Visvalingam%E2%80%93Whyatt_algorithm
/// </remarks>
public static class VisvalingamWhyatt
{
    // Naive implementation for testing only - this is inefficient and does not scale!


    public static Coord[] Simplify(Coord[] points,
        int maxPointCount = int.MaxValue,
        double toleranceMaxArea = double.MaxValue)
    {
        List<Coord> coordList = new(points);
        while (Optimize_SingleStep(coordList, maxPointCount, toleranceMaxArea)) {}
        return coordList.ToArray();
    }

    /// <summary>
    /// Attempt to remove a single point.
    /// </summary>
    /// <returns>
    /// True if a point was removed, false otherwise.
    /// </returns>
    private static bool Optimize_SingleStep(
        List<Coord> coords, int maxPointCount, double toleranceMaxArea)
    {
        if (coords.Count < 3) return false;
        if (coords.Count <= maxPointCount) return false;

        int minAreaIndex = 1;
        double minArea = double.MaxValue;
        for (int i = 1; i < coords.Count-1; i++)
        {
            double area = GetArea(coords[i - 1], coords[i], coords[i + 1]);
            if (area < minArea)
            {
                minArea = area;
                minAreaIndex = i;
            }
        }

        if (minArea > toleranceMaxArea)
            return false;

        coords.RemoveAt(minAreaIndex);
        return true;
    }

    private static double GetArea(Coord prev, Coord curr, Coord next)
        => 0.5 * Math.Abs(
            prev.X * curr.Y + curr.X * next.Y + next.X * prev.Y -
            prev.X * next.Y - curr.X * prev.Y - next.X * curr.Y);

}
