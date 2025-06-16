using MapLib.DataSources;
using MapLib.DataSources.Vector;

namespace MapLib.Render;

public abstract class MapDataSource
{
    public string Name { get; }
 
    public abstract string SourceSrs { get; }

    //public abstract string MapSrs { get; }



    public MapDataSource(string name)
    {
        Name = name;
    }
}

public class VectorMapDataSource : MapDataSource
{
    public BaseVectorDataSource DataSource { get; }
    public override string SourceSrs => DataSource.Srs;



    public VectorMapDataSource(string name,
        BaseVectorDataSource dataSource) : base(name)
    {
        DataSource = dataSource;
    }
}

public class RasterMapDataSource2 : MapDataSource
{
    public BaseRasterDataSource DataSource { get; }
    public override string SourceSrs => DataSource.Srs;

    public RasterMapDataSource2(string name,
        BaseRasterDataSource dataSource) : base(name)
    {
        DataSource = dataSource;
    }
}
