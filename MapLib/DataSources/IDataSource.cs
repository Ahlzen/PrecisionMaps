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
    public RasterData GetData(Bounds bounds);
}

public interface IVectorDataSource : IDataSource
{
    public VectorData GetData(Bounds bounds);
}