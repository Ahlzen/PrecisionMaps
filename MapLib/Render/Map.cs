using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Geometry.Helpers;
using MapLib.Output;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using MapLib.DataSources;

namespace MapLib.Render;

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

    public OrderedDictionary<string, BaseVectorDataSource> VectorDataSources { get; } = new();
    public OrderedDictionary<string, BaseRasterDataSource> RasterDataSources { get; } = new();

    public List<MapLayer> Layers { get; } = new();
    //private Dictionary<string, CanvasMask> Masks { get; } = new();
    private Dictionary<string, CanvasLayer> Masks { get; } = new();

    public ObjectPlacementManager PlacementManager { get; } = new(); 

    private double _scaleX, _scaleY;
    private double _offsetX, _offsetY;


    public Map(Bounds boundsWgs84, string mapSrs)
    {
        Srs = mapSrs;
        RequestedBoundsWgs84 = boundsWgs84;                       
    }

    public async void Render(Canvas canvas,
        AspectRatioMismatchStrategy ratioMismatchStrategy = AspectRatioMismatchStrategy.CenterOnCanvas)
    {
        ComputeActualBounds(canvas, ratioMismatchStrategy);
        
        // Render layer masks
        foreach (MapLayer layer in Layers)
        {
            if (layer is VectorMapLayer vectorLayer)
            {
                if (vectorLayer.Style.MaskName == null)
                    continue; // no mask

                // Find or create mask layer
                await RenderVectorMask(vectorLayer.Style.MaskName,
                    canvas, vectorLayer);
            }
        }

        // Render visible layers
        foreach (MapLayer layer in Layers)
        {
            if (layer is VectorMapLayer vectorLayer)
            {
                await RenderVectorLayer(canvas, vectorLayer);
            }
            else if (layer is RasterMapLayer rasterLayer)
            {
                await RenderRasterLayer(canvas, rasterLayer);
            }
            else
            {
                throw new NotSupportedException(
                    "Unsupported layer type: " + layer.GetType());
            }
        }
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

    private async Task RenderVectorLayer(Canvas canvas, VectorMapLayer vectorLayer)
    {
        var dataInCanvasSpace = await GetVectorDataForLayer(vectorLayer);
        CanvasLayer layer = DrawVectors(vectorLayer.Name,
            canvas, vectorLayer, dataInCanvasSpace, false);

        // Apply any mask(s)
        foreach (var maskName in vectorLayer.Style.MaskedBy)
        {
            if (!Masks.TryGetValue(maskName, out CanvasLayer? mask))
                throw new ApplicationException("Mask not found: " + maskName);
            layer.ApplyMask(mask);
        }
    }

    private async Task RenderVectorMask(string maskName,
        Canvas canvas, VectorMapLayer vectorLayer)
    {
        var dataInCanvasSpace = await GetVectorDataForLayer(vectorLayer);
        DrawVectors(maskName, canvas, vectorLayer, dataInCanvasSpace, true);
    }

    private async Task<VectorData> GetVectorDataForLayer(VectorMapLayer vectorLayer)
    {
        BaseVectorDataSource? dataSource =
            VectorDataSources.GetValueOrDefault(vectorLayer.DataSourceName);
        if (dataSource == null)
            throw new InvalidOperationException(
                $"Vector layer data source not found: \"{vectorLayer.DataSourceName}\"");

        using Transformer wgs84ToSourceTransformer = new(
            Epsg.Wgs84, dataSource.Srs);
        using Transformer sourceToMapTransformer = new(
            dataSource.Srs, Srs);
        Bounds dataSourceBounds = wgs84ToSourceTransformer.Transform(RequestedBoundsWgs84);

        VectorData data = await dataSource.GetData(dataSourceBounds, Srs);

        // Filter features (if applicable)
        if (vectorLayer.Filter != null)
            data = vectorLayer.Filter.Filter(data);

        // Transform to canvas space
        VectorData dataInCanvasSpace =
            data.Transform(_scaleX, _scaleY, _offsetX, _offsetY);
        return dataInCanvasSpace;
    }

    #region Vector helpers

    /// <returns>
    /// The resulting CanvasLayer.
    /// </returns>
    private CanvasLayer DrawVectors(
        string layerName,
        Canvas canvas, VectorMapLayer mapLayer,
        VectorData data, bool isMask)
    {
        CanvasLayer layer = canvas.AddNewLayer(layerName);
        VectorStyle style = mapLayer.Style;

        if (isMask)
        {
            layer.Clear(CanvasLayer.MaskBackgroundColor);
            Masks.Add(layerName, layer);
        }

        // Compute coordinates
        // TODO: Only enumerate these if we really need them
        // TODO: Use better polygon centroid algorithm (at least
        // some form of point-in-polygon!)
        GetVectorDataCoordinates(data, out var pointCoords, out var lineMidpoints, out var polygonCentroids, out var allPoints);

        if (isMask)
        {
            if (style.LineMaskWidth != null)
                Stroke(canvas, layer, data.Lines, data.MultiLines, null, null,
                    CanvasLayer.MaskColor, style.LineWidth + style.LineMaskWidth * 2);
            if (style.PolygonMaskWidth != null)
                Stroke(canvas, layer, null, null, data.Polygons, data.MultiPolygons,
                    CanvasLayer.MaskColor, style.PolygonMaskWidth * 2);
            if (style.SymbolMaskWidth != null)
                DrawSymbols(canvas, layer, allPoints, style.Symbol,
                    style.SymbolSize + style.SymbolMaskWidth * 2, style.SymbolColor);
            if (style.TextMaskWidth != null)
                DrawTextLabels(canvas, layer, allPoints, style, style.TextMaskWidth * 2);
        }
        else
        {
            Fill(canvas, layer, data.Polygons, data.MultiPolygons, style.FillColor);
            Stroke(canvas, layer, data.Lines, data.MultiLines, data.Polygons, data.MultiPolygons, style.LineColor, style.LineWidth);
            DrawSymbols(canvas, layer, allPoints, style.Symbol, style.SymbolSize, style.SymbolColor);
            DrawTextLabels(canvas, layer, allPoints, style);
        }

        return layer;
    }

    //public void DrawVectorMask(Canvas canvas, VectorMapLayer mapLayer, VectorData data, boo)
    //{
    //    VectorStyle style = mapLayer.Style;

    //    string? maskName = mapLayer.Style.MaskName;
    //    if (maskName == null)
    //        return;

    //    CanvasMask mask = canvas.AddNewMask(maskName);

    //    // TODO: DRY. Only compute these once per layer/source
    //    GetVectorDataCoordinates(data, out var pointCoords, out var lineMidpoints, out var polygonCentroids, out var allPoints);


    //}

    private static void GetVectorDataCoordinates(VectorData data,
        out IEnumerable<(Coord[] coords, TagList tags)> pointCoords,
        out IEnumerable<(Coord[] coords, TagList tags)> lineMidpoints,
        out IEnumerable<(Coord[] coords, TagList tags)> polygonCentroids,
        out IEnumerable<(Coord[] coords, TagList tags)> allPoints)
    {
        pointCoords = data.Points.Select(p => (new[] { p.Coord }, p.Tags))
            .Union(data.MultiPoints.Select(mp => (new[] { mp.GetBounds().Center }, mp.Tags)));
        lineMidpoints = data.Lines.Select(l => (new[] { l.GetMidpoint() }, l.Tags))
            .Union(data.MultiLines.Select(ml => (new[] { ml.GetBounds().Center }, ml.Tags)));
        polygonCentroids = data.Polygons.Select(p => (new[] { PolyLabel.Calculate(p) }, p.Tags))
            .Union(data.MultiPolygons.Select(mp => (new[] { PolyLabel.Calculate(mp) }, mp.Tags)));
        allPoints = pointCoords.Union(lineMidpoints).Union(polygonCentroids);
    }

    private static void Fill(
        Canvas canvas, CanvasLayer layer,
        IEnumerable<Polygon>? polygons,
        IEnumerable<MultiPolygon>? multiPolygons,
        Color? fillColor)
    {
        if (fillColor == null) return;

        if (polygons != null)
            foreach (Polygon p in polygons)
                layer.DrawFilledPolygon(p.Coords, fillColor.Value);
        if (multiPolygons != null)
            foreach (MultiPolygon mp in multiPolygons)
                layer.DrawFilledMultiPolygon(mp.Coords, fillColor.Value);
    }

    private static void Stroke(
        Canvas canvas, CanvasLayer layer,
        IEnumerable<Line>? lines,
        IEnumerable<MultiLine>? multiLines,
        IEnumerable<Polygon>? polygons,
        IEnumerable<MultiPolygon>? multiPolygons,
        Color? lineColor, double? lineWidth)
        //IEnumerable<(Coord[] coords, TagList tags)> pointCoords)

    {
        if (lineColor == null || lineWidth == null) return;

        //// Points
        //layer.DrawFilledCircles(pointCoords.SelectMany(c => c.coords),
        //    lineWidth.Value, lineColor.Value);

        // Lines
        if (lines != null)
            foreach (Line l in lines)
                layer.DrawLine(l.Coords, lineWidth.Value, lineColor.Value);
        if (multiLines != null)
            foreach (MultiLine ml in multiLines)
                layer.DrawLines(ml.Coords, lineWidth.Value, lineColor.Value);

        // Polygons
        if (polygons != null)
            foreach (Polygon p in polygons)
                layer.DrawLine(p.Coords, lineWidth.Value, lineColor.Value);
        if (multiPolygons != null)
            foreach (MultiPolygon mp in multiPolygons)
                layer.DrawLines(mp.Coords, lineWidth.Value, lineColor.Value);
    }

    private void DrawSymbols(
        Canvas canvas, CanvasLayer layer,
        IEnumerable<(Coord[] coords, TagList tags)> allPoints,
        SymbolType? symbolType, double? symbolSize, Color? symbolFillColor)
    {
        if (symbolType == null) return;
        double actualSymbolSize = symbolSize ?? canvas.Width * 0.001; // default size
        Color actualSymbolFillColor = symbolFillColor ?? Color.Black; // default color

        //if (style.Symbol == null) return;
        //SymbolType? symbolType = style.Symbol;
        //double symbolSize = style.SymbolSize ?? canvas.Width * 0.001;
        //Color symbolFillColor = style.SymbolColor ?? Color.Black;

        switch (symbolType)
        {
            case SymbolType.Circle:
                layer.DrawFilledCircles(allPoints.SelectMany(c => c.coords),
                    actualSymbolSize/2, actualSymbolFillColor);
                foreach (Coord point in allPoints.SelectMany(p => p.coords))
                    PlacementManager.TryAdd([new Bounds(
                        point.X-actualSymbolSize/2, point.X+actualSymbolSize/2,
                        point.Y-actualSymbolSize/2, point.Y+actualSymbolSize/2)]);
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

    private void DrawTextLabels(Canvas canvas, CanvasLayer layer,
        IEnumerable<(Coord[] coords, TagList tags)> allPoints,
        VectorStyle style, double? outlineWidth = null)
    {
        if (style.TextTag == null)  return;

        Color textColor = style.TextColor ?? Color.Black;
        string fontName = style.TextFont ?? "Calibri";
        double fontSize = style.TextSize ?? canvas.Width * 0.003;
        foreach ((Coord[] coords, TagList tags) point in allPoints)
        {
            // TODO: Factor out TagList lookup?
            string? labelText = TagFilter.ValueOrNull(point.tags, style.TextTag);
            if (labelText == null) continue;

            double spacing = fontSize * 0.3; // spacing between point and edge of text
            Coord textSize = layer.GetTextSize(fontName, fontSize, labelText);

            // NOTE: Usually a single coord per array (since these
            // are text labels)
            foreach (Coord coord in point.coords)
            {
                Coord? placement = GetLabelPlacement(coord, textSize, spacing);
                if (placement != null)
                {
                    layer.DrawText(fontName, fontSize, labelText, placement.Value, textColor);
                    if (outlineWidth != null)
                        layer.DrawTextOutline(fontName, fontSize, labelText, placement.Value,
                            textColor, outlineWidth.Value);
                }
            }
        }
    }

    #endregion

    #region Label placement

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

    #endregion

    #region Raster helpers

    private async Task RenderRasterLayer(Canvas canvas, RasterMapLayer rasterLayer)
    {
        BaseRasterDataSource? dataSource =
            RasterDataSources.GetValueOrDefault(rasterLayer.DataSourceName);
        if (dataSource == null)
            throw new InvalidOperationException(
                $"Raster layer data source not found: \"{rasterLayer.DataSourceName}\"");

        RasterData data = await dataSource.GetData(Srs);
        DrawRaster(canvas, rasterLayer, data);
    }

    private void DrawRaster(Canvas canvas, RasterMapLayer mapLayer, RasterData data)
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

    #endregion
}
