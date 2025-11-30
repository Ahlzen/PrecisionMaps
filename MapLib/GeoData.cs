using MapLib.GdalSupport;
using MapLib.Geometry;

namespace MapLib;

public interface IHasSrs
{
    /// <summary>
    /// Spatial Reference System of this data set.
    /// </summary>
    public Srs Srs { get; }
}

public interface IBounded : IHasSrs
{
    /// <summary>
    /// Bounds, in the SRS of the source data set.
    /// </summary>
    public Bounds Bounds { get; }
}

public interface IGeoData : IHasSrs, IBounded { }

//public abstract class GeoData : IHasSrs, IBounded
//{
//    /// <summary>
//    /// Bounds, in source (dataset) SRS.
//    /// </summary>
//    public abstract Bounds Bounds { get; }

//    public virtual Coord Center => Bounds.Center;

//    /// <summary>
//    /// Spatial Reference System / Coordinate Reference System
//    /// </summary>
//    public Srs Srs { get; }

//    protected GeoData(Srs srs)
//    {
//        Srs = srs;
//    }
//}