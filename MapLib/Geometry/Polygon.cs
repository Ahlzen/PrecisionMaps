using MapLib.GdalSupport;
using MapLib.Geometry.Helpers;

namespace MapLib.Geometry;

/// <summary>
/// 2D polygon. Immutable.
/// </summary>
/// <remarks>
/// Simple linear ring with no holes. If a polygon has holes, it
/// is considered a MultiPolygon in our data model.
/// </remarks>
public class Polygon : Line
{
    public Polygon(Coord[] coords, TagList? tags) : base(coords, tags) {
        Validate();
    }

    public Polygon(IEnumerable<Coord> coords, TagList? tags) : base(coords, tags) {
        Validate();
    }

    private void Validate()
    {
        if (Coords.Length < 3)
            throw new ArgumentException("A polygon requires at least three coordinates", "coords");
        if (Coords[0] != Coords[Coords.Length - 1])
            throw new ArgumentException("First and last points must be the same", "coords");
    }

    public MultiPolygon AsMultiPolygon()
        => new MultiPolygon(this, Tags);

    #region Transformations

    public override Polygon Transform(Func<Coord, Coord> transformation)
        => new Polygon(Coords.Select(transformation), Tags);

    /// <returns>
    /// Returns the polygon transformed as (X*scale+offsetX, Y*scale+offsetY)
    /// </returns>
    public override Polygon Transform(double scale, double offsetX, double offsetY)
        => Transform(scale, scale, offsetX, offsetY);

    /// <returns>
    /// Returns the polygon transformed as (X*scaleX+offsetX, Y*scaleY+offsetY)
    /// </returns>
    public override Polygon Transform(double scaleX, double scaleY, double offsetX, double offsetY)
        => new(Coords.Transform(scaleX, scaleY, offsetX, offsetY), Tags);

    public new Polygon Transform(Transformer transformer)
        => new Polygon(transformer.Transform(Coords), Tags);

    #endregion

    public override Polygon Reverse()
        => new Polygon(Coords.Reverse(), Tags);

    public new MultiPolygon Offset(double d)
        => AsMultiPolygon().Offset(d);

    public new Polygon Smooth_Chaikin(int iterations)
        => new Polygon(Chaikin.Smooth_Fixed(Coords, true, iterations), Tags);

    public new Polygon Smooth_ChaikinAdaptive(double maxAngleDegrees)
        => new Polygon(Chaikin.Smooth_Adaptive(Coords, true, maxAngleDegrees), Tags);

    #region Operations and properties

    public double Area {
        get {
            if (_area == null) {
                _area = Coords.CalculatePolygonArea();
            }
            return _area.Value;
        }
    }
    private double? _area; // cached value

    /// <summary>
    /// Returns the winding of the polygon (true if CW, false if CCW).
    /// </summary>
    public bool IsClockwise => Area < 0;

    /// <summary>
    /// Returns the winding of the polygon (false if CW, true if CCW).
    /// </summary>
    public bool IsCounterClockwise => Area > 0;

    #endregion

    #region Static primitive factory methods

    public static Polygon CreateCircle(Coord center, double radius,
        TagList? tags,
        int pointsPerRevolution = DEFAULT_POINTS_PER_REVOLUTION,
        bool outer = true)
    {
        Coord[] coords = Coord.CreateCircle(center, radius, pointsPerRevolution, outer);
        return new Polygon(coords, tags);
    }

    #endregion
}