using System.Diagnostics.Contracts;

namespace MapLib.Geometry;

public static class CoordExtensions
{
    #region Transformations

    public static Coord Transform(this Coord source, double scale, double offsetX, double offsetY)
        => Transform(source, scale, scale, offsetX, offsetY);
    public static Coord Transform(this Coord source, double scaleX, double scaleY,
        double offsetX, double offsetY)
        => new(
            source.X * scaleX + offsetX,
            source.Y * scaleY + offsetY);

    public static Coord[] Transform(this Coord[] source, double scale, double offsetX, double offsetY)
        => Transform(source, scale, scale, offsetX, offsetY);
    public static Coord[] Transform(this Coord[] source,
        double scaleX, double scaleY, double offsetX, double offsetY)
    {
        var dest = new Coord[source.Length];
        for (int i = 0; i < source.Length; i++)
            dest[i] = Transform(source[i], scaleX, scaleY, offsetX, offsetY);
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

    #region Line calculations

    /// <summary>
    /// Calculates the length of the line (in the units of the line's
    /// coordinates).
    /// </summary>
    public static double GetLength(this Coord[] coords)
    {
        if (coords.Length < 2)
            return 0;
        double totalLength = 0;
        Coord prev = coords[0];
        for (int i = 1; i < coords.Length; i++)
        {
            Coord next = coords[i];
            totalLength += prev.DistanceTo(next);
            prev = next;
        }
        return totalLength;
    }

    /// <summary>
    /// Returns the coordinates at a point along the line between
    /// the start and end. Relative distance is [0,1], where
    /// 0 is the start and 1 is the end.
    /// </summary>
    public static Coord GetPointAlongLine(this Coord[] coords, double relativeDistance)
    {
        if (coords.Length == 0)
            throw new InvalidOperationException("Line has no points");
        if (coords.Length == 1)
            return coords[0];
        double totalLength = coords.GetLength();
        relativeDistance = Math.Clamp(relativeDistance, 0, 1);
        double absoluteDistance = totalLength * relativeDistance;

        Coord prev = coords[0];
        double prevDistance = 0;
        for (int i = 1; i < coords.Length; i++)
        {
            Coord next = coords[i];
            double segmentLength = prev.DistanceTo(next);
            if ((prevDistance + segmentLength) <= absoluteDistance)
            {
                // The point is along this segment
                double remainder = absoluteDistance - prevDistance;
                double t = remainder / segmentLength;
                return Coord.Lerp(prev, next, t);
            }
            else
            {
                prevDistance += segmentLength;
                prev = next;
            }
        }
        return coords[^1];
    }
    public static Coord GetMidpoint(this Coord[] coords)
        => coords.GetPointAlongLine(0.5);

    #endregion
}