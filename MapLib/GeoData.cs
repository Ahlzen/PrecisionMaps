using MapLib.Geometry;

namespace MapLib;

public abstract class GeoData
{
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