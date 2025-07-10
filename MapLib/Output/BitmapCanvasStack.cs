#define EXTRADEBUG

using MapLib.Geometry;
using MapLib.Util;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MapLib.Output;

public class BitmapCanvasStack : CanvasStack, IDisposable
{
    internal readonly double _width; // canvas width in canvas units
    internal readonly double _height; // canvas height in canvas units
    internal readonly int _pixelsX; // canvas width in pixels
    internal readonly int _pixelsY; // canvas height in pixels
    
    /// <summary>
    /// Scale to convert canvas units to pixels.
    /// </summary>
    private readonly double _pixelScaleFactor;

    /// <summary>
    /// User scale (under/oversampling).
    /// </summary>
    private readonly double _userScaleFactor;

    /// <summary>
    /// The total scale factor at which the graphics is to be rendered.
    /// </summary>
    internal double ScaleFactor => _pixelScaleFactor * _userScaleFactor;

    private readonly Color _backgroundColor;
    private readonly List<BitmapCanvas> _layers = new();
    private readonly List<BitmapCanvas> _masks = new();

    public BitmapCanvasStack(CanvasUnit unit, double width,
        double height, Color? backgroundColor,
        double? scaleFactor = null)
        : base(unit, width, height)
    {
        _width = width;
        _height = height;
        
        // Values to final scale factor
        _pixelScaleFactor = ToPixelsFactor();
        _userScaleFactor = scaleFactor ?? 1.0;

        _pixelsX = (int)(width * ScaleFactor);
        _pixelsY = (int)(height * ScaleFactor);

        _backgroundColor = backgroundColor ?? Color.Transparent;
    }

    public override void Dispose()
    {
        _layers.Each(l => l.Dispose());
        _layers.Clear();
        _masks.Each(m => m.Dispose());
        _masks.Clear();
    }

    public override string FormatSummary()
    {
        return base.FormatSummary() +
            $" ({_pixelsX}x{_pixelsY} px) Scale: {_pixelScaleFactor} (pixel) * {_userScaleFactor} (user) = {ScaleFactor}";
    }

    public override IEnumerable<Canvas> Layers => _layers;
    public override int LayerCount => _layers.Count;
    public override Canvas AddNewLayer(string name)
    {
        var layer = new BitmapCanvas(this);
        layer.Name = name;
        _layers.Add(layer);
        return layer;
    }

    public override IEnumerable<Canvas> Masks => _masks;
    public override int MaskCount => _masks.Count;
    public override Canvas AddNewMask(string name)
    {
        var mask = new BitmapCanvas(this);
        mask.Name = name;
        mask.Clear(Canvas.MaskBackgroundColor);
        _masks.Add(mask);
        return mask;
    }

    public Bitmap GetBitmap()
    {
        // Render composite bitmap from layers
        var canvasBitmap = new Bitmap(_pixelsX, _pixelsY, PixelFormat.Format32bppArgb);
        canvasBitmap.MakeTransparent(canvasBitmap.GetPixel(0, 0));
        using (Graphics g = Graphics.FromImage(canvasBitmap))
        {
            if (_backgroundColor != Color.Transparent)
                g.Clear(_backgroundColor);
            foreach (BitmapCanvas layer in _layers)
                g.DrawImage(layer.Bitmap, new PointF(0f, 0f));
        }
        return canvasBitmap;
    }

    public override string DefaultFileExtension => ".png";

    public override void SaveToFile(string filename)
    {
        using Bitmap bitmap = GetBitmap();
        bitmap.Save(filename);
    }

    public override void SaveLayersToFile(string baseFilename)
    {
        foreach (Canvas layer in _layers.Union(_masks))
        {
            if (layer is BitmapCanvas bitmapLayer)
            {
                string filename = FileSystemHelpers.GetTempOutputFileName(
                    ".png", baseFilename + "_" + layer.Name);
                bitmapLayer.Bitmap.Save(filename);
            }
            else
                throw new InvalidOperationException(
                    "Only BitmapCanvasLayer can be saved to file.");
        }
    }
}
