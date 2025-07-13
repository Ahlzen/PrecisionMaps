using OSGeo.OGR;

namespace MapLib.Output;

public abstract class CanvasStack : IDisposable
{
    public const double PixelsPerInch = 90; // may make this non-const
    public const double MmPerInch = 25.4;
    public const double MmPerPoint = MmPerInch / 72.0; // 1 pt = 1/72 in
    public const double PointsPerMm = 1.0 / MmPerPoint;

    public CanvasUnit Unit { get; }
    public double Width { get; }
    public double Height { get; }

    public CanvasStack(CanvasUnit unit, double width, double height)
    {
        Unit = unit;
        Width = width;
        Height = height;
    }

    public abstract void Dispose();

    // NOTE: This determines layer order (bottom to top)
    public OrderedDictionary<string, Canvas> Layers { get; } = new();
    public int LayerCount => Layers.Count;
    public abstract Canvas AddNewLayer(string name);
    public Canvas GetLayer(string layerName) {
        if (!Layers.ContainsKey(layerName))
            throw new ApplicationException($"Layer \"{layerName}\" not found.");
        return Layers[layerName];
    }
    public IList<Canvas> GetLayers(IEnumerable<string> layerNames)
        => layerNames.Select(l => GetLayer(l)).ToList();

    public Dictionary<string, Canvas> Masks { get; } = new();
    public int MaskCount => Masks.Count;
    public abstract Canvas AddNewMask(string name);
    public Canvas GetMask(string maskName) {
        if (!Masks.ContainsKey(maskName))
            throw new ApplicationException($"Mask \"{maskName}\" not found.");
        return Masks[maskName];
    }
    public IList<Canvas> GetMasks(IEnumerable<string> maskNames)
        => maskNames.Select(m => GetMask(m)).ToList();

    public abstract string DefaultFileExtension { get; }
    public abstract void SaveToFile(string filename);
    public abstract void SaveLayerToFile(string baseFilename, string layerName);
    public virtual void SaveAllLayersToFile(string baseFilename)
    {
        foreach (var layer in Layers)
            SaveLayerToFile(baseFilename, layer.Key);
        foreach (var mask in Masks)
            SaveLayerToFile(baseFilename, mask.Key);
    }

    public virtual string FormatSummary()
        => $"{GetType()}, {Unit}, {Width} x {Height}";


    // Unit conversion

    /// <summary>
    /// Translate mm -> canvas units.
    /// </summary>
    public double FromMm(double mm) => mm * FromMmFactor();

    /// <summary>
    /// Translate canvas units -> mm.
    /// </summary>
    public double ToMm(double units) => units * ToMmFactor();

    /// <summary>
    /// Conversion factor mm -> Canvas Units
    /// </summary>
    public double FromMmFactor()
    {
        switch (Unit)
        {
            case CanvasUnit.Mm:
                return 1;
            case CanvasUnit.In:
                return 1 / MmPerInch;
            case CanvasUnit.Pixel:
                return PixelsPerInch / MmPerInch;
            default:
                throw new NotSupportedException(
                    "Unsupported canvas unit type");
        }
    }
    /// <summary>
    /// Conversion factor Canvas Units -> mm
    /// </summary>
    public double ToMmFactor() => 1.0 / FromMmFactor();

    /// <summary>
    /// Translate points (as used e.g. in typography) -> canvas units.
    /// </summary>
    public double FromPt(double pt) => FromMm(pt * MmPerPoint);

    /// <summary>
    /// Translate canvas units -> points.
    /// </summary>
    public double ToPt(double units) => ToMm(units) * PointsPerMm;

    /// <summary>
    /// Translate canvas units -> pixels.
    /// </summary>
    public double ToPixels(double units) =>
        units * ToPixelsFactor();

    /// <summary>
    /// Translate canvas pixels -> units.
    /// </summary>
    public double FromPixels(double units) =>
        units / ToPixelsFactor();

    /// <summary>
    /// Conversion factor canvas units -> Pixels
    /// </summary>
    public double ToPixelsFactor()
    {
        switch (Unit)
        {
            case CanvasUnit.Mm:
                return PixelsPerInch / MmPerInch;
            case CanvasUnit.In:
                return PixelsPerInch;
            case CanvasUnit.Pixel:
                return 1;
            default:
                throw new NotSupportedException(
                    "Unsupported canvas unit type");
        }
    }
}