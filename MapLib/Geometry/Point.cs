using MapLib.GdalSupport;

namespace MapLib.Geometry;

/// <summary>
/// A 2D point. Immutable.
/// </summary>
public class Point : Shape
{
    public Coord Coord { get; }

    public Point(Coord coord, TagList? tags) : base(tags)
    {
        Coord = coord;
    }
    public Point(double x, double y, TagList? tags) : base(tags)
    {
        Coord = new Coord(x, y);
    }

    public static implicit operator Coord(Point p) => p.Coord;
    public static implicit operator Point(Coord c) => new Point(c, null);

    public override Bounds GetBounds()
        => new Bounds(Coord.X, Coord.X, Coord.Y, Coord.Y);

    public override Coord GetCenter() => Coord;

    public double DistanceTo(Coord c)
        => Coord.Distance(Coord, c);

    public override string ToString()
        => "Coord" + Coord.ToString();

    #region Transformations

    public Point Transform(Func<Coord, Coord> transformation)
        => new Point(transformation(Coord), Tags);

    /// <returns>
    /// Returns the transformed point at (X*scale+offsetX, Y*scale+offsetY)
    /// </returns>
    public Point Transform(double scale, double offsetX, double offsetY)
        => Transform(scale, scale, offsetX, offsetY);

    /// <returns>
    /// Returns the transformed point at (X*scaleX+offsetX, Y*scaleY+offsetY)
    /// </returns>
    public Point Transform(double scaleX, double scaleY, double offsetX, double offsetY)
        => new(Coord.X * scaleX + offsetX, Coord.Y * scaleY + offsetY, Tags);

    public Point Transform(Transformer transformer)
        => new Point(transformer.Transform(Coord), Tags);

    #endregion

    #region Operations

    public override MultiPolygon Buffer(double radius)
        => new MultiPolygon([CreateBuffer(Coord, radius)], Tags);

    #endregion

    public static Coord[] CreateBuffer(Coord c, double radius)
        => Coord.CreateCircle(c, radius, DEFAULT_POINTS_PER_REVOLUTION);
}