namespace MapLib.Geometry;

/// <summary>
/// Base class for all shapes.
/// </summary>
public abstract class Shape
{
    /// <summary>
    /// Default number of points per full revolution for buffering,
    /// round line joints/ends, etc.
    /// </summary>
    public const int DEFAULT_POINTS_PER_REVOLUTION = 24;

    public static readonly TagList NoTags =
        Array.Empty<KeyValuePair<string, string>>();

    public TagList Tags { get; }
    public Shape(TagList? tags) { Tags = tags ?? NoTags; }


    public abstract Coord GetCenter();
    public abstract Bounds GetBounds();

    // TODO: Add options
    public abstract MultiPolygon Buffer(double radius);
}