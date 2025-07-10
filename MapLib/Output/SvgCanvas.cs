using MapLib.Geometry;
using MapLib.Util;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace MapLib.Output;

/// <remarks>
/// NOTE: Since the SVG coordinate system has positive Y down,
/// and our coordinates are positive Y up, drawing operations have to
/// flip (offset and negate) the Y coordinate.
/// </remarks>
public class SvgCanvas : Canvas, IDisposable
{
    private readonly SvgCanvasStack _stack;
    private readonly List<XObject> _objects = new();
    
    private double Height => _stack.Height;
    private double Width => _stack.Width;

    private List<string> _maskedBy = new();

    /// <summary>
    /// Hack! Needed for certain operations, like measuring text.
    /// </summary>
    private readonly Graphics _graphics;
    private readonly Bitmap _bitmap;

    internal SvgCanvas(SvgCanvasStack stack)
    {
        _stack = stack;
        
        _bitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        _graphics = Graphics.FromImage(_bitmap);
        _graphics.PageUnit = GraphicsUnit.Pixel;
        _graphics.SmoothingMode = SmoothingMode.HighQuality;
    }

    public override void Dispose() {
        _bitmap.Dispose();
        _graphics.Dispose();
    }

    internal XElement GetSvgData()
    {
        return new XElement(SvgCanvasStack.XmlNs + "g",
            new XAttribute("id", Name ?? ""),
            // NOTE: This supports only the first mask
            // TODO: support combined masks
            // https://www.reddit.com/r/svg/comments/1cm6vc6/how_to_combine_two_svg_masks/
            new XAttribute("mask", _maskedBy.Any() ? $"url(#{_maskedBy[0]})" : ""),
            _objects);
    }

    internal XElement GetMaskData()
    {
        return new XElement(SvgCanvasStack.XmlNs + "mask",
            new XAttribute("id", Name ?? ""),
            _objects);
    }

    public override void Clear(Color color)
    {
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            SvgCanvasStack.Clear(color)));
    }

    public override void DrawBitmap(Bitmap bitmap,
        double x, double y, double width, double height, double opacity)
    {
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            GetRasterAttributes(opacity),
            GetRasterData(bitmap, x, Height - y - height, width, height)));
    }

    public override void DrawLine(Coord[] coords, double width,
        Color color, LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Miter, double[]? dasharray = null)
    {
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            GetStrokeAttributes(width, color, cap, join, dasharray),
            GetSvgPath(coords, isClosedPath: false)));
    }

    public override void DrawLines(IEnumerable<Coord[]> lines, double width,
        Color color, LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Miter, double[]? dasharray = null)
    {
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            GetStrokeAttributes(width, color, cap, join, dasharray),
            lines.Select(l => GetSvgPath(l, isClosedPath: false))));
    }

    public override void DrawPolygon(Coord[] coords, double width,
        Color color, LineJoin join = LineJoin.Miter, double[]? dasharray = null)
    {
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            GetStrokeAttributes(width, color,
                LineCap.Square, // doesn't matter since shape is closed
                join, dasharray),
            GetSvgPath(coords, isClosedPath: true)));
    }

    public override void DrawFilledPolygon(Coord[] coords, Color color)
    {
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            GetFillAttributes(color),
            GetSvgPath(coords, isClosedPath: true)));
    }

    public override void DrawFilledPolygons(
        IEnumerable<Coord[]> polygons, Color color)
    {
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            GetFillAttributes(color),
            // one path per polygon
            polygons.Select(polygon => GetSvgPath(polygon, isClosedPath: true))));
    }

    public override void DrawFilledMultiPolygon(
        IEnumerable<Coord[]> multiPolygon, Color color)
    {
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            GetFillAttributes(color),
            // single multi-segment path for multipolygon
            GetSvgPath(multiPolygon, areClosedPaths: true)));
    }

    public override void DrawFilledMultiPolygons(
        IEnumerable<IEnumerable<Coord[]>> multipolygons, Color color)
    {
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            GetFillAttributes(color),
            // one multi-segment path for each multipolygon
            multipolygons.Select(multipolygon => GetSvgPath(
                multipolygon, areClosedPaths: true))));
    }

    public override void DrawCircles(IEnumerable<Coord> coords,
        double radius, double lineWidth, Color color)
    {
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            GetStrokeAttributes(lineWidth, color),
            coords.Select(point => GetSvgCircle(point, radius))));
    }

    public override void DrawFilledCircles(
        IEnumerable<Coord> coords, double radius, Color color)
    {
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            GetFillAttributes(color),
            coords.Select(point => GetSvgCircle(point, radius))));
    }


    // TODO: Move to base class?
    private Font GetFont(string fontName, double emSize)
    {
        return new Font(fontName, (float)emSize);
    }

    // TODO: Move to base class?
    public override Coord GetTextSize(string fontName, double emSize, string s)
    {
        // TODO: optimize. Cache Font?
        using Font font = GetFont(fontName, emSize);
        SizeF stringSize = _graphics.MeasureString(s, font);
        return new Coord(stringSize.Width, stringSize.Height);
    }

    public override void DrawText(string fontName, double emSize,
        string s, Coord centerCoord, Color color)
    {
        double svgTextSize = emSize * SvgCanvasStack.TextScaleFactor; // see remarks above
        string sizeStr = svgTextSize.ToString();

        // x/y are at baseline, according to the specified anchor
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "text",
            new XAttribute("font", fontName),
            new XAttribute("fill", color.ToHexCode()),
            new XAttribute("x", centerCoord.X.ToString(_stack.SvgCoordFormat)),
            new XAttribute("y", (Height - centerCoord.Y).ToString(_stack.SvgCoordFormat)),
            new XAttribute("style", $"font-size: {sizeStr}; text-anchor: middle; dominant-baseline: central;"),
            new XText(s)
            ));
    }

    public override void DrawTextOutline(string fontName, double emSize,
        string s, Coord centerCoord, Color color,
        double lineWidth, LineJoin join = LineJoin.Miter)
    {
        double svgTextSize = emSize * SvgCanvasStack.TextScaleFactor; // see remarks above
        string sizeStr = svgTextSize.ToString();

        // x/y are at baseline, according to the specified anchor
        var strokeAttributes = GetStrokeAttributes(
            lineWidth, color, LineCap.Butt, join);
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "text",
            strokeAttributes,
            new XAttribute("font", fontName),
            new XAttribute("x", centerCoord.X.ToString(_stack.SvgCoordFormat)),
            new XAttribute("y", (Height - centerCoord.Y).ToString(_stack.SvgCoordFormat)),
            new XAttribute("style", $"fill: none; font-size: {sizeStr}; text-anchor: middle; dominant-baseline: central;"),
            new XText(s)
            ));
    }

    public override void ApplyMasks(IList<Canvas> maskSources)
    {
        foreach (var maskSource in maskSources)
            ApplyMask(maskSource);
    }

    private void ApplyMask(Canvas maskSource)
    {
        // Wrap current objects in a group with the mask applied
        // (for multiple masks, this may be a nested set)

        List<XObject> children = _objects.ToList();
        _objects.Clear();
        _objects.Add(new XElement(SvgCanvasStack.XmlNs + "g",
            new XAttribute("mask", $"url(#{maskSource.Name})"),
            children
        ));
    }

    #region SVG Raster data

    private IEnumerable<XAttribute> GetRasterAttributes(double opacity)
    {
        if (opacity < 1.0)
        {
            yield return new XAttribute("opacity",
                opacity.ToString(_stack.SvgCoordFormat));
        }
    }

    private XElement GetRasterData(Bitmap bitmap,
        double x, double y, double width, double height)
    {
        return new XElement(SvgCanvasStack.XmlNs + "image",
            new XAttribute("x", x.ToString(_stack.SvgCoordFormat)),
            new XAttribute("y", y.ToString(_stack.SvgCoordFormat)),
            new XAttribute("width", width.ToString(_stack.SvgCoordFormat)),
            new XAttribute("height", height.ToString(_stack.SvgCoordFormat)),
            new XAttribute(SvgCanvasStack.XmlNsXlink + "href", "data:image/png;base64," +
                GetBase64PngData(bitmap)
            ));
    }

    private string GetBase64PngData(Bitmap bitmap)
    {
        using (var stream = new MemoryStream())
        {
            bitmap.Save(stream, ImageFormat.Png);
            return Convert.ToBase64String(stream.GetBuffer());
        }
    }

    #endregion

    #region SVG Vector data

    private IEnumerable<XAttribute> GetStrokeAttributes(
        double width, Color color,
        LineCap cap = LineCap.Butt, LineJoin join = LineJoin.Miter,
        double[]? dasharray = null)
    {
        yield return new XAttribute("fill", "none");
        if (color.A != 255)
        {
            // transparent or semi-transparent stroke
            yield return new XAttribute("stroke", Color.FromArgb(255, color).ToHexCode());
            yield return new XAttribute("stroke-opacity", color.A / 255.0);
        }
        else
        {
            yield return new XAttribute("stroke", color.ToHexCode());
        }
        yield return new XAttribute("stroke-width", width);

        switch (cap)
        {
            case LineCap.Butt: break; // butt is default
            case LineCap.Square: yield return new XAttribute("stroke-linecap", "square"); break;
            case LineCap.Round: yield return new XAttribute("stroke-linecap", "round"); break;
        }
        switch (join)
        {
            case LineJoin.Miter: break; // miter is default
            case LineJoin.Bevel: yield return new XAttribute("stroke-linejoin", "bevel"); break;
            case LineJoin.Round: yield return new XAttribute("stroke-linejoin", "round"); break;
        }
        if (dasharray != null)
            yield return new XAttribute("stroke-dasharray",
                string.Join(",", dasharray.Select(d => d.ToString(_stack.SvgCoordFormat))));
    }

    private IEnumerable<XAttribute> GetFillAttributes(Color color)
    {
        if (color.A != 255)
        {
            // transparent or semi-transparent fill
            yield return new XAttribute("fill", Color.FromArgb(255, color).ToHexCode());
            yield return new XAttribute("fill-opacity", color.A / 255.0);
        }
        else
        {
            yield return new XAttribute("fill", color.ToHexCode());
        }
        yield return new XAttribute("fill-rule", "evenodd"); // for mutipolygons etc
    }

    #endregion

    #region SVG Utils

    private XElement GetSvgPath(Coord[] points, bool isClosedPath)
    {
        return new XElement(SvgCanvasStack.XmlNs + "path",
            new XAttribute("d", GetSvgPathCoords(points, isClosedPath)));
    }

    private XElement GetSvgPath(IEnumerable<Coord[]> paths, bool areClosedPaths)
    {
        return new XElement(SvgCanvasStack.XmlNs + "path",
            new XAttribute("d", GetSvgPathCoords(paths, areClosedPaths)));
    }

    private string GetSvgPathCoords(IEnumerable<Coord[]> paths, bool areClosedPaths)
    {
        var sb = new StringBuilder();
        foreach (Coord[] path in paths)
        {
            AddSvgPathCoords(sb, path);
            sb.Append(" ");
        }
        return sb.ToString().TrimEnd();
    }

    private string GetSvgPathCoords(Coord[] coords, bool isClosedPath)
    {
        var sb = new StringBuilder();
        AddSvgPathCoords(sb, coords);
        return sb.ToString();
    }

    private void AddSvgPathCoords(StringBuilder sb, Coord[] coords)
    {
        bool isFirst = true;
        foreach (Coord coord in coords)
        {
            sb.Append(isFirst ? "M " : " L ");
            sb.Append(coord.X.ToString(_stack.SvgCoordFormat));
            sb.Append(" ");
            sb.Append((Height - coord.Y).ToString(_stack.SvgCoordFormat));
            isFirst = false;
        }

        bool isClosedPath = coords[0] == coords[^1];
        if (isClosedPath)
          sb.Append(" Z");
    }


    private XElement GetSvgCircle(Coord point, double radius)
    {
        return new XElement(SvgCanvasStack.XmlNs + "circle",
            new XAttribute("cx", point.X.ToString(_stack.SvgCoordFormat)),
            new XAttribute("cy", (Height-point.Y).ToString(_stack.SvgCoordFormat)),
            new XAttribute("r", radius.ToString(_stack.SvgCoordFormat))
            );
    }




    #endregion
}