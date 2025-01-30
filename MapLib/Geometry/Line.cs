using System.Reflection.Metadata.Ecma335;

namespace MapLib.Geometry;

/// <summary>
/// 2D (multi-point) line. Immutable.
/// </summary>
public class Line : Shape, IEnumerable<Coord>
{
    public Coord[] Coords { get; }

    public Line(Coord[] coords, TagList? tags) : base(tags)
    {
        Coords = coords;
    }

    public Line(IEnumerable<Coord> coords, TagList? tags) : base(tags)
    {
        Coords = coords.ToArray();
    }

    public override Bounds GetBounds() {
        if (_bounds == null)
            _bounds = Bounds.FromCoords(Coords);
        return _bounds.Value;
    }
    private Bounds? _bounds; // cached bounds

    public Coord this[int i] {
        get => Coords[i];
    }

    public override Coord GetCenter()
        => GetBounds().Center;

    #region Modifiers

    public virtual MultiLine AsMultiLine()
        => new MultiLine(new Coord[][]{Coords}, Tags);

    /// <returns>
    /// Returns the line transformed as (X*scale+offsetX, Y*scale+offsetY)
    /// </returns>
    public virtual Line Transform(double scale, double offsetX, double offsetY)
        => new(Coords.Transform(scale, offsetX, offsetY), Tags);

    public virtual Line Transform(Func<Coord, Coord> transformation)
        => new Line(Coords.Select(transformation), Tags);

    public virtual Line Reverse()
        => new Line(Coords.Reverse(), Tags);

    /// <summary>
    /// Offsets a line the given distance. Positive is right
    /// (outward for polygons), negative is left (inward).
    /// </summary>
    public virtual Line Offset(double d)
    {
        // TEST CODE
        // TODO: use clipper

        if (d == 0) return this;

        List<Coord> coords = new List<Coord>(Coords.Length * 2);
        Coord lastCoord = Coord.None;
        for (int n = 0; n < Coords.Length-1; n++) {
            Coord offset = ComputeOffset(Coords[n], Coords[n+1], d);
            Coord c1 = Coords[n] + offset;
            Coord c2 = Coords[n + 1] + offset;
            if (c1 != lastCoord) coords.Add(c1); // connector (if applicable)
            coords.Add(c2);
        }
        return new Line(coords, Tags);
    }

    private Coord ComputeOffset(Coord c1, Coord c2, double distance)
    {
        double length = c1.DistanceTo(c2);
        if (length == 0) throw new InvalidOperationException("Cannot compute offset for 0-length line");
        double scale = distance / length;
        return new Coord(scale*(c2.Y - c1.Y), -scale*(c2.X - c1.X));
    }

    #endregion

    #region Calculations

    public override MultiPolygon Buffer(double radius)
        => AsMultiLine().Buffer(radius);

    /// <summary>
    /// Finds intersection between lines a1-a2 and b1-b2.
    /// Returns Coord.None if lines do not intersect.
    /// </summary>
    public static Coord LineSegmentIntersection(
        Coord p0, Coord p1, Coord p2, Coord p3)
    {
        double s1_x = p1.X - p0.X;
        double s1_y = p1.Y - p0.Y;
        double s2_x = p3.X - p2.X;
        double s2_y = p3.Y - p2.Y;

        double s = (-s1_y * (p0.X - p2.X) + s1_x * (p0.Y - p2.Y)) / (-s2_x * s1_y + s1_x * s2_y);
        double t = (s2_x * (p0.Y - p2.Y) - s2_y * (p0.X - p2.X)) / (-s2_x * s1_y + s1_x * s2_y);

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
            return new Coord(
                p0.X + (t * s1_x),
                p0.Y + (t * s1_y));
        }

        return Coord.None; // No collision

        //    s2_x = p3_x - p2_x; s2_y = p3_y - p2_y;
        //char get_line_intersection(float p0_x, float p0_y, float p1_x, float p1_y,
        //    float p2_x, float p2_y, float p3_x, float p3_y, float* i_x, float* i_y)
        //{
        //    float s1_x, s1_y, s2_x, s2_y;
        //    s1_x = p1_x - p0_x; s1_y = p1_y - p0_y;
        //    s2_x = p3_x - p2_x; s2_y = p3_y - p2_y;

        //    float s, t;
        //    s = (-s1_y * (p0_x - p2_x) + s1_x * (p0_y - p2_y)) / (-s2_x * s1_y + s1_x * s2_y);
        //    t = (s2_x * (p0_y - p2_y) - s2_y * (p0_x - p2_x)) / (-s2_x * s1_y + s1_x * s2_y);

        //    if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        //    {
        //        // Collision detected
        //        if (i_x != NULL)
        //            *i_x = p0_x + (t * s1_x);
        //        if (i_y != NULL)
        //            *i_y = p0_y + (t * s1_y);
        //        return 1;
        //    }

        //    return 0; // No collision
        //}

        //Coord p = a1;
        //Coord r = a2 - a1;
        //Coord q = b1;
        //Coord s = b2 - b1;

        //// t = (q − p) × s / (r × s)

        ////double t = (q-p)^
        //if ((r^s) == 0)
        //{

        //}
    }

    #endregion

    public IEnumerator<Coord> GetEnumerator() => (IEnumerator<Coord>) Coords.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Coords.GetEnumerator();
}