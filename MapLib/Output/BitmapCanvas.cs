#define EXTRADEBUG

using MapLib.Geometry;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using OSGeo.GDAL;

namespace MapLib.Output;

public class BitmapCanvas : Canvas, IDisposable
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
    private readonly List<BitmapCanvasLayer> _layers = new List<BitmapCanvasLayer>();

    public BitmapCanvas(CanvasUnit unit, double width,
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
        foreach (BitmapCanvasLayer layer in _layers)
            layer.Dispose();
        _layers.Clear();
    }

    public override IEnumerable<CanvasLayer> Layers => _layers;
    public override int LayerCount => _layers.Count;

    public override CanvasLayer AddNewLayer(string name)
    {
        var layer = new BitmapCanvasLayer(this);
        layer.Name = name;
        _layers.Add(layer);
        return layer;
    }

    public override void RemoveLayer(CanvasLayer layer)
    {
        var bitmapCanvasLayer = layer as BitmapCanvasLayer;
        if (bitmapCanvasLayer == null) return;
        _layers.Remove(bitmapCanvasLayer);
        bitmapCanvasLayer.Dispose();
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
            foreach (BitmapCanvasLayer layer in _layers)
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
}

/// <remarks>
/// NOTE: Since the GDI+ coordinate system has positive Y down,
/// and our coordinates are positive Y up, the Y-coordinate needs
/// top be flipped (offset and negate).
/// </remarks>
internal class BitmapCanvasLayer : CanvasLayer, IDisposable
{
    private static readonly Color DebugColor = Color.Magenta;

    private readonly BitmapCanvas _canvas;
    private readonly Graphics _graphics;
    //private int _pixelsX;
    //private int _pixelsY;
    //private float _height; // in canvas units
    private int pixelsX => _canvas._pixelsX;
    private int pixelsY => _canvas._pixelsY;
    private double _height => _canvas.Height;

    private double scaleFactor => _canvas.ScaleFactor;

    /// <param name="height">Height, in canvas units.</param>
    internal BitmapCanvasLayer(
        //int pixelsX, int pixelsY,
        //double height, double scaleFactor
        BitmapCanvas canvas)
    {
        _canvas = canvas;

        Bitmap = new Bitmap(pixelsX, pixelsY, PixelFormat.Format32bppArgb);
        Bitmap.MakeTransparent(Bitmap.GetPixel(0, 0));
        //_height = (float)height;
        //_pixelsY = pixelsX;
        //_pixelsX = pixelsY;
        _graphics = Graphics.FromImage(Bitmap);
        _graphics.SmoothingMode = SmoothingMode.HighQuality;

        // Offset and invert Y (see remarks)
        //_graphics.TranslateTransform(0, (float)(height*scaleFactor));
        //_graphics.ScaleTransform((float)scaleFactor, -(float)scaleFactor);

        // Scale drawing ops
        _graphics.ScaleTransform((float)scaleFactor, (float)scaleFactor);
    }

    public override void Dispose()
    {
        _graphics.Dispose();
        Bitmap.Dispose();
    }

    public override void DrawLine(Coord[] coords,
        double width, Color color, LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Miter, double[]? dasharray = null)
    {
        using Pen pen = GetPen(width, color, cap, join, dasharray);
        _graphics.DrawLines(pen, ToPoints(coords));
    }

    public override void DrawLines(
        IEnumerable<Coord[]> lines,
        double width, Color color, LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Miter, double[]? dasharray = null)
    {
        using Pen pen = GetPen(width, color, cap, join, dasharray);
        foreach (Coord[] line in lines)
            _graphics.DrawLines(pen, ToPoints(line));
    }

    public override void DrawPolygon(Coord[] coords,
        double width, Color color, LineJoin join = LineJoin.Miter,
        double[]? dasharray = null)
    {
        using Pen pen = GetPen(width, color,
            LineCap.Square, // doesn't matter since this is always closed
            join, dasharray);
        PointF[] points = ToPoints(coords);
        _graphics.DrawPolygon(pen, points);
    }

    public override void DrawFilledCircles(
        IEnumerable<Coord> points, double radius, Color color)
    {
        foreach (Coord point in points)
            _graphics.FillEllipse(new SolidBrush(color),
                new RectangleF((float)(point.X - radius / 2),
                    (float)_height - (float)(point.Y - radius / 2),
                    (int)radius, (int)radius));
    }

    public override void DrawFilledPolygon(Coord[] polygon, Color color)
    {
        using var brush = new SolidBrush(color);
        _graphics.FillPolygon(brush, ToPoints(polygon));
    }

    public override void DrawFilledPolygons(
        IEnumerable<Coord[]> polygons, Color color)
    {
        using var brush = new SolidBrush(color);
        foreach (Coord[] polygon in polygons)
            _graphics.FillPolygon(brush, ToPoints(polygon));
    }

    public override void DrawFilledMultiPolygon(
        IEnumerable<Coord[]> multiPolygon, Color color)
    {
        using SolidBrush brush = new(color);
        using GraphicsPath path = new(FillMode.Winding);
        foreach (Coord[] coords in multiPolygon)
        {
            path.AddPolygon(ToPoints(coords));
        }
        _graphics.FillPath(brush, path);
    }

    public override void DrawFilledMultiPolygons(
        IEnumerable<IEnumerable<Coord[]>> multipolygons, Color color)
    {
        using SolidBrush brush = new(color);
        foreach (IEnumerable<Coord[]> multipolygon in multipolygons)
        {
            using GraphicsPath path = new(FillMode.Winding);
            foreach (Coord[] polygon in multipolygon)
                path.AddPolygon(ToPoints(polygon));
            _graphics.FillPath(brush, path);
        }
    }

    /// <param name="emSizePt">Text em-size, in canvas units</param>
    public override void DrawText(string s, Coord coord,
        Color color, string fontName, double emSize,
        TextHAlign hAlign, TextVAlign vAlign)
    {
        float emSizePt = (float)(_canvas.ToPt(emSize));
        using Font font = new Font(fontName, (float)emSizePt);
        using Brush brush = new SolidBrush(color);

        SizeF stringSize = _graphics.MeasureString(s, font);
        float baseline = GetBaseline(font); // Top<->Baseline distance

        float x = (float)coord.X;
        float y = (float)(_height - coord.Y);

        // Graphics.DrawString coordinates are for top left corner.
        // Adjust location depending on selected alignment:
        switch (hAlign) {
            case TextHAlign.Left: break; // no extra offset
            case TextHAlign.Center: x -= stringSize.Width / 2.0f; break;
            case TextHAlign.Right: x -= stringSize.Width; break;
        }
        switch (vAlign) {
            case TextVAlign.Top: break; // no extra offset
            case TextVAlign.Center: y -= stringSize.Height / 2; break;
            case TextVAlign.Baseline: y -= baseline; break;
            case TextVAlign.Bottom: y -= stringSize.Height; break;
        }

#if EXTRADEBUG
        float lineWidth = (float)emSizePt * 0.03f;
        float pointRadius = (float)emSizePt * 0.15f;
        using Brush debugBrush = new SolidBrush(DebugColor);
        using Pen debugPen = new Pen(DebugColor, lineWidth);
        // text coordinate
        _graphics.FillEllipse(debugBrush, new RectangleF(
            (float)(coord.X) - pointRadius,
            (float)(_height - coord.Y) - pointRadius,
            pointRadius * 2,
            pointRadius * 2));
        // bbox
        _graphics.DrawLines(debugPen, new PointF[] {
            new PointF(x, y),
            new PointF(x + stringSize.Width, y),
            new PointF(x + stringSize.Width, y + stringSize.Height),
            new PointF(x, y + stringSize.Height),
            new PointF(x, y)
            });
        // baseline
        _graphics.DrawLines(debugPen, new PointF[] {
            new PointF(x, y + baseline),
            new PointF(x + stringSize.Width, y + baseline)});
#endif
        _graphics.DrawString(s, font, brush,
            new PointF(x, y));
    }

    internal Bitmap Bitmap { get; }

    public override void DrawBitmap(Bitmap srcBitmap,
        double x, double y, double width, double height, double opacity)
    {
        PointF[] cornerPoints = [ // order: [UL, UR, LL]
            new PointF((float)x, (float)(_height - y - height)), // UL
            new PointF((float)(x + width), (float)(_height - y - height)), // UR
            new PointF((float)x, (float)(_height - y)) // LL
        ];

        if (opacity < 1.0)
        {
            float[][] colorMatrixElements = {
                   [1, 0, 0, 0, 0],
                   [0, 1, 0, 0, 0],
                   [0, 0, 1, 0, 0],
                   [0, 0, 0, (float)opacity, 0],
                   [0, 0, 0, 0, 1]};
            var colorMatrix = new ColorMatrix(colorMatrixElements);
            var imageAttributes = new ImageAttributes();
            imageAttributes.SetColorMatrix(
               colorMatrix,
               ColorMatrixFlag.Default,
               ColorAdjustType.Bitmap);
            GraphicsUnit unit = GraphicsUnit.Pixel;
            _graphics.DrawImage(srcBitmap, cornerPoints,
                srcBitmap.GetBounds(ref unit),
                GraphicsUnit.Pixel,
                imageAttributes);
        }
        else
        {
            _graphics.DrawImage(srcBitmap, cornerPoints);
        }
    }

    #region Helpers

    public Pen GetPen(double width, Color color,
        LineCap cap, LineJoin join,
        double[]? dasharray)
    {
        Pen pen = new Pen(color, (float)width);
        System.Drawing.Drawing2D.LineCap wCap = System.Drawing.Drawing2D.LineCap.Flat;
        DashCap dashCap = DashCap.Flat;
        System.Drawing.Drawing2D.LineJoin wJoin = System.Drawing.Drawing2D.LineJoin.Miter;
        switch (cap)
        {
            case LineCap.Butt:
                wCap = System.Drawing.Drawing2D.LineCap.Flat;
                break;
            case LineCap.Square:
                wCap = System.Drawing.Drawing2D.LineCap.Square;
                break;
            case LineCap.Round:
                wCap = System.Drawing.Drawing2D.LineCap.Round;
                dashCap = DashCap.Round;
                break;
        }
        pen.SetLineCap(wCap, wCap, dashCap);
        switch (join)
        {
            case LineJoin.Miter:
                wJoin = System.Drawing.Drawing2D.LineJoin.Miter;
                break;
            case LineJoin.Bevel:
                wJoin = System.Drawing.Drawing2D.LineJoin.Bevel;
                break;
            case LineJoin.Round:
                wJoin = System.Drawing.Drawing2D.LineJoin.Round;
                break;
        }
        pen.LineJoin = wJoin;
        if (dasharray != null)
            pen.DashPattern = dasharray.Select(d => (float)d).ToArray();
        return pen;
    }

    private float GetBaseline(Font font)
    {
        FontFamily ff = font.FontFamily;
        float lineSpace = ff.GetLineSpacing(font.Style);
        float ascent = ff.GetCellAscent(font.Style);
        float baseline = font.GetHeight(_graphics) * ascent / lineSpace;
        return baseline;
    }

    public PointF[] ToPoints(Coord[] coords)
    {
        PointF[] points = new PointF[coords.Length];
        for (int i = 0; i < coords.Length; i++)
        {
            points[i] = new PointF(
                (float)coords[i].X,
                // Invert Y coordinate (see class remarks)
                (float)_height - (float)coords[i].Y);
        }
        return points;
    }

    #endregion
}

