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

public class VectorMapLayer : MapLayer
{
    public TagFilter? Filter { get; }

    // TODO: break out and expand
    public Color? FillColor { get; }
    public Color? StrokeColor { get; }
    public double? StrokeWidth { get; }

    public VectorMapLayer(string name, string dataSourceName,
        TagFilter? filter = null,
        Color? fillColor = null,
        Color? strokeColor = null,
        double? strokeWidth = null)
        : base(name, dataSourceName)
    {
        Filter = filter;
        FillColor = fillColor;
        StrokeColor = strokeColor;
        StrokeWidth = strokeWidth;
    }
}

public class RasterMapLayer : MapLayer
{
    public RasterMapLayer(string name, string dataSourceName)
        : base(name, dataSourceName)
    {
    }
}