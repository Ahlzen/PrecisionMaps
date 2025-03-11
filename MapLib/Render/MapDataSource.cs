using MapLib.DataSources;

namespace MapLib.Render;

public abstract class MapDataSource
{
    public string Name { get; }
    public abstract string Srs { get; }

    public MapDataSource(string name)
    {
        Name = name;
    }
}

public class VectorMapDataSource : MapDataSource
{
    public IVectorDataSource DataSource { get; }
    public override string Srs => DataSource.Srs;

    public VectorMapDataSource(string name,
        IVectorDataSource dataSource) : base(name)
    {
        DataSource = dataSource;
    }
}

public class RasterMapDataSource : MapDataSource
{
    public IRasterDataSource DataSource { get; }
    public override string Srs => DataSource.Srs;

    public RasterMapDataSource(string name,
        IRasterDataSource dataSource) : base(name)
    {
        DataSource = dataSource;
    }
}
