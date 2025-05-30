namespace MapLib.Output;

public abstract class Canvas : IDisposable
{
    public const double PixelsPerInch = 90; // may make this non-const
    public const double MmPerInch = 25.4;
    public const double MmPerPoint = MmPerInch / 72.0; // 1 pt = 1/72 in
    public const double PointsPerMm = 1.0 / MmPerPoint;

    public CanvasUnit Unit { get; }
    public double Width { get; }
    public double Height { get; }

    public Canvas(CanvasUnit unit, double width, double height)
    {
        Unit = unit;
        Width = width;
        Height = height;
    }

    public abstract void Dispose();

    public abstract CanvasLayer AddNewLayer(string name);
    public abstract void RemoveLayer(CanvasLayer layer);

    public abstract IEnumerable<CanvasLayer> Layers { get; }
    public abstract int LayerCount { get; }

    public abstract string DefaultFileExtension { get; }
    public abstract void SaveToFile(string filename);

    public virtual string FormatSummary()
        => $"{GetType()}, {Unit}, {Width} x {Height}";

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