using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Geometry.Helpers;
using MapLib.Output;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using MapLib.DataSources;
using System.Reflection.Metadata;

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
    public Srs Srs { get; set; }

    public OrderedDictionary<string, BaseVectorDataSource> VectorDataSources { get; } = new();
    public OrderedDictionary<string, BaseRasterDataSource> RasterDataSources { get; } = new();

    public List<MapLayer> MapLayers { get; } = new();

    public ObjectOverlapManager OverlapManager { get; } = new();

    private double _scaleX, _scaleY;
    private double _offsetX, _offsetY;


    public Map(Bounds boundsWgs84, Srs mapSrs)
    {
        Srs = mapSrs;
        RequestedBoundsWgs84 = boundsWgs84;                       
    }

    public async Task Render(CanvasStack stack,
        AspectRatioMismatchStrategy ratioMismatchStrategy = AspectRatioMismatchStrategy.CenterOnCanvas)
    {
        ComputeActualBounds(stack, ratioMismatchStrategy);

        // Render all layers
        foreach (MapLayer layer in MapLayers)
        {
            // Add main layer canvas
            Canvas canvas = stack.AddNewLayer(layer.Name);

            if (layer is VectorMapLayer vectorLayer)
            {
                // Get or add mask canvas (if applicable)
                string? maskName = vectorLayer.Style.MaskName;
                Canvas? mask = null;
                if (maskName != null)
                {
                    if (!stack.Masks.ContainsKey(maskName))
                        stack.AddNewMask(maskName);
                    mask = stack.Masks[maskName];
                }

                // Draw vectors
                VectorData dataInCanvasSpace = await GetVectorDataForLayer(vectorLayer);
                DrawVectors(dataInCanvasSpace, canvas, mask, vectorLayer.Style);
            }
            else if (layer is RasterMapLayer rasterLayer)
            {
                RasterData rasterData = await GetRasterDataForLayer(rasterLayer);
                DrawRaster(canvas, rasterLayer.Style, rasterData);
            }
        }

        // Apply masks
        foreach (MapLayer layer in MapLayers)
        {
            if (layer is VectorMapLayer vectorLayer)
            {
                Canvas canvas = stack.GetLayer(layer.Name);
                IList<Canvas> masks = stack.GetMasks(vectorLayer.Style.MaskedBy);
                canvas.ApplyMasks(masks);
            }
        }
    }

    #region Data and projections

    private void ComputeActualBounds(
        CanvasStack canvas,
        AspectRatioMismatchStrategy strategy)
    {
        Transformer wgs84ToMapSrs = new(Srs.Wgs84, this.Srs);
        Transformer mapSrsToWgs84 = new(this.Srs, Srs.Wgs84);

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

    #endregion
   
    private async Task<VectorData> GetVectorDataForLayer(VectorMapLayer vectorLayer)
    {
        BaseVectorDataSource? dataSource =
            VectorDataSources.GetValueOrDefault(vectorLayer.DataSourceName);
        if (dataSource == null)
            throw new InvalidOperationException(
                $"Vector layer data source not found: \"{vectorLayer.DataSourceName}\"");

        using Transformer wgs84ToSourceTransformer = new(
            Srs.Wgs84, dataSource.Srs);
        using Transformer sourceToMapTransformer = new(
            dataSource.Srs, Srs);
        Bounds dataSourceBounds = wgs84ToSourceTransformer.Transform(RequestedBoundsWgs84);

        // TODO: This transform doesn't seem to work. Fix.
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

    private void DrawVectors(
        VectorData data, // in canvas space
        Canvas canvas, Canvas? mask,
        VectorStyle style)
    {
        // Compute coordinates
        // TODO: Only enumerate these once per data source,
        // and only if we really need them
        GetVectorDataCoordinates(data,
            out var pointCoords, out var lineMidpoints,
            out var polygonCentroids, out var allPoints);

        Fill(canvas, mask, data.Polygons, data.MultiPolygons, style.FillColor,
            style.PolygonMaskWidth);
        Stroke(canvas, mask, data.Lines, data.MultiLines, data.Polygons,
            data.MultiPolygons, style.LineColor, style.LineWidth, style.LineMaskWidth);
        DrawSymbols(canvas, mask, OverlapManager, allPoints, style.Symbol,
            style.SymbolSize, style.SymbolColor, style.SymbolMaskWidth);
        DrawTextLabels(canvas, mask, OverlapManager, allPoints, style.Symbol, style.TextTag,
            style.TextColor, style.TextFont, style.TextSize, null, null, style.TextMaskWidth);
    }

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
        Canvas layer, Canvas? mask,
        IEnumerable<Polygon>? polygons,
        IEnumerable<MultiPolygon>? multiPolygons,
        Color? fillColor,
        double? maskWidth = null)
    {
        if (fillColor == null) return;

        if (polygons != null) {
            foreach (Polygon p in polygons) {
                layer.DrawFilledPolygon(p.Coords, fillColor.Value);
                mask?.DrawFilledPolygon(p.Coords, Canvas.MaskColor);
                mask?.DrawLine(p.Coords, (maskWidth ?? 0) * 2, Canvas.MaskColor);
            }}
        if (multiPolygons != null) {
            foreach (MultiPolygon mp in multiPolygons) {
                layer.DrawFilledMultiPolygon(mp.Coords, fillColor.Value);
                mask?.DrawFilledMultiPolygon(mp.Coords, Canvas.MaskColor);
                mask?.DrawLines(mp.Coords, (maskWidth ?? 0) * 2, Canvas.MaskColor);
            }}
    }

    private static void Stroke(
        Canvas layer, Canvas? mask,
        IEnumerable<Line>? lines,
        IEnumerable<MultiLine>? multiLines,
        IEnumerable<Polygon>? polygons,
        IEnumerable<MultiPolygon>? multiPolygons,
        Color? lineColor, double? lineWidth, double? maskWidth = null)
    {
        if (lineColor == null || lineWidth == null) return;

        // Lines
        if (lines != null) {
            foreach (Line l in lines) {
                layer.DrawLine(l.Coords, lineWidth.Value, lineColor.Value);
                mask?.DrawLine(l.Coords, (maskWidth ?? 0) * 2, Canvas.MaskColor);
            }}
        if (multiLines != null) {
            foreach (MultiLine ml in multiLines) {
                layer.DrawLines(ml.Coords, lineWidth.Value, lineColor.Value);
                mask?.DrawLines(ml.Coords, (maskWidth ?? 0) * 2, Canvas.MaskColor);
            }
        }

        // Polygons
        if (polygons != null) { 
            foreach (Polygon p in polygons) {
                layer.DrawLine(p.Coords, lineWidth.Value, lineColor.Value);
                mask?.DrawLine(p.Coords, (maskWidth ?? 0) * 2, Canvas.MaskColor);
            }}
        if (multiPolygons != null) {
            foreach (MultiPolygon mp in multiPolygons) {
                layer.DrawLines(mp.Coords, lineWidth.Value, lineColor.Value);
                mask?.DrawLines(mp.Coords, (maskWidth ?? 0) * 2, Canvas.MaskColor);
            }}
    }

    private void DrawSymbols(Canvas canvas, Canvas? mask,
        ObjectOverlapManager overlapManager,
        IEnumerable<(Coord[] coords, TagList tags)> allPoints,
        SymbolType? symbolType, double? symbolSize, Color? symbolFillColor,
        double? maskWidth = null)
    {
        if (symbolType == null) return;
        double effectiveSymbolSize = symbolSize ?? canvas.Width * 0.001; // default size
        double radius = effectiveSymbolSize / 2;
        Color effectiveSymbolFillColor = symbolFillColor ?? Color.Black; // default color

        switch (symbolType)
        {
            case SymbolType.Circle:
                foreach (Coord point in allPoints.SelectMany(p => p.coords))
                {
                    Bounds? bounds = overlapManager.TryAdd([new Bounds(
                        point.X-radius, point.X+radius,
                        point.Y-radius, point.Y+radius)]);
                    if (bounds == null)
                        continue; // failed to place point (likely overlap)
                    canvas.DrawFilledCircle(point, radius, effectiveSymbolFillColor);
                    mask?.DrawFilledCircle(point, radius + (maskWidth ?? 0), Canvas.MaskColor);
                }
                break;
            case SymbolType.Square:
                throw new NotImplementedException();
            case SymbolType.Star:
                throw new NotImplementedException();
            case SymbolType.Image:
                throw new NotImplementedException();
        }
    }

    private void DrawTextLabels(Canvas canvas, Canvas? mask,
        ObjectOverlapManager overlapManager,
        IEnumerable<(Coord[] coords, TagList tags)> allPoints,
        SymbolType? symbolType, string? textTag, Color? textColor, string? fontName,
        double? fontSize, Color? outlineColor = null, double? outlineWidth = null,
        double? maskWidth = null)
    {
        if (textTag == null) return;

        Color effectiveTextColor = textColor ?? Color.Black; // default color
        string effectiveFontName = fontName ?? "Calibri"; // default font
        double effectiveFontSize = fontSize ?? canvas.Width * 0.003; // default size
        bool hasSymbol = symbolType != null;

        foreach ((Coord[] coords, TagList tags) point in allPoints)
        {
            // TODO: Factor out TagList lookup?
            string? labelText = TagFilter.ValueOrNull(point.tags, textTag);
            if (labelText == null) continue;

            double spacing = effectiveFontSize * 0.3; // spacing between point and edge of text
            Coord textSize = canvas.GetTextSize(effectiveFontName, effectiveFontSize, labelText);

            // NOTE: Usually a single coord per array (since these
            // are text labels)
            foreach (Coord coord in point.coords)
            {
                Coord? placement = GetLabelPlacement(overlapManager, coord, textSize, spacing, hasSymbol);
                if (placement != null)
                {
                    canvas.DrawText(effectiveFontName, effectiveFontSize, labelText, placement.Value, effectiveTextColor);
                    mask?.DrawText(effectiveFontName, effectiveFontSize, labelText, placement.Value, Canvas.MaskColor);
                    if (outlineColor != null && outlineWidth != null)
                    {
                        canvas.DrawTextOutline(effectiveFontName, effectiveFontSize, labelText, placement.Value,
                            outlineColor.Value, outlineWidth.Value * 2);
                        mask?.DrawTextOutline(effectiveFontName, effectiveFontSize, labelText, placement.Value,
                            Canvas.MaskColor, outlineWidth.Value * 2);
                    }
                    if (maskWidth != null)
                    {
                        mask?.DrawTextOutline(effectiveFontName, effectiveFontSize, labelText, placement.Value,
                            Canvas.MaskColor, ((outlineWidth ?? 0) + (maskWidth ?? 0)) * 2);
                    }
                }
            }
        }
    }

    #endregion

    #region Label placement

    /// <param name="featureCoord">Feature centerpoint.</param>
    /// <param name="textSize">Measured size of text</param>
    /// <param name="hasSymbol">
    /// If true, center placement (where the symbol is assumed to be) is avoided.
    /// Typically true for point features and false for polygons/areas.
    /// </param>
    /// <returns>
    /// The text centerpoint for the text label as placed by
    /// alignment priority and existing labels. Null if no suitable
    /// placement found.
    /// </returns>
    private Coord? GetLabelPlacement(ObjectOverlapManager overlapManager,
        Coord featureCoord, Coord textSize, double spacing, bool hasSymbol)
    {
        /* Current priority order:
         *  With symbol:
         *   8 4 7
         *   2 * 1
         *   6 3 5
         *  Without symbol:
         *   8 3 9
         *   4 1 5
         *   6 2 7
         */
        if (hasSymbol)
            return
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Left, TextVAlign.Center) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Right, TextVAlign.Center) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Center, TextVAlign.Top) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Center, TextVAlign.Bottom) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Left, TextVAlign.Top) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Right, TextVAlign.Top) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Left, TextVAlign.Bottom) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Right, TextVAlign.Bottom);
        else
            return
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Center, TextVAlign.Center) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Center, TextVAlign.Top) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Center, TextVAlign.Bottom) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Right, TextVAlign.Center) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Left, TextVAlign.Center) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Right, TextVAlign.Top) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Left, TextVAlign.Top) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Right, TextVAlign.Bottom) ??
                GetLabelPlacement(overlapManager, featureCoord, textSize, spacing, TextHAlign.Left, TextVAlign.Bottom);
    }
    private Coord? GetLabelPlacement(ObjectOverlapManager overlapManager,
        Coord featureCoord, Coord textSize, double spacing,
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
        if (overlapManager.TryAdd([bounds]) != null)
            return new Coord(x, y);
        return null;
    }

    #endregion

    #region Raster helpers

    /// <summary>
    /// Returns the raster data for the given raster layer (projected ot map SRS).
    /// </summary>
    private async Task<RasterData> GetRasterDataForLayer(RasterMapLayer rasterLayer)
    {
        BaseRasterDataSource? dataSource =
            RasterDataSources.GetValueOrDefault(rasterLayer.DataSourceName);
        if (dataSource == null)
            throw new InvalidOperationException(
                $"Raster layer data source not found: \"{rasterLayer.DataSourceName}\"");

        RasterData data = await dataSource.GetData(Srs);
        return data;
    }

    private void DrawRaster(Canvas canvas, RasterStyle style, RasterData data)
    {
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

            canvas.DrawBitmap(imageData.Bitmap,
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
