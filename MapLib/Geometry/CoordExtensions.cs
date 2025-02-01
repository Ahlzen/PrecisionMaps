namespace MapLib.Geometry;

public static class CoordExtensions
{
    #region Transformations

    public static Coord Transform(this Coord source, double scale, double offsetX, double offsetY)
        => new(
            source.X * scale + offsetX,
            source.Y * scale + offsetY);

    public static Coord[] Transform(this Coord[] source, double scale, double offsetX, double offsetY)
    {
        var dest = new Coord[source.Length];
        for (int i = 0; i < source.Length; i++)
            dest[i] = Transform(source[i], scale, offsetX, offsetY);
        return dest;
    }

    /// <summary>
    /// Returns the area of the polygon (positive if CCW, negative if CW).
    /// </summary>
    /// <remarks>
    /// This algorithm works for both convex and concave
    /// polygons, as well as overlapping polygons.
    /// Based on https://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order/1180256#1180256
    /// </remarks>
    public static double CalculatePolygonArea(this Coord[] coords)
    {
        double sum = 0;
        for (int n = 0; n < coords.Length - 1; n++)
            sum += (coords[n + 1].X - coords[n].X) * (coords[n + 1].Y + coords[n].Y);
        double area = sum / 2.0;
        return -area; // invert since outer is CCW
    }

    /// <summary>
    /// Returns the winding of the polygon (true if CW, false if CCW).
    /// </summary>
    public static bool IsClockwise(this Coord[] coords) => CalculatePolygonArea(coords) < 0;

    /// <summary>
    /// Returns the winding of the polygon (false if CW, true if CCW).
    /// </summary>
    public static bool IsCounterClockwise(this Coord[] coords) => CalculatePolygonArea(coords) > 0;

    #endregion
}