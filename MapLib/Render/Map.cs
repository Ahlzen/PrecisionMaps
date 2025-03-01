using MapLib.DataSources;
using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Output;
using System.Drawing;

namespace MapLib.Render;

public class Map
{
    /// <summary>
    /// Geographical bounds of the map (WGS84 lat/lon)
    /// </summary>
    public Bounds BoundsWGS84 { get; set; }

    /// <summary>
    /// Bounds in the SRS/projection of the map.
    /// </summary>
    public Bounds BoundsMapSrs { get; set; }

    /// <summary>
    /// Coordinate system and projection for the
    /// resulting map.
    /// </summary>
    /// <remarks>
    /// If data sources are in a different projection, they
    /// are reprojected on-the-fly.
    /// </remarks>
    public string MapSrs { get; set; }


    public List<MapDataSource> DataSources { get; }
        = new List<MapDataSource>();

    public List<MapLayer> Layers { get; }
        = new List<MapLayer>();


    public Map(Bounds boundsWgs84, string mapSrs)
    {
        MapSrs = mapSrs;
        BoundsWGS84 = boundsWgs84;

        // Compute bounds in the map SRS
        Transformer wgsToMapSrs = new(Transformer.WktWgs84, this.MapSrs);
        BoundsMapSrs = wgsToMapSrs.Transform(boundsWgs84);
    }


    public void Render(Canvas canvas)
    {
        Dictionary<string, MapDataSource> sourcesByName =
            DataSources.ToDictionary(ds => ds.Name);

        // Compute offset/scale to transform
        // map SRS to canvas space
        double offsetX, offsetY;
        double scaleX, scaleY;
        offsetX = -BoundsMapSrs.XMin;
        offsetY = -BoundsMapSrs.YMin;
        scaleX = canvas.Width / BoundsMapSrs.Width;
        scaleY = canvas.Height / BoundsMapSrs.Height;


        foreach (MapLayer layer in Layers)
        {
            MapDataSource layerDataSource =
                sourcesByName[layer.DataSourceName];
            using Transformer wgs84ToSourceTransformer = new(
                Transformer.WktWgs84, layerDataSource.Srs);
            using Transformer sourceToMapTransformer = new(
                layerDataSource.Srs, MapSrs);

            // Determine bounds in the SRS of the data source
            Bounds dataSourceBounds = wgs84ToSourceTransformer.Transform(BoundsWGS84);

            // Get data
            if (layerDataSource is VectorMapDataSource vectorDataSource)
            {
                VectorData layerDataSourceSrs = vectorDataSource.DataSource.GetData(dataSourceBounds);

                // Transform layer data to map SRS/Projection
                VectorData layerDataMapSrs = layerDataSourceSrs.Transform(sourceToMapTransformer);

                // Transform to canvas space
                VectorData layerDataCanvasSpace = layerDataMapSrs.Transform(scaleX, scaleY, offsetX, offsetY);

                // Render data onto canvas
                DrawLines(canvas, layer.Name, layerDataCanvasSpace);
            }
            else if (layerDataSource is RasterMapDataSource rasterDataSource)
            {
                throw new NotImplementedException();
            }
        }
    }

    private void DrawLines(Canvas canvas, string layerName, VectorData data)
    {
        // TEST CODE (placeholder for real rendering)

        CanvasLayer layer = canvas.AddNewLayer(layerName);
        Color color = Color.Black;
        layer.DrawFilledCircles(data.Points.Select(p => p.Coord), canvas.FromMm(3), color);
        foreach (MultiPoint mp in data.MultiPoints)
            layer.DrawFilledCircles(mp.Coords, canvas.FromMm(3), color);
        foreach (Line l in data.Lines)
            layer.DrawLine(l.Coords, canvas.FromMm(1), color);
        foreach (MultiLine ml in data.MultiLines)
            layer.DrawLines(ml.Coords, canvas.FromMm(1), color);
        foreach (Polygon p in data.Polygons)
            layer.DrawLine(p.Coords, canvas.FromMm(1), color);
        foreach (MultiPolygon mp in data.MultiPolygons)
            layer.DrawLines(mp.Coords, canvas.FromMm(1), color);
    }
}


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
    public VectorMapLayer(string name, string dataSourceName)
        : base(name, dataSourceName)
    {
    }
}