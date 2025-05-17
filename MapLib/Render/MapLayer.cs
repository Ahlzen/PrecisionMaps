using MapLib.Output;
using System.Drawing;

namespace MapLib.Render;

public abstract class MapLayer
{
    public MapLayer(string name, string dataSourceName)
    {
        Name = name;
        DataSourceName = dataSourceName;
    }

    public string Name { get; }
    public string DataSourceName { get; }
}

public class RasterMapLayer : MapLayer
{
    public RasterMapLayer(string name, string dataSourceName)
        : base(name, dataSourceName)
    {
    }
}

public class VectorMapLayer : MapLayer
{
    public TagFilter? Filter { get; }
    public VectorStyle Style { get; }

    public VectorMapLayer(string name, string dataSourceName,
        VectorStyle style, TagFilter? filter = null)
        : base(name, dataSourceName)
    {
        Style = style;
        Filter = filter;
    }
}
