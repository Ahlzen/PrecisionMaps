namespace MapLib.Geometry;

/// <summary>
/// A 2D point. Immutable.
/// </summary>
public class Point : Shape
{
    public Coord Coord { get; }

    public Point(Coord coord)
    {
        Coord = coord;
    }
    public Point(double x, double y)
    {
        Coord = new Coord(x, y);
    }

    public static implicit operator Coord(Point p) => p.Coord;
    public static implicit operator Point(Coord c) => new Point(c);

    public Point Transform(Func<Coord, Coord> transformation)
        => new Point(transformation(Coord));

    public override Bounds GetBounds()
        => new Bounds(Coord.X, Coord.X, Coord.Y, Coord.Y);

    public override Coord GetCenter() => Coord;

    public double DistanceTo(Coord c)
        => Coord.Distance(Coord, c);

    public override string ToString()
        => "Coord" + Coord.ToString();

    #region Operations

    public override MultiPolygon Buffer(double radius)
    {
        return CreateBufferPolygon(Coord, radius)
            .AsMultiPolygon();
    }

    #endregion

    public static Polygon CreateBufferPolygon(
        Coord c, double radius)
        => Polygon.CreateCircle(c, radius,
            // TODO
            DEFAULT_POINTS_PER_REVOLUTION);
}