using MapLib.Geometry;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Drawing.Drawing2D;

namespace MapLib.Output;

public class SvgCanvas : Canvas
{
    internal static readonly XNamespace XmlNs = XNamespace.Get("http://www.w3.org/2000/svg");
    internal static readonly XNamespace XmlNsXlink = XNamespace.Get("http://www.w3.org/1999/xlink");

    private readonly double _width;
    private readonly double _height;
    private readonly Color _backgroundColor;
    private readonly List<SvgCanvasLayer> _layers = new List<SvgCanvasLayer>();
    internal string SvgCoordFormat { get; }

    public SvgCanvas(CanvasUnit unit, double width, double height,
        Color? backgroundColor, int decimals = 3) :
        base(unit, width, height)
    {
        _width = width;
        _height = height;
        _backgroundColor = backgroundColor ?? Color.Transparent;
        SvgCoordFormat = "F" + decimals;
    }

    public override void Dispose()
    {
        foreach (SvgCanvasLayer layer in _layers)
            layer.Dispose();
        _layers.Clear();
    }

    public override IEnumerable<CanvasLayer> Layers => _layers;
    public override int LayerCount => _layers.Count;

    public override CanvasLayer AddNewLayer(string name)
    {
        var layer = new SvgCanvasLayer(this);
        layer.Name = name;
        _layers.Add(layer);
        return layer;
    }

    public override void RemoveLayer(CanvasLayer layer)
    {
        SvgCanvasLayer? svgCanvasLayer = layer as SvgCanvasLayer;
        if (svgCanvasLayer == null) return;
        _layers.Remove(svgCanvasLayer);
    }

    public string GetSvg() => GetSvgData().ToString();
    public void SaveSvg(string filename) => GetSvgData().Save(filename);

    private XDocument GetSvgData()
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(XmlNs + "svg",
                new XAttribute("xmlns", XmlNs),
                new XAttribute(XNamespace.Xmlns + "xlink", XmlNsXlink),
                new XAttribute("width", _width.ToString(SvgCoordFormat)),
                new XAttribute("height", _height.ToString(SvgCoordFormat)),
                Clear(_backgroundColor),
                _layers.Select(layer => layer.GetSvgData())));
    }

    private IEnumerable<XElement> Clear(Color color)
    {
        if (color == Color.Transparent) yield break;
        yield return new XElement(SvgCanvas.XmlNs + "rect",
            new XAttribute("width", "100%"),
            new XAttribute("height", "100%"),
            new XAttribute("fill", color.ToHexCode()));
    }

    public override string DefaultFileExtension => ".svg";

    public override void SaveToFile(string filename)
    {
        string svg = GetSvg();
        File.WriteAllText(filename, svg);
    }
}

/// <remarks>
/// NOTE: Since the SVG coordinate system has positive Y down,
/// and our coordinates are positive Y up, drawing operations have to
/// flip (offset and negate) the Y coordinate.
/// </remarks>
public class SvgCanvasLayer : CanvasLayer, IDisposable
{
    private SvgCanvas _canvas;
    private List<XObject> _objects = new List<XObject>();
    private double _layerHeight;
    private double _layerWidth;

    /// <summary>
    /// Hack! Needed for certain operations, like measuring text.
    /// </summary>
    private readonly Graphics _graphics;
    private readonly Bitmap _bitmap;

    internal SvgCanvasLayer(SvgCanvas canvas)
    {
        _canvas = canvas;
        _layerWidth = canvas.Width;
        _layerHeight = canvas.Height;

        _bitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        _graphics = Graphics.FromImage(_bitmap);
        _graphics.SmoothingMode = SmoothingMode.HighQuality;
    }

    public override void Dispose() {
        _bitmap.Dispose();
        _graphics.Dispose();
    }

    internal XElement GetSvgData()
    {
        return new XElement(SvgCanvas.XmlNs + "g",
            new XAttribute("id", Name ?? ""),
            _objects);
    }

    public override void DrawBitmap(Bitmap bitmap,
        double x, double y, double width, double height, double opacity)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetRasterAttributes(opacity),
            GetRasterData(bitmap, x, _layerHeight - y - height, width, height)));
    }

    public override void DrawLine(Coord[] coords, double width,
        Color color, LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Miter, double[]? dasharray = null)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetStrokeAttributes(width, color, cap, join, dasharray),
            GetSvgPath(coords, isClosedPath: false)));
    }

    public override void DrawLines(IEnumerable<Coord[]> lines, double width,
        Color color, LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Miter, double[]? dasharray = null)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetStrokeAttributes(width, color, cap, join, dasharray),
            lines.Select(l => GetSvgPath(l, isClosedPath: false))));
    }

    public override void DrawPolygon(Coord[] coords, double width,
        Color color, LineJoin join = LineJoin.Miter, double[]? dasharray = null)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetStrokeAttributes(width, color,
                LineCap.Square, // doesn't matter since shape is closed
                join, dasharray),
            GetSvgPath(coords, isClosedPath: true)));
    }

    public override void DrawFilledPolygon(Coord[] coords, Color color)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetFillAttributes(color),
            GetSvgPath(coords, isClosedPath: true)));
    }

    public override void DrawFilledPolygons(
        IEnumerable<Coord[]> polygons, Color color)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetFillAttributes(color),
            // one path per polygon
            polygons.Select(polygon => GetSvgPath(polygon, isClosedPath: true))));
    }

    public override void DrawFilledMultiPolygon(
        IEnumerable<Coord[]> multiPolygon, Color color)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetFillAttributes(color),
            // single multi-segment path for multipolygon
            GetSvgPath(multiPolygon, areClosedPaths: true)));
    }

    public override void DrawFilledMultiPolygons(
        IEnumerable<IEnumerable<Coord[]>> multipolygons, Color color)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetFillAttributes(color),
            // one multi-segment path for each multipolygon
            multipolygons.Select(multipolygon => GetSvgPath(
                multipolygon, areClosedPaths: true))));
    }

    public override void DrawCircles(IEnumerable<Coord> coords,
        double radius, double lineWidth, Color color)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetStrokeAttributes(lineWidth, color),
            coords.Select(point => GetSvgCircle(point, radius))));
    }

    public override void DrawFilledCircles(
        IEnumerable<Coord> coords, double radius, Color color)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetFillAttributes(color),
            coords.Select(point => GetSvgCircle(point, radius))));
    }


    /// <param name="emSizePt">Text em-size, in canvas units</param>
    [Obsolete]
    public override void DrawText(string s, Coord coord,
        Color color, string font, double emSize,
        TextHAlign hAlign, TextVAlign vAlign)
    {
        // Presumably, with no units, font-size is the em size
        // in canvas units:
        string sizeStr = emSize.ToString();

        // hAlign
        string? anchorStr = null;
        switch (hAlign)
        {
            case TextHAlign.Left: anchorStr = "start"; break;
            case TextHAlign.Center: anchorStr = "middle"; break;
            case TextHAlign.Right: anchorStr = "end"; break;
        }

        // vAlign
        string? dominantBaselineStr = null;
        switch (vAlign)
        {
            case TextVAlign.Top: dominantBaselineStr = "hanging"; break;
            case TextVAlign.Center: dominantBaselineStr = "central"; break; // or "middle"
            case TextVAlign.Baseline: dominantBaselineStr = "auto"; break;
            case TextVAlign.Bottom: dominantBaselineStr = "ideographic"; break;
        }

        // x/y are at baseline, according to the specified anchor
        _objects.Add(new XElement(SvgCanvas.XmlNs + "text",
            new XAttribute("font", font),
            new XAttribute("fill", color.ToHexCode()),
            new XAttribute("x", coord.X.ToString(_canvas.SvgCoordFormat)),
            new XAttribute("y", (_layerHeight-coord.Y).ToString(_canvas.SvgCoordFormat)),
            new XAttribute("style", $"font-size: {sizeStr}; text-anchor: {anchorStr}; dominant-baseline: {dominantBaselineStr};"),
            new XText(s)
            ));
    }


    // TODO: Move to base class?
    private Font GetFont(string fontName, double emSize)
    {
        float emSizePt = (float)(_canvas.ToPt(emSize));
        return new Font(fontName, (float)emSizePt);
    }

    // TODO: Move to base class?
    public override Coord GetTextSize(string fontName, double emSize, string s)
    {
        // TODO: optimize. Cache Font?
        using Font font = GetFont(fontName, emSize);
        SizeF stringSize = _graphics.MeasureString(s, font);
        return new Coord(stringSize.Width, stringSize.Height);
    }

    public override void DrawText(
        string font, double emSize,
        string s, Coord centerCiird, Color color)
    {
        // Presumably, with no units, font-size is the em size
        // in canvas units:
        string sizeStr = emSize.ToString();

        // x/y are at baseline, according to the specified anchor
        _objects.Add(new XElement(SvgCanvas.XmlNs + "text",
            new XAttribute("font", font),
            new XAttribute("fill", color.ToHexCode()),
            new XAttribute("x", centerCiird.X.ToString(_canvas.SvgCoordFormat)),
            new XAttribute("y", (_layerHeight - centerCiird.Y).ToString(_canvas.SvgCoordFormat)),
            new XAttribute("style", $"font-size: {sizeStr}; text-anchor: middle; dominant-baseline: central;"),
            new XText(s)
            ));
    }

    #region SVG Raster data

    private IEnumerable<XAttribute> GetRasterAttributes(double opacity)
    {
        if (opacity < 1.0)
        {
            yield return new XAttribute("opacity",
                opacity.ToString(_canvas.SvgCoordFormat));
        }
    }

    private XElement GetRasterData(Bitmap bitmap,
        double x, double y, double width, double height)
    {
        return new XElement(SvgCanvas.XmlNs + "image",
            new XAttribute("x", x.ToString(_canvas.SvgCoordFormat)),
            new XAttribute("y", y.ToString(_canvas.SvgCoordFormat)),
            new XAttribute("width", width.ToString(_canvas.SvgCoordFormat)),
            new XAttribute("height", height.ToString(_canvas.SvgCoordFormat)),
            new XAttribute(SvgCanvas.XmlNsXlink + "href", "data:image/png;base64," +
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
                string.Join(",", dasharray.Select(d => d.ToString(_canvas.SvgCoordFormat))));
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
        return new XElement(SvgCanvas.XmlNs + "path",
            new XAttribute("d", GetSvgPathCoords(points, isClosedPath)));
    }

    private XElement GetSvgPath(IEnumerable<Coord[]> paths, bool areClosedPaths)
    {
        return new XElement(SvgCanvas.XmlNs + "path",
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
            sb.Append(coord.X.ToString(_canvas.SvgCoordFormat));
            sb.Append(" ");
            sb.Append((_layerHeight - coord.Y).ToString(_canvas.SvgCoordFormat));
            isFirst = false;
        }

        bool isClosedPath = coords[0] == coords[^1];
        if (isClosedPath)
          sb.Append(" Z");
    }


    private XElement GetSvgCircle(Coord point, double radius)
    {
        return new XElement(SvgCanvas.XmlNs + "circle",
            new XAttribute("cx", point.X.ToString(_canvas.SvgCoordFormat)),
            new XAttribute("cy", (_layerHeight-point.Y).ToString(_canvas.SvgCoordFormat)),
            new XAttribute("r", radius.ToString(_canvas.SvgCoordFormat))
            );
    }




    #endregion
}