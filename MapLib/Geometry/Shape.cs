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


    public Shape(TagList? tags)
    { 
        Tags = tags ?? NoTags;
    }


    ///// Tags

    public TagList Tags { get; }

    public bool HasTag(string key) =>
        Tags.Any(kvp => kvp.Key == key);

    public string? this[string key]
    {
        get
        {
            foreach (KeyValuePair<string, string> kvp in Tags)
                if (kvp.Key == key) return kvp.Value;
            return null;
        }
    }


    ///// Geometry

    public abstract Coord GetCenter();

    public abstract Bounds GetBounds();

    /// <summary>
    /// Returns true iff this shape's bounding box intersects
    /// (partly or fully) the specified bounds.
    /// </summary>
    public bool IsWithin(Bounds bounds) => GetBounds().Intersects(bounds);

    /// <summary>
    /// Buffer the shape and returns the resulting polygon(s).
    /// </summary>
    /// <param name="radius"></param>
    /// <returns>
    /// The buffered shape, or null if no resulting geometry (e.g.
    /// if offsetting inward, and there is no resulting area).
    /// </returns>
    public abstract MultiPolygon? Buffer(double radius);
}
