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

        scaleX = canvas.Width / BoundsMapSrs.Width;
        scaleY = canvas.Height / BoundsMapSrs.Height;
        offsetX = -BoundsMapSrs.XMin * scaleX;
        offsetY = -BoundsMapSrs.YMin * scaleY;


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
                if (!(layer is VectorMapLayer))
                    throw new InvalidOperationException("Vector data source must use vector layer");
                var vectorLayer = (VectorMapLayer)layer;

                VectorData layerDataSourceSrs = vectorDataSource.DataSource.GetData(dataSourceBounds);

                // Filter features?
                if (vectorLayer.Filter != null)
                    layerDataSourceSrs = vectorLayer.Filter.Filter(layerDataSourceSrs);

                // Transform layer data to map SRS/Projection
                VectorData layerDataMapSrs = layerDataSourceSrs.Transform(sourceToMapTransformer);

                // Transform to canvas space
                VectorData layerDataCanvasSpace = layerDataMapSrs.Transform(scaleX, scaleY, offsetX, offsetY);

                // Render data onto canvas
                DrawVectors(canvas, vectorLayer, layerDataCanvasSpace);
            }
            else if (layerDataSource is RasterMapDataSource rasterDataSource)
            {
                throw new NotImplementedException();
            }
        }
    }

    private void DrawVectors(Canvas canvas, VectorMapLayer mapLayer, VectorData data)
    {
        // TEST CODE (placeholder for real rendering)

        CanvasLayer layer = canvas.AddNewLayer(mapLayer.Name);
        Color? strokeColor = mapLayer.StrokeColor;
        Color? fillColor = mapLayer.FillColor;
        double? strokeWidth = mapLayer.StrokeWidth;

        // Fill
        if (fillColor != null)
        {
            foreach (Polygon p in data.Polygons)
                layer.DrawFilledPolygon(p.Coords, fillColor.Value);
            foreach (MultiPolygon mp in data.MultiPolygons)
                layer.DrawFilledMultiPolygon(mp.Coords, fillColor.Value);
        }

        // Stroke
        if (strokeColor != null && strokeWidth != null)
        {
            // Points
            layer.DrawFilledCircles(data.Points.Select(p => p.Coord),
                strokeWidth.Value, strokeColor.Value);
            foreach (MultiPoint mp in data.MultiPoints)
                layer.DrawFilledCircles(mp.Coords,
                    strokeWidth.Value, strokeColor.Value);

            // Lines
            foreach (Line l in data.Lines)
                layer.DrawLine(l.Coords, strokeWidth.Value, strokeColor.Value);
            foreach (MultiLine ml in data.MultiLines)
                layer.DrawLines(ml.Coords, strokeWidth.Value, strokeColor.Value);

            // Polygons
            foreach (Polygon p in data.Polygons)
                layer.DrawLine(p.Coords, strokeWidth.Value, strokeColor.Value);
            foreach (MultiPolygon mp in data.MultiPolygons)
                layer.DrawLines(mp.Coords, strokeWidth.Value, strokeColor.Value);
        }
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

public class TagFilter
{
    private List<(string, string?)> Tags { get; } = new();

    /// <param name="tagName">Name. Case sensitive.</param>
    /// <param name="tagValue">Value. Case sensitive.
    /// Null is wildcard (name must match, but can be any value)
    /// </param>
    public TagFilter(string tagName, string? tagValue = null)
    {
        Tags.Add((tagName, tagValue));
    }

    public TagFilter(IEnumerable<(string, string?)> tags)
    {
        Tags.AddRange(tags);
    }

    public TagFilter(params (string, string?)[] tags)
    {
        Tags.AddRange(tags);
    }

    public bool Matches(TagList featureTags)
    {
        // Find key
        foreach (KeyValuePair<string, string> featureTag in featureTags)
        {
            foreach ((string filterKey, string? filterValue) in Tags)
            if (featureTag.Key == filterKey)
            {
                if (filterValue == null)
                    return true;
                else if (filterValue == featureTag.Value)
                    return true;
            }
        }
        return false;
    }

    public VectorData Filter(VectorData source)
    {
        return new VectorData(source.Srs,
            source.Points.Where(p => Matches(p.Tags)).ToArray(),
            source.MultiPoints.Where(mp => Matches(mp.Tags)).ToArray(),
            source.Lines.Where(l => Matches(l.Tags)).ToArray(),
            source.MultiLines.Where(ml => Matches(ml.Tags)).ToArray(),
            source.Polygons.Where(p => Matches(p.Tags)).ToArray(),
            source.MultiPolygons.Where(mp => Matches(mp.Tags)).ToArray());
    }
}