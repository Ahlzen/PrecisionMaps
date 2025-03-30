using MapLib.Geometry;

namespace MapLib;

public interface IHasSrs
{
    /// <summary>
    /// Spatial Reference System of this data set, in WKT format.
    /// </summary>
    public string Srs { get; }
}

public interface IBounded : IHasSrs
{
    /// <summary>
    /// Bounds, in the SRS of the source data set.
    /// </summary>
    public Bounds Bounds { get; }
}

public abstract class GeoData : IHasSrs, IBounded
{
    /// <summary>
    /// Bounds, in source (dataset) SRS.
    /// </summary>
    public abstract Bounds Bounds { get; }

    public virtual Coord Center => Bounds.Center;

    /// <summary>
    /// Spatial Reference System / Coordinate Reference System
    /// (can be shorthand accepted by PROJ/GDAL/OGR like "WGS84" or "EPSG:4326",
    /// or a full OGS WKT definition)
    /// </summary>
    public string Srs { get; }

    protected GeoData(string srs)
    {
        Srs = srs;
    }
}