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
    public Polygon(Coord[] coords, TagDictionary? tags) : base(coords, tags) {
        Validate();
    }

    public Polygon(IEnumerable<Coord> coords, TagDictionary? tags) : base(coords, tags) {
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

    public override Polygon Transform(Func<Coord, Coord> transformation)
        => new Polygon(Coords.Select(transformation), Tags);

    /// <returns>
    /// Returns the polygon transformed as (X*scale+offsetX, Y*scale+offsetY)
    /// </returns>
    public override Polygon Transform(double scale, double offsetX, double offsetY)
        => new(Coords.Transform(scale, offsetX, offsetY), Tags);

    public override Polygon Reverse()
        => new Polygon(Coords.Reverse(), Tags);

    public new MultiPolygon Offset(double d)
        => AsMultiPolygon().Offset(d);

    #region Operations and properties

    /// <summary>
    /// Returns the area of the polygon (positive if CCW, negative if CW).
    /// </summary>
    /// <remarks>
    /// This algorithm works for both convex and concave
    /// polygons, as well as overlapping polygons.
    /// Based on https://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order/1180256#1180256
    /// </remarks>
    public double Area {
        get {
            if (_area == null) {
                double sum = 0;
                for (int n = 0; n < Coords.Length - 1; n++)
                    sum += (Coords[n + 1].X - Coords[n].X) * (Coords[n + 1].Y + Coords[n].Y);
                _area = sum / 2.0;
            }
            return -_area.Value; // invert since outer is CCW
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
        TagDictionary? tags,
        int pointsPerRevolution = DEFAULT_POINTS_PER_REVOLUTION,
        bool outer = true)
    {
        Coord[] coords = Coord.CreateCircle(center, radius, pointsPerRevolution, outer);
        return new Polygon(coords, tags);
    }

    #endregion
}