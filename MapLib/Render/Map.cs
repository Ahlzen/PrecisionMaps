using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Output;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace MapLib.Render;

/// <summary>
/// Strategies for handling the (common) case where the projection
/// of the requested area doesn't exactly match the aspect ratio
/// of the destination canvas.
/// </summary>
public enum AspectRatioMismatchStrategy
{
    /// <summary>
    /// Stretch the data to fit the canvas. Typically results
    /// in skewed objects.
    /// </summary>
    StretchToFillCanvas,

    /// <summary>
    /// Include only the requested area, centering the data
    /// on the canvas. Typically results in empty space on
    /// the left/right or top/bottom.
    /// </summary>
    CenterOnCanvas,

    /// <summary>
    /// Crop (in either x or y) the requested area to
    /// match the canvas aspect ratio. Typically results in parts
    /// of the requested area not being included on the map.
    /// </summary>
    CropBounds,

    /// <summary>
    /// Extend (in either x or y) the requested area to
    /// match the canvas aspect ratio. Typically results in a larger
    /// area than requested included on the map.
    /// </summary>
    ExtendBounds,
}

public class Map : IHasSrs, IBounded
{
    /// <summary>
    /// The requested bounds of the map, in lon/lat WGS84.
    /// </summary>
    public Bounds RequestedBoundsWgs84 { get; private set; }

    /// <summary>
    /// The actual bounds of the map, in lon/lat WGS84.
    /// </summary>
    /// <remarks>
    /// This may be the same as the requested bounds, or different
    /// if cropped or extended to match canvas aspect ratio.
    /// </remarks>
    public Bounds ActualBoundsWgs84 { get; private set; }

    /// <summary>
    /// The actual bounds of the map, in the map SRS.
    /// </summary>
    public Bounds ActualBoundsMapSrs { get; private set; }

    public Bounds Bounds => ActualBoundsMapSrs;

    /// <summary>
    /// Coordinate system and projection for the
    /// resulting map.
    /// </summary>
    /// <remarks>
    /// If data sources are in a different projection, they
    /// are reprojected on-the-fly.
    /// </remarks>
    public string Srs { get; set; }

    public List<MapDataSource> DataSources { get; } = new();

    public List<MapLayer> Layers { get; } = new();

    private double _scaleX, _scaleY;
    private double _offsetX, _offsetY;

    public Map(Bounds boundsWgs84, string mapSrs)
    {
        Srs = mapSrs;
        RequestedBoundsWgs84 = boundsWgs84;                       
    }

    private void ComputeActualBounds(
        Canvas canvas,
        AspectRatioMismatchStrategy strategy)
    {
        Transformer wgs84ToMapSrs = new(Transformer.WktWgs84, this.Srs);
        Transformer mapSrsToWgs84 = new(this.Srs, Transformer.WktWgs84);

        Debug.WriteLine("Requested bounds (WGS84): " + RequestedBoundsWgs84);

        double canvasAspectRatio = canvas.Width / canvas.Height;

        Debug.WriteLine("Canvas aspect ratio: " + canvasAspectRatio);

        double usableCanvasWidth = canvas.Width;
        double usableCanvasHeight = canvas.Height;
        double canvasOffsetX = 0;
        double canvasOffsetY = 0;

        Bounds requestedBoundsMapSrs = wgs84ToMapSrs.Transform(RequestedBoundsWgs84);
        double projectedAspectRatio = requestedBoundsMapSrs.Width / requestedBoundsMapSrs.Height;

        Debug.WriteLine("Requested bounds (map SRS): " + requestedBoundsMapSrs);
        Debug.WriteLine("Projected aspect ratio: " + projectedAspectRatio);

        // Compute actual bounds in the map SRS
        // (and possibly adjusting useable canvas size/offset)
        switch (strategy)
        {
            case AspectRatioMismatchStrategy.StretchToFillCanvas:
                {
                    // Use the requested bounds directly, regardless of aspect ratio
                    ActualBoundsMapSrs = wgs84ToMapSrs.Transform(RequestedBoundsWgs84);                    
                    break;
                }
            case AspectRatioMismatchStrategy.CenterOnCanvas:
                {
                    // use the same requested bounds, but change the useable canvas size accordingly
                    ActualBoundsMapSrs = requestedBoundsMapSrs;

                    if (projectedAspectRatio < canvasAspectRatio)
                    {
                        // canvas is wider than projected
                        usableCanvasWidth = canvas.Width * (projectedAspectRatio / canvasAspectRatio);
                        canvasOffsetX = (canvas.Width - usableCanvasWidth) / 2;
                    }
                    else
                    {
                        // canvas is taller (or equal to) projected
                        usableCanvasHeight = canvas.Height * (canvasAspectRatio / projectedAspectRatio);
                        canvasOffsetY = (canvas.Height - usableCanvasHeight) / 2;
                    }
                    break;
                }                
            case AspectRatioMismatchStrategy.CropBounds:
                {
                    if (projectedAspectRatio < canvasAspectRatio)
                    {
                        // Canvas is wider than projected data, i.e. projected data
                        // is taller than canvas: Need to crop top and bottom.
                        double useableVerticalFactor = projectedAspectRatio / canvasAspectRatio; // should be <= 1
                        Debug.Assert(useableVerticalFactor <= 1);
                        ActualBoundsMapSrs = requestedBoundsMapSrs.ResizeAndCenterY(
                            newHeight: requestedBoundsMapSrs.Height * useableVerticalFactor);
                    }
                    else
                    {
                        // Canvas is taller than projected data i.e. projected data
                        // is wider than canvas: Need to crop left and right.
                        double useableHorizontalFactor = canvasAspectRatio / projectedAspectRatio; // should be <= 1
                        Debug.Assert(useableHorizontalFactor <= 1);
                        ActualBoundsMapSrs = requestedBoundsMapSrs.ResizeAndCenterX(
                            newWidth: requestedBoundsMapSrs.Width * useableHorizontalFactor);
                    }
                    break;
                }
            case AspectRatioMismatchStrategy.ExtendBounds:
                {
                    if (projectedAspectRatio < canvasAspectRatio)
                    {
                        // Canvas is wider than projected data.
                        // Extend actual bounds horizontally:
                        double horizontalFactor = canvasAspectRatio / projectedAspectRatio;
                        Debug.Assert(horizontalFactor >= 1);
                        ActualBoundsMapSrs = requestedBoundsMapSrs.ResizeAndCenterX(
                            newWidth: requestedBoundsMapSrs.Width * horizontalFactor);
                    }
                    else
                    {
                        // Canvas is taller than projected data.
                        // Extend actual bounds vertically:
                        double verticalFactor = projectedAspectRatio / canvasAspectRatio;
                        Debug.Assert(verticalFactor >= 1);
                        ActualBoundsMapSrs = requestedBoundsMapSrs.ResizeAndCenterY(
                            newHeight: requestedBoundsMapSrs.Height * verticalFactor);
                    }
                    break;
                }
            default:
                throw new NotSupportedException("Unexpected strategy: " + strategy);
        }

        // Back-project to lon/lat
        ActualBoundsWgs84 = mapSrsToWgs84.Transform(ActualBoundsMapSrs);

        Debug.WriteLine("Actual bounds (map SRS): " + ActualBoundsMapSrs);
        Debug.WriteLine("Actual bounds (WGS84): " + ActualBoundsWgs84);
        Debug.WriteLine("Usable canvas width: " + usableCanvasWidth);
        Debug.WriteLine("Usable canvas height: " + usableCanvasHeight);

        // Compute offset/scale to transform
        // from map SRS to canvas space.
        // Any projection is already done at this point,
        // so we just need to scale/offset the result to fit the canvas.

        // TODO: add in canvasOffsetX, canvasOffsetY

        _scaleX = usableCanvasWidth / ActualBoundsMapSrs.Width;
        _scaleY = usableCanvasHeight / ActualBoundsMapSrs.Height;

        _offsetX = -ActualBoundsMapSrs.XMin * _scaleX;
        _offsetY = -ActualBoundsMapSrs.YMin * _scaleY;
    }

    public void Render(Canvas canvas,
        AspectRatioMismatchStrategy ratioMismatchStrategy = AspectRatioMismatchStrategy.CenterOnCanvas)
    {
        ComputeActualBounds(canvas, ratioMismatchStrategy);

        Dictionary<string, MapDataSource> sourcesByName =
            DataSources.ToDictionary(ds => ds.Name);
        
        foreach (MapLayer layer in Layers)
        {
            MapDataSource layerDataSource =
                sourcesByName[layer.DataSourceName];
            using Transformer wgs84ToSourceTransformer = new(
                Transformer.WktWgs84, layerDataSource.Srs);
            using Transformer sourceToMapTransformer = new(
                layerDataSource.Srs, Srs);

            // Determine bounds in the SRS of the data source
            Bounds dataSourceBounds = wgs84ToSourceTransformer.Transform(RequestedBoundsWgs84);

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
                VectorData layerDataCanvasSpace =
                    layerDataMapSrs.Transform(_scaleX, _scaleY, _offsetX, _offsetY);

                // Render data onto canvas
                DrawVectors(canvas, vectorLayer, layerDataCanvasSpace);
            }
            else if (layerDataSource is RasterMapDataSource rasterDataSource)
            {
                if (!(layer is RasterMapLayer))
                    throw new InvalidOperationException("Raster data source must use raster layer");
                var rasterLayer = (RasterMapLayer)layer;

                RasterData rasterData = rasterDataSource.DataSource.GetData();
                DrawRaster(canvas, rasterLayer, rasterData);
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

    private void DrawRaster(Canvas canvas, RasterMapLayer mapLayer, RasterData data)
    {
        CanvasLayer layer = canvas.AddNewLayer(mapLayer.Name);

        Console.WriteLine("Map SRS: " + Srs);
        Console.WriteLine("Raster SRS: " + data.Srs);
        
        Console.WriteLine("Map bounds LL: " + this.BoundsToSrs(Transformer.WktWgs84));
        Console.WriteLine("Raster bounds LL: " + data.BoundsToSrs(Transformer.WktWgs84));

        Console.WriteLine("Map bounds: " + Bounds);
        Console.WriteLine("Raster bounds: " + data.BoundsToSrs(this.Srs));

        // Find overlapping area (in map SRS)
        Bounds? overlap = Bounds.Intersection(data.BoundsToSrs(this.Srs));
        if (overlap == null)
            return; // no overlap - nothing to render

        // TODO: Figure out cropped part of bitmap
        // For now, just transform coordinates to canvas space,
        // and draw the full bitmap
        Bounds rasterBoundsInMapSrs = data.BoundsToSrs(this.Srs);
        Bounds rasterBoundsInCanvasSrs = rasterBoundsInMapSrs.Transform(
            _scaleX, _scaleY, _offsetX, _offsetY);
        layer.DrawBitmap(data.Bitmap,
            rasterBoundsInCanvasSrs.XMin, rasterBoundsInCanvasSrs.YMin,
            rasterBoundsInCanvasSrs.Width, rasterBoundsInCanvasSrs.Height,
            1.0);
    }
}
