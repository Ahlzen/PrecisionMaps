using MapLib.Geometry;

namespace MapLib.DataSources;

/// <remarks>
/// All bounds are lat/lon WGS84.
/// </remarks>
public interface IDataSource
{
    public string Name { get; }

    public string Srs { get; }

    /// <summary>
    /// Bounds of full data set, if known.
    /// </summary>
    public Bounds? BoundsWgs84 { get; } // WGS84
}

public interface IRasterDataSource : IDataSource
{
    /// <summary>
    /// Return raster data for (at least) the specified bounds from
    /// this source.
    /// </summary>
    /// <remarks>
    /// May return less if the requested area extends outside the bounds
    /// of this data source.
    /// May return more if needed (e.g. may not crop/trim a file data source).
    /// </remarks>
    public RasterData GetData(Bounds boundsWgs84);
}

public interface IVectorDataSource : IDataSource
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
    public VectorData GetData(Bounds boundsWgs84);
}