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


    public static Coord Transform(this Coord coord, string srcSrs, string destSrs)
    {
        Transformer transformer = new(srcSrs, destSrs);
        return transformer.Transform(coord);
    }
    public static Coord ToWgs84(this Coord coord, string srcSrs)
        => coord.Transform(srcSrs, Transformer.WktWgs84);

    public static Bounds Transform(this Bounds bounds, string srcSrs, string destSrs)
    {
        Transformer transformer = new(srcSrs, destSrs);
        return transformer.Transform(bounds);
    }
    public static Bounds ToWgs84(this Bounds bounds, string srcSrs)
        => bounds.Transform(srcSrs, Transformer.WktWgs84);
}
