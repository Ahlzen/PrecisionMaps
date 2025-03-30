using MapLib.GdalSupport;
using MapLib.Geometry;

namespace MapLib.DataSources;

/// <remarks>
/// All bounds are lat/lon WGS84.
/// </remarks>
public abstract class BaseDataSource
{
    public abstract string Name { get; }

    public abstract string Srs { get; }

    /// <summary>
    /// Bounds of full data set. In source (dataset) SRS. If known.
    /// </summary>
    public abstract Bounds? Bounds { get; }

    /// <summary>
    /// Bounds of full data set, in lon/lat WGS84. If known.
    /// </summary>
    public Bounds? BoundsWgs84
    {
        get
        {
            if (Bounds == null) return null;
            Transformer sourceToWgs84 = new(Srs, Transformer.WktWgs84);
            return sourceToWgs84.Transform(Bounds.Value);
        }
    }
}

public abstract class BaseRasterDataSource : BaseDataSource
{
    /// <summary>
    /// Return all raster data for the specified source.
    /// </summary>
    public abstract RasterData GetData();

    ///// <summary>
    ///// Return raster data for (at least) the specified bounds from
    ///// this source.
    ///// </summary>
    ///// <remarks>
    ///// May return less if the requested area extends outside the bounds
    ///// of this data source.
    ///// May return more if needed (e.g. may not crop/trim a file data source).
    ///// </remarks>
    //public abstract RasterData GetData(Bounds boundsWgs84);
}

public abstract class BaseVectorDataSource : BaseDataSource
{
    /// <summary>
    /// Return vector data for (at least) the specified bounds from
    /// this source.
    /// </summary>
    /// <remarks>
    /// May return less if specified bounds are beyond those of
    /// this data source.
    /// May return more if needed (e.g. may not crop/trim a file data source).
    /// </remarks>
    public abstract VectorData GetData(Bounds boundsWgs84);
}