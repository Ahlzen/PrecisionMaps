namespace MapLib.Geometry;

/// <summary>
/// A 2D bounding box. Immutable.
/// </summary>
public struct Bounds : IEquatable<Bounds>
{
    public double XMin { get; }
    public double XMax { get; }
    public double YMin { get; }
    public double YMax { get; }

    public Bounds(double xmin, double xmax, double ymin, double ymax)
    {
        XMin = xmin; XMax = xmax; YMin = ymin; YMax = ymax;
    }
    public Bounds(Bounds b)
    {
        XMin = b.XMin; XMax = b.XMax; YMin = b.YMin; YMax = b.YMax;
    }
    public Bounds(Coord c1, Coord c2)
    {
        XMin = Math.Min(c1.X, c2.X);
        XMax = Math.Max(c1.X, c2.X);
        YMin = Math.Min(c1.Y, c2.Y);
        YMax = Math.Max(c1.Y, c2.Y);
    }

    public double Width => XMax - XMin;
    public double Height => YMax - YMin;
    public Coord BottomLeft => new(XMin, YMin);
    public Coord BottomRight => new(XMax, YMin);
    public Coord TopLeft => new(XMin, YMax);
    public Coord TopRight => new(XMax, YMax);
    public Coord Center => new(
        (XMax + XMin) * 0.5, (YMax + YMin) * 0.5);

    public Polygon AsPolygon() => new Polygon(
        [(XMin, YMin), (XMin, YMax), (XMax, YMax), (XMax, YMin), (XMin, YMin)], null);

    public static Bounds FromCoords(IEnumerable<Coord> srcCoords)
    {
        if (!srcCoords.Any()) throw new InvalidOperationException();
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        foreach (var coord in srcCoords)
        {
            xMin = Math.Min(xMin, coord.X);
            xMax = Math.Max(xMax, coord.X);
            yMin = Math.Min(yMin, coord.Y);
            yMax = Math.Max(yMax, coord.Y);
        }
        return new Bounds(xMin, xMax, yMin, yMax);
    }

    public static Bounds FromBounds(IEnumerable<Bounds> srcBounds)
    {
        if (!srcBounds.Any()) throw new InvalidOperationException();
        Bounds bounds = srcBounds.First();
        foreach (var bound in srcBounds)
            bounds += bound;
        return bounds;
    }

    public static Bounds operator +(Bounds b1, Bounds b2)
    {
        return new Bounds(
            Math.Min(b1.XMin, b2.XMin),
            Math.Max(b1.XMax, b2.XMax),
            Math.Min(b1.YMin, b2.YMin),
            Math.Max(b1.YMax, b2.YMax));
    }

    public static Bounds? operator +(Bounds? b1, Bounds? b2)
    {
        if (b1 == null && b2 == null) return null;
        if (b1 == null) return b2;
        if (b2 == null) return b1;
        return b1.Value + b2.Value;
    }

    /// <summary>
    /// Returns the intersection (overlapping area) of this
    /// and the specified other Bounds. Returns null if no overlap,
    /// including when sharing a corner or side.
    /// </summary>
    public Bounds? Intersection(Bounds other)
    {
        if (XMax <= other.XMin ||
            XMin >= other.XMax ||
            YMax <= other.YMin ||
            YMin >= other.YMax)
            return null;
        return new Bounds(
            Math.Max(XMin, other.XMin),
            Math.Min(XMax, other.XMax),
            Math.Max(YMin, other.YMin),
            Math.Min(YMax, other.YMax));
    }

    public Bounds ResizeAndCenterX(double newWidth)
    {
        double extraWidth = newWidth - Width;
        return new Bounds(
            XMin - extraWidth / 2, XMax + extraWidth / 2,
            YMin, YMax);
    }

    public Bounds ResizeAndCenterY(double newHeight)
    {
        double extraHeight = newHeight - Height;
        return new Bounds(
            XMin, XMax,
            YMin - extraHeight / 2, YMax + extraHeight / 2);
    }

    /// <summary>
    /// Returns the size, defined as the maximum of width and height.
    /// </summary>
    /// <remarks>
    /// Useful to get a rough idea of an feature's size, especially since
    /// many algorithms require some threshold that's relative to the
    /// feature's dimensions.
    /// </remarks>
    public double Size => Math.Max(Width, Height);

    public override string ToString() =>
        $"X: ({XMin}, {XMax}) Y: ({YMin}, {YMax})";

    #region IEquatable

    public static bool operator ==(Bounds b1, Bounds b2) => b1.Equals(b2);
    public static bool operator !=(Bounds b1, Bounds b2) => !(b1 == b2);
    public bool Equals(Bounds other) =>
        XMin == other.XMin &&
        XMax == other.XMax &&
        YMin == other.YMin &&
        YMax == other.YMax;

    #endregion
}