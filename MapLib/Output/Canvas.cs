namespace MapLib.Output;

public abstract class Canvas
{
    public const double PixelsPerInch = 90; // may make this non-const
    public const double MmPerInch = 25.4;

    public CanvasUnit Unit { get; }
    public double Width { get; }
    public double Height { get; }

    public Canvas(CanvasUnit unit, double width, double height)
    {
        Unit = unit;
        Width = width;
        Height = height;
    }

    public abstract CanvasLayer AddNewLayer(string name);
    public abstract void RemoveLayer(CanvasLayer layer);

    public abstract IEnumerable<CanvasLayer> Layers { get; }
    public abstract int LayerCount { get; }

    public abstract string DefaultFileExtension { get; }
    public abstract void SaveToFile(string filename);

    /// <summary>
    /// Translate mm to canvas unit.
    /// </summary>
    public double FromMm(double mm)
    {
        switch (Unit)
        {
            case CanvasUnit.Mm:
                return mm;
            case CanvasUnit.In:
                return mm / MmPerInch;
            case CanvasUnit.Pixel:
                return mm * (MmPerInch / PixelsPerInch);
            default:
                throw new NotSupportedException(
                    "Unsupported canvas unit type");
        }
    }

    public double ToPixels(double units) =>
        units * ToPixelsFactor();

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