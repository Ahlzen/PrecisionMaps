using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Geometry.Helpers;
using MapLib.Output;
using System.Diagnostics;
using System.Drawing;

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

    public ObjectPlacementManager PlacementManager { get; } = new(); 

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
        Transformer wgs84ToMapSrs = new(Epsg.Wgs84, this.Srs);
        Transformer mapSrsToWgs84 = new(this.Srs, Epsg.Wgs84);

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

        _offsetX = canvasOffsetX + -ActualBoundsMapSrs.XMin * _scaleX;
        _offsetY = canvasOffsetY + -ActualBoundsMapSrs.YMin * _scaleY;
    }

    public async void Render(Canvas canvas,
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
                Epsg.Wgs84, layerDataSource.SourceSrs);
            using Transformer sourceToMapTransformer = new(
                layerDataSource.SourceSrs, Srs);

            // Determine bounds in the SRS of the data source
            Bounds dataSourceBounds = wgs84ToSourceTransformer.Transform(RequestedBoundsWgs84);

            // Get data
            if (layerDataSource is VectorMapDataSource vectorDataSource)
            {
                if (!(layer is VectorMapLayer))
                    throw new InvalidOperationException("Vector data source must use vector layer");
                var vectorLayer = (VectorMapLayer)layer;
                VectorData data = await vectorDataSource.DataSource.GetData(
                    dataSourceBounds, this.Srs);

                // Filter features (if applicable)
                if (vectorLayer.Filter != null)
                    data = vectorLayer.Filter.Filter(data);

                // Transform to canvas space
                VectorData dataInCanvasSpace =
                    data.Transform(_scaleX, _scaleY, _offsetX, _offsetY);

                // Render data onto canvas
                DrawVectors(canvas, vectorLayer, dataInCanvasSpace);
            }
            //else if (layerDataSource is RasterMapDataSource rasterDataSource)
            //{
            //    if (!(layer is RasterMapLayer))
            //        throw new InvalidOperationException("Raster data source must use raster layer");
            //    var rasterLayer = (RasterMapLayer)layer;
            //    RasterData data = await rasterDataSource.DataSource.GetData(this.Srs);
            //    DrawRaster(canvas, rasterLayer, data);
            //}
            else if (layerDataSource is RasterMapDataSource2 rasterDataSource2)
            {
                if (!(layer is RasterMapLayer))
                    throw new InvalidOperationException("Raster data source must use raster layer");
                var rasterLayer = (RasterMapLayer)layer;
                RasterData2 data = await rasterDataSource2.DataSource.GetData(this.Srs);
                DrawRaster(canvas, rasterLayer, data);
            }
        }
    }

    private void DrawVectors(Canvas canvas, VectorMapLayer mapLayer, VectorData data)
    {
        // TEST CODE (placeholder for real rendering)

        CanvasLayer layer = canvas.AddNewLayer(mapLayer.Name);
        VectorStyle style = mapLayer.Style;

        Color? lineColor = style.LineColor;
        Color? fillColor = style.FillColor;
        double? lineWidth = style.LineWidth;

        // TODO: Only enumerate these if we really need them
        // TODO: Use better polygon centroid algorithm (at least
        // some form of point-in-polygon!)

        IEnumerable<(Coord[] coords, TagList tags)> pointCoords =
            data.Points.Select(p => (new[] { p.Coord }, p.Tags))
            //.Union(data.MultiPoints.Select(mp => (mp.Coords, mp.Tags)));
            .Union(data.MultiPoints.Select(mp => (new[] { mp.GetBounds().Center }, mp.Tags)));

        IEnumerable<(Coord[] coords, TagList tags)> lineMidpoints =
            data.Lines.Select(l => (new[] { l.GetMidpoint() }, l.Tags))
            //.Union(data.MultiLines.Select(ml => (ml.Select(cs => cs.GetMidpoint()).ToArray(), ml.Tags)));
            .Union(data.MultiLines.Select(ml => (new[] { ml.GetBounds().Center }, ml.Tags)));

        IEnumerable<(Coord[] coords, TagList tags)> polygonCentroids =
            data.Polygons.Select(p => (new[] { p.GetCenter() }, p.Tags))
            //.Union(data.MultiPolygons.Select(mp => (mp.Select(cs => Bounds.FromCoords(cs).Center).ToArray(), mp.Tags)));
            .Union(data.MultiPolygons.Select(mp => (new[] { mp.GetBounds().Center }, mp.Tags)));

        IEnumerable<(Coord[] coords, TagList tags)> allPoints =
            pointCoords.Union(lineMidpoints).Union(polygonCentroids);

        // Fill
        if (fillColor != null)
        {
            foreach (Polygon p in data.Polygons)
                layer.DrawFilledPolygon(p.Coords, fillColor.Value);
            foreach (MultiPolygon mp in data.MultiPolygons)
                layer.DrawFilledMultiPolygon(mp.Coords, fillColor.Value);
        }

        // Stroke
        if (lineColor != null && lineWidth != null)
        {
            // Points
            layer.DrawFilledCircles(pointCoords.SelectMany(c => c.coords),
                lineWidth.Value, lineColor.Value);

            // Lines
            foreach (Line l in data.Lines)
                layer.DrawLine(l.Coords, lineWidth.Value, lineColor.Value);
            foreach (MultiLine ml in data.MultiLines)
                layer.DrawLines(ml.Coords, lineWidth.Value, lineColor.Value);

            // Polygons
            foreach (Polygon p in data.Polygons)
                layer.DrawLine(p.Coords, lineWidth.Value, lineColor.Value);
            foreach (MultiPolygon mp in data.MultiPolygons)
                layer.DrawLines(mp.Coords, lineWidth.Value, lineColor.Value);
        }

        // Symbol
        if (style.Symbol != null)
        {
            SymbolType symbolType = style.Symbol.Value;
            double symbolSize = style.SymbolSize ?? canvas.Width * 0.001;
            Color symbolFillColor = style.SymbolColor ?? Color.Black;
            

            switch (style.Symbol)
            {
                case SymbolType.Circle:
                    layer.DrawFilledCircles(allPoints.SelectMany(c => c.coords),
                        symbolSize/2, symbolFillColor);
                    foreach (Coord point in allPoints.SelectMany(p => p.coords))
                        PlacementManager.TryAdd([new Bounds(
                            point.X-symbolSize/2, point.X+symbolSize/2,
                            point.Y-symbolSize/2, point.Y+symbolSize/2)]);
                    break;
                case SymbolType.Square:
                    throw new NotImplementedException();
                    break;
                case SymbolType.Star:
                    throw new NotImplementedException();
                    break;
                case SymbolType.Image:
                    throw new NotImplementedException();
                    break;
            }
        }

        // Text label
        if (style.TextTag != null)
        {        
            Color textColor = style.TextColor ?? Color.Black;
            string fontName = style.TextFont ?? "Calibri";
            double fontSize = style.TextSize ?? canvas.Width * 0.003;
            foreach ((Coord[] coords, TagList tags) point in allPoints)
            {
                // TODO: Factor out TagList lookup?
                string? labelText = TagFilter.ValueOrNull(point.tags, style.TextTag);
                if (labelText == null) continue;

                double spacing = fontSize * 0.2; // spacing between point and edge of text
                Coord textSize = layer.GetTextSize(fontName, fontSize, labelText);

                // NOTE: Usually a single coord per array (since these
                // are text labels)
                foreach (Coord coord in point.coords)
                {
                    Coord? placement = GetLabelPlacement(coord, textSize, fontSize * 0.3);
                    if (placement != null)
                        layer.DrawText(fontName, fontSize, labelText, placement.Value, textColor);
                }
            }
        }
    }

    /// <param name="featureCoord">Feature centerpoint.</param>
    /// <param name="textSize">Measured size of text</param>
    /// <returns>
    /// The text centerpoint for the text label as placed by
    /// alignment priority and existing labels. Null if no suitable
    /// placement found.
    /// </returns>
    private Coord? GetLabelPlacement(Coord featureCoord, Coord textSize, double spacing)
    {
        // TODO: Pick more appropriate order?
        /* Current priority order:
         *   8 4 7
         *   2 * 1
         *   6 3 5
         */
        return
            GetLabelPlacement(featureCoord, textSize, spacing, TextHAlign.Left, TextVAlign.Center) ??
            GetLabelPlacement(featureCoord, textSize, spacing, TextHAlign.Right, TextVAlign.Center) ??
            GetLabelPlacement(featureCoord, textSize, spacing, TextHAlign.Center, TextVAlign.Top) ??
            GetLabelPlacement(featureCoord, textSize, spacing, TextHAlign.Center, TextVAlign.Bottom) ??
            GetLabelPlacement(featureCoord, textSize, spacing, TextHAlign.Left, TextVAlign.Top) ??
            GetLabelPlacement(featureCoord, textSize, spacing, TextHAlign.Right, TextVAlign.Top) ??
            GetLabelPlacement(featureCoord, textSize, spacing, TextHAlign.Left, TextVAlign.Bottom) ??
            GetLabelPlacement(featureCoord, textSize, spacing, TextHAlign.Right, TextVAlign.Bottom);
    }
    private Coord? GetLabelPlacement(Coord featureCoord, Coord textSize, double spacing,
        TextHAlign halign, TextVAlign valign)
    {
        double x = featureCoord.X, y = featureCoord.Y;
        if (halign == TextHAlign.Left) x += (textSize.X / 2 + spacing);
        if (halign == TextHAlign.Right) x -= (textSize.X / 2 + spacing);
        if (valign == TextVAlign.Bottom) y -= (textSize.Y / 2 + spacing);
        if (valign == TextVAlign.Top) y += (textSize.Y / 2 + spacing);
        Bounds bounds = new Bounds(
            x - 0.5 * textSize.X, x + 0.5 * textSize.X,
            y - 0.5 * textSize.Y, y + textSize.Y);
        if (PlacementManager.TryAdd([bounds]) != null)
            return new Coord(x, y);
        return null;
    }

    private void DrawRaster(Canvas canvas, RasterMapLayer mapLayer, RasterData2 data)
    {
        CanvasLayer layer = canvas.AddNewLayer(mapLayer.Name);

        Console.WriteLine("Map SRS: " + Srs);
        Console.WriteLine("Raster SRS: " + data.Srs);

        Console.WriteLine("Map bounds LL: " + this.BoundsToSrs(Epsg.Wgs84));
        Console.WriteLine("Raster bounds LL: " + data.BoundsToSrs(Epsg.Wgs84));

        Console.WriteLine("Map bounds: " + Bounds);
        Console.WriteLine("Raster bounds: " + data.BoundsToSrs(this.Srs));

        if (data is SingleBandRasterData)
        {
            throw new InvalidOperationException(
                "Single-band raster data cannot be rendered directly");
        }
        else if (data is ImageRasterData imageData)
        {
            Console.WriteLine("Bitmap pixel format: " +
                imageData.Bitmap.PixelFormat);

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

            layer.DrawBitmap(imageData.Bitmap,
                rasterBoundsInCanvasSrs.XMin, rasterBoundsInCanvasSrs.YMin,
                rasterBoundsInCanvasSrs.Width, rasterBoundsInCanvasSrs.Height,
                1.0);
        }
        else
        {
            throw new NotSupportedException("Unsupported raster data type");
        }
    }
}
