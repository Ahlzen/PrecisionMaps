namespace MapLib.Output;

public abstract class Canvas
{
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
                return mm / 25.4;
            case CanvasUnit.Pixel:
                return mm * (25.4 / 72); // 72 ppi = (25.4/72) mm / pixel
            default:
                throw new NotSupportedException(
                    "Unsupported canvas unit type");
        }
    }
}