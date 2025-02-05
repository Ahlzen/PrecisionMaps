namespace MapLib.Output;

public abstract class Canvas
{
    public double Width { get; set; }
    public double Height { get; set; }

    public Canvas(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public abstract CanvasLayer AddNewLayer(string name);
    public abstract void RemoveLayer(CanvasLayer layer);

    public abstract IEnumerable<CanvasLayer> Layers { get; }
    public abstract int LayerCount { get; }

    public abstract string DefaultFileExtension { get; }
    public abstract void SaveToFile(string filename);
}