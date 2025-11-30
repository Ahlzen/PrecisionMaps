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
    public static Bounds BoundsToSrs(this IBounded feature, Srs destSrs)
    {
        Transformer transformer = new(feature.Srs, destSrs);
        return transformer.Transform(feature.Bounds);
    }

    /// <summary>
    /// Transforms the coord of a feature/geometry to the specified SRS.
    /// </summary>
    public static Coord CoordToSrs(this IHasSrs feature, Coord coord, Srs destSrs)
    {
        Transformer transformer = new(feature.Srs, destSrs);
        return transformer.Transform(coord);
    }


    public static Coord Transform(this Coord coord, Srs srcSrs, Srs destSrs)
    {
        Transformer transformer = new(srcSrs, destSrs);
        return transformer.Transform(coord);
    }

    public static Coord ToWgs84(this Coord coord, Srs srcSrs)
        => coord.Transform(srcSrs, Srs.Wgs84);

    public static Bounds Transform(this Bounds bounds, Srs srcSrs, Srs destSrs)
    {
        Transformer transformer = new(srcSrs, destSrs);
        return transformer.Transform(bounds);
    }

    public static Bounds ToWgs84(this Bounds bounds, Srs srcSrs)
        => bounds.Transform(srcSrs, Srs.Wgs84);

    public static Bounds FromWgs84(this Bounds boundsWgs84, Srs destSrs)
        => boundsWgs84.Transform(Srs.Wgs84, destSrs);
}
