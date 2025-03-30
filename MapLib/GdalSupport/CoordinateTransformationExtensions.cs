using MapLib.Geometry;

namespace MapLib.GdalSupport;

/// <summary>
/// Extension methods for transforming coordinates and bounds
/// between spatial reference systems.
/// </summary>
public static class CoordinateTransformationExtensions
{
    /// <summary>
    /// Transforms the bounds to the specified SRS.
    /// </summary>
    public static Bounds BoundsToSrs(this IBounded feature, string destSrs)
    {
        Transformer transformer = new(feature.Srs, destSrs);
        return transformer.Transform(feature.Bounds);
    }

    /// <summary>
    /// Transforms the coord of a feature/geometry to the specified SRS.
    /// </summary>
    public static Coord CoordToSrs(this IHasSrs feature, Coord coord, string destSrs)
    {
        Transformer transformer = new(feature.Srs, destSrs);
        return transformer.Transform(coord);
    }
}
