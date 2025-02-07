namespace MapLib.Geometry;

/// <summary>
/// A 2d (x/y) coordinate or vector. Immutable.
/// </summary>
public struct Coord : IEquatable<Coord>
{
    public double X { get; }
    public double Y { get; }

    public Coord(double x, double y) { X = x; Y = y; }
    public Coord(Coord c) { X = c.X; Y = c.Y; }

    public static implicit operator Coord((double x, double y) value)
        => new Coord(value.x, value.y);

    public override string ToString() => $"({X}, {Y})";

    public double DistanceTo(Coord c) => Distance(this, c);

    #region IEquatable

    public override bool Equals(object? o)
        => o is Coord c2 && this.Equals(c2);

    public bool Equals(Coord c)
        => X == c.X && Y == c.Y;

    public override int GetHashCode()
        => (X, Y).GetHashCode();

    public static bool operator ==(Coord p1, Coord p2)
        => p1.Equals(p2);

    public static bool operator !=(Coord p1, Coord p2)
        => !(p1 == p2);

    #endregion

    public static Coord operator - (Coord c)
        => new Coord(-c.X, -c.Y);

    public static Coord operator + (Coord c1, Coord c2)
        => new Coord(c1.X + c2.X, c1.Y + c2.Y);

    public static Coord operator - (Coord c1, Coord c2)
        => new Coord(c1.X - c2.X, c1.Y - c2.Y);

    public static Coord operator * (Coord c, double d)
        => new Coord(c.X * d, c.Y * d);

    public static Coord operator * (double d, Coord c)
        => new Coord(c.X * d, c.Y * d);

    /// <summary>
    /// Cross/vector product (magnitude) of c1 and c2.
    /// </summary>
    public static double operator ^ (Coord c1, Coord c2)
        => c1.X * c2.Y - c1.Y * c2.X;

    /// <summary>
    /// Dot product of vectors c1 and c2.
    /// </summary>
    public static double operator * (Coord c1, Coord c2)
        => c1.X * c2.X + c1.Y * c2.Y;

    
    // Math utilities

    /// <summary>
    /// Distance between two points.
    /// </summary>
    public static double Distance(Coord c1, Coord c2)
    {
        double width = c1.X - c2.X;
        double height = c1.Y - c2.Y;
        return Math.Sqrt(width * width + height * height);
    }

    /// <summary>
    /// Length of the vector from origin to point c.
    /// </summary>
    public static double Length(Coord c)
    {
        return Math.Sqrt(c.X * c.X + c.Y * c.Y);
    }

    /// <summary>
    /// Linear interpolation (or extrapolation) between
    /// points c1 and c2.
    /// </summary>
    public static Coord Lerp(Coord c1, Coord c2, double t)
    {
        return new Coord(
            c1.X + (c2.X - c1.X) * t,
            c1.Y + (c2.Y - c1.Y) * t);
    }

    public static Coord None =
        new Coord(double.MinValue, double.MinValue);

    #region Static primitive factory methods

    public static Coord[] CreateCircle(Coord center, double radius,
        int pointsPerRevolution = Shape.DEFAULT_POINTS_PER_REVOLUTION,
        bool outer = true)
    {
        Coord[] coords = new Coord[pointsPerRevolution + 1];
        double radsPerPoint = 2.0 * Math.PI / pointsPerRevolution;
        if (outer)
            radsPerPoint *= -1; // this ensures CCW outer polygon
        double angle = 0;
        for (int p = 0; p < pointsPerRevolution; p++)
        {
            coords[p] = new Coord(
                center.X + Math.Sin(angle) * radius,
                center.Y + Math.Cos(angle) * radius);
            angle += radsPerPoint;
        }
        coords[pointsPerRevolution] = coords[0];
        return coords;
    }

    #endregion
}