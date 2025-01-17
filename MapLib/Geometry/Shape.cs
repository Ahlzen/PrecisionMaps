namespace MapLib.Geometry;

/// <summary>
/// Base class for all shapes.
/// </summary>
public abstract class Shape
{
    public const int DEFAULT_POINTS_PER_REVOLUTION = 24;

    public abstract Coord GetCenter();
    public abstract Bounds GetBounds();

    // TODO: Add options
    public abstract MultiPolygon Buffer(double radius);
}