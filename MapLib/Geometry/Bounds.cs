namespace MapLib.Geometry;

/// <summary>
/// A 2D bounding box. Immutable.
/// </summary>
public struct Bounds
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

    //public static Bounds? FromBounds(IEnumerable<Bounds?> srcBounds)
    //{
    //    if (!srcBounds.Any()) return null;
    //    Bounds? bounds = srcBounds.First();
    //    foreach (var bound in srcBounds)
    //        bounds += bound;
    //    return bounds;
    //}

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

    public override string ToString() =>
        $"X: ({XMin}, {XMax}) Y: ({YMin}, {YMax})";
}