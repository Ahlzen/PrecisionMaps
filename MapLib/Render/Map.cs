using MapLib.DataSources;
using MapLib.Geometry;

namespace MapLib.Render;

public class Map
{
    /// <summary>
    /// Geographical bounds of the map (WGS84 lat/lon)
    /// </summary>
    public Bounds Bounds { get; set; }

    // Canvas dimensions
    public CanvasUnit CanvasUnit { get; set; }
    public double CanvasWidth { get; set; }
    public double CanvasHeight { get; set; }


    public Map(Bounds bounds, CanvasUnit canvasUnit,
        double canvasWidth, double canvasHeight)
    {
        Bounds = bounds;
        CanvasUnit = canvasUnit;
        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;
    }


}


public abstract class MapDataSource
{
    public string Name { get; }
    //public abstract IDataSource DataSource { get; }
}

public class MapVectorDataSource : MapDataSource
{
    public IVectorDataSource DataSource { get; }
}

public class MapRasterDataSource : MapDataSource
{
    public IRasterDataSource DataSource { get; }
}



public abstract class MapLayer
{
    public string? Name { get; }
}

public class VectorMapLayer : MapLayer
{
    public string DataSourceName { get; set; }

}