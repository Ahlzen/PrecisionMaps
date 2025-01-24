using MapLib.Geometry;

namespace MapLib;

public abstract class GeoData
{
    public abstract Bounds Bounds { get; }
    public virtual Coord Center => Bounds.Center;
}