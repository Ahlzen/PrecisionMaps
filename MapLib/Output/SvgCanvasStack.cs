using MapLib.Util;
using System.Drawing;
using System.IO;
using System.Xml.Linq;

namespace MapLib.Output;

public class SvgCanvasStack : CanvasStack
{
    internal static readonly XNamespace XmlNs = XNamespace.Get("http://www.w3.org/2000/svg");
    internal static readonly XNamespace XmlNsXlink = XNamespace.Get("http://www.w3.org/1999/xlink");

    // Presumably, with no units, font-size is the em size
    // in canvas units:
    // HACK: GDI+ measures text slightly differently (em-size) than the
    // baseline-to-baseline measurement of SVG. Add an approximate
    // compensation factor (for now; long term: do own text rendering
    // for full control)
    internal const double TextScaleFactor = (35.0 / 30.0);

    private readonly double _width;
    private readonly double _height;
    private readonly Color _backgroundColor;
    private readonly List<SvgCanvas> _layers = new();
    private readonly List<SvgCanvas> _masks = new();
    internal string SvgCoordFormat { get; }

    public SvgCanvasStack(CanvasUnit unit, double width, double height,
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
        _layers.Each(l => l.Dispose());
        _layers.Clear();
        _masks.Each(m => m.Dispose());
        _masks.Clear();
    }

    public override IEnumerable<Canvas> Layers => _layers;
    public override int LayerCount => _layers.Count;
    public override Canvas AddNewLayer(string name)
    {
        var layer = new SvgCanvas(this);
        layer.Name = name;
        _layers.Add(layer);
        return layer;
    }

    public override IEnumerable<Canvas> Masks => _masks;
    public override int MaskCount => _masks.Count;
    public override Canvas AddNewMask(string name)
    {
        var mask = new SvgCanvas(this);
        mask.Name = name;
        mask.Clear(Canvas.MaskBackgroundColor);
        _masks.Add(mask);
        return mask;
    }


    public string GetSvg() => GetSvgData().ToString();
    public void SaveSvg(string filename) => GetSvgData().Save(filename);

    private XDocument GetSvgData() => GetSvgData(
        _layers.Select(layer => layer.GetSvgData()).Union(
            _masks.Select(mask => mask.GetMaskData())));

    private XDocument GetSvgData(IEnumerable<XElement> layerData)
    {
        return new XDocument(
             new XDeclaration("1.0", "utf-8", "yes"),
             new XElement(XmlNs + "svg",
                 new XAttribute("xmlns", XmlNs),
                 new XAttribute(XNamespace.Xmlns + "xlink", XmlNsXlink),
                 new XAttribute("width", _width.ToString(SvgCoordFormat)),
                 new XAttribute("height", _height.ToString(SvgCoordFormat)),
                 Clear(_backgroundColor),
                 layerData));
    }

    internal static IEnumerable<XElement> Clear(Color color)
    {
        if (color == Color.Transparent) yield break;
        yield return new XElement(SvgCanvasStack.XmlNs + "rect",
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

    public override void SaveLayersToFile(string baseFilename)
    {
        foreach (SvgCanvas layer in _layers.Union(_masks))
        {
            string filename = FileSystemHelpers.GetTempOutputFileName(
                    ".svg", baseFilename + "_" + layer.Name);
            XElement layerData = layer.GetSvgData();
            XDocument svgData = GetSvgData([layerData]);
            File.WriteAllText(filename, svgData.ToString());
        }
    }
}