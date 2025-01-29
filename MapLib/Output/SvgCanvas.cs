using MapLib.Geometry;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml.Linq;

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

    public SvgCanvas(double width, double height,
        Color? backgroundColor, int decimals = 3) :
        base(width, height)
    {
        _width = width;
        _height = height;
        _backgroundColor = backgroundColor ?? Color.Transparent;
        SvgCoordFormat = "F" + decimals;
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
        var svgCanvasLayer = layer as SvgCanvasLayer;
        if (layer == null) return;
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
}

/// <remarks>
/// NOTE: Since the SVG coordinate system has positive Y down,
/// and our coordinates are positive Y up, drawing operations have to
/// flip (offset and negate) the Y coordinate.
/// </remarks>
public class SvgCanvasLayer : CanvasLayer
{
    private SvgCanvas _canvas;
    private List<XObject> _objects = new List<XObject>();
    private double _layerHeight;
    private double _layerWidth;


    internal SvgCanvasLayer(SvgCanvas canvas)
    {
        _canvas = canvas;
        _layerWidth = canvas.Width;
        _layerHeight = canvas.Height;
    }

    internal XElement GetSvgData()
    {
        return new XElement(SvgCanvas.XmlNs + "g",
            new XAttribute("id", Name),
            _objects);
    }

    public override void DrawBitmap(Bitmap bitmap,
        double x, double y, double width, double height, double opacity)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetRasterAttributes(opacity),
            GetRasterData(bitmap, x, _layerHeight-y, width, height)));
    }

    public override void DrawLines(
        IEnumerable<Coord[]> lines,
        double width, Color color,
        LineCap cap = LineCap.Butt, LineJoin join = LineJoin.Miter,
        double[] dasharray = null)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetStrokeAttributes(width, color, cap, join, dasharray),
            lines.Select(line => GetSvgPath(line))));
    }

    public override void DrawFilledPolygons(IEnumerable<IEnumerable<Coord>> polygons, Color color)
    {
        // TODO: is this an issue with the fill rule?
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetFillAttributes(color),
            polygons.Select(polygon => GetSvgPath(polygon))));
    }

    public override void DrawFilledMultiPolygons(
        IEnumerable<IEnumerable<IEnumerable<Coord>>> multipolygons, Color color)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetFillAttributes(color),
            multipolygons.Select(multipolygon => GetSvgPath(multipolygon))));
    }

    public override void DrawFilledCircles(IEnumerable<Coord> points, double radius, Color color)
    {
        _objects.Add(new XElement(SvgCanvas.XmlNs + "g",
            GetFillAttributes(color),
            points.Select(point => GetSvgCircle(point, radius))));
    }

    public override void DrawText(string s, Coord coord,
        Color color, string font, double size,
        TextHAlign hAlign)
    {
        string anchorStr = null;
        switch (hAlign)
        {
            case TextHAlign.Left: anchorStr = "start"; break;
            case TextHAlign.Center: anchorStr = "middle"; break;
            case TextHAlign.Right: anchorStr = "end"; break;
        }
        string sizeStr = size.ToString();

        _objects.Add(new XElement(SvgCanvas.XmlNs + "text",
            new XAttribute("font", font),
            new XAttribute("fill", color.ToHexCode()),
            new XAttribute("x", coord.X.ToString(_canvas.SvgCoordFormat)),
            new XAttribute("y", coord.Y.ToString(_canvas.SvgCoordFormat)),
            new XAttribute("style", $"font-size: {sizeStr}; text-anchor: {anchorStr};"),
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
        double[] dasharray = null)
    {
        yield return new XAttribute("fill", "none");
        yield return new XAttribute("stroke", color.ToHexCode());
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
        yield return new XAttribute("fill", color.ToHexCode());
        yield return new XAttribute("fill-rule", "evenodd"); // for mutipolygons etc
    }

    #endregion

    #region SVG Utils

    private XElement GetSvgPath(IEnumerable<Coord> points)
    {
        return new XElement(SvgCanvas.XmlNs + "path",
            new XAttribute("d", GetSvgPathCoords(points)));
    }

    private XElement GetSvgPath(IEnumerable<IEnumerable<Coord>> polygons)
    {
        return new XElement(SvgCanvas.XmlNs + "path",
            new XAttribute("d", GetSvgPathCoords(polygons)));
    }

    private string GetSvgPathCoords(IEnumerable<IEnumerable<Coord>> polygons)
    {
        var sb = new StringBuilder();
        foreach (IEnumerable<Coord> polygon in polygons)
        {
            sb.Append(GetSvgPathCoords(polygon));
            sb.Append(" ");
        }
        return sb.ToString().TrimEnd();
    }

    private string GetSvgPathCoords(IEnumerable<Coord> points)
    {
        var sb = new StringBuilder();
        bool isFirst = true;
        foreach (Coord point in points)
        {
            sb.Append(isFirst ? "M " : " L ");
            sb.Append(point.X.ToString(_canvas.SvgCoordFormat));
            sb.Append(" ");
            sb.Append((_layerHeight - point.Y).ToString(_canvas.SvgCoordFormat));
            isFirst = false;
        }
        return sb.ToString();
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