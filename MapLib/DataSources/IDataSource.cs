using MapLib.Geometry;

namespace MapLib.DataSources;

/// <remarks>
/// All bounds are lat/lon WGS84.
/// </remarks>
public interface IDataSource
{
    public string Name { get; }
    public Bounds Bounds { get; }
}

public interface IRasterDataSource : IDataSource
{
    /// <summary>
    /// Return raster data for (at least) the specified bounds from
    /// this source.
    /// </summary>
    /// <remarks>
    /// May return less if specified bounds are beyond those of
    /// this data source.
    /// May return more if needed (e.g. may not crop/trim a file data source).
    /// </remarks>
    public RasterData GetData(Bounds bounds);
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
    public VectorData GetData(Bounds bounds);
}