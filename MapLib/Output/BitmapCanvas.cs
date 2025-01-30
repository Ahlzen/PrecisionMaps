#define EXTRADEBUG

using MapLib.Geometry;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;

namespace MapLib.Output;

public class BitmapCanvas : Canvas
{
    private readonly int _width;
    private readonly int _height;
    private readonly Color _backgroundColor;
    private readonly List<BitmapCanvasLayer> _layers = new List<BitmapCanvasLayer>();

    public BitmapCanvas(double width, double height, Color? backgroundColor)
        : base(width, height)
    {
        _width = (int)width;
        _height = (int)height;
        _backgroundColor = backgroundColor ?? Color.Transparent;
    }

    public override IEnumerable<CanvasLayer> Layers => _layers;
    public override int LayerCount => _layers.Count;

    public override CanvasLayer AddNewLayer(string name)
    {
        var layer = new BitmapCanvasLayer(_width, _height);
        layer.Name = name;
        _layers.Add(layer);
        return layer;
    }

    public override void RemoveLayer(CanvasLayer layer)
    {
        var bitmapCanvasLayer = layer as BitmapCanvasLayer;
        if (bitmapCanvasLayer == null) return;
        _layers.Remove(bitmapCanvasLayer);
    }

    public Bitmap GetBitmap()
    {
        // Render composite bitmap from layers
        var canvasBitmap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
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
}

/// <remarks>
/// NOTE: Since the GDI+ coordinate system has positive Y down,
/// and our coordinates are positive Y up, drawing operations have to
/// flip (offset and negate) the Y coordinate.
/// </remarks>
internal class BitmapCanvasLayer : CanvasLayer
{
    private static readonly Color DebugColor = Color.Red;

    private readonly Graphics _graphics;
    private int _layerHeight;
    private int _layerWidth;

    internal BitmapCanvasLayer(int width, int height)
    {
        Bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        Bitmap.MakeTransparent(Bitmap.GetPixel(0, 0));
        _layerWidth = width;
        _layerHeight = height;
        _graphics = Graphics.FromImage(Bitmap);
        _graphics.SmoothingMode = SmoothingMode.HighQuality;
    }

    internal Bitmap Bitmap { get; }

    public override void DrawBitmap(Bitmap srcBitmap,
        double x, double y, double width, double height, double opacity)
    {
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
            _graphics.DrawImage(srcBitmap, [
                    new PointF((float)x, (float)(_layerHeight-y)),
                    new PointF((float)(x+width), (float)(_layerHeight-y)),
                    new PointF((float)x, (float)(_layerHeight - y + height))],
                srcBitmap.GetBounds(ref unit),
                GraphicsUnit.Pixel,
                imageAttributes
                );
        }
        else
        {
            _graphics.DrawImage(srcBitmap, new[] {
                    new PointF((float)x, (float)(_layerHeight-y)),
                    new PointF((float)(x+width), (float)(_layerHeight-y)),
                    new PointF((float)x, (float)(_layerHeight - y + height))
                });
        }
    }

    public override void DrawLines(
        IEnumerable<Coord[]> lines,
        double width,
        Color color, LineCap cap = LineCap.Butt, LineJoin join = LineJoin.Miter,
        double[]? dasharray = null)
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
        foreach (IEnumerable<Coord> line in lines)
            _graphics.DrawLines(pen, line.Select(c =>
                new PointF((float)c.X, _layerHeight-(float)c.Y)).ToArray());
        pen.Dispose();
    }

    public override void DrawFilledCircles(IEnumerable<Coord> points, double radius, Color color)
    {
        foreach (Coord point in points)
            _graphics.FillEllipse(new SolidBrush(color),
                new RectangleF((float)(point.X-radius/2), (float)(_layerHeight-point.Y-radius/2), (int)radius, (int)radius));
    }

    public override void DrawFilledPolygons(IEnumerable<IEnumerable<Coord>> polygons, Color color)
    {
        var brush = new SolidBrush(color);
        foreach (IEnumerable<Coord> polygon in polygons)
            _graphics.FillPolygon(brush, polygon.Select(c =>
                new PointF((float)c.X, (float)(_layerHeight-c.Y))).ToArray());
        brush.Dispose();
    }

    public override void DrawFilledMultiPolygons(
        IEnumerable<IEnumerable<IEnumerable<Coord>>> multipolygons, Color color)
    {
        var brush = new SolidBrush(color);
        foreach (IEnumerable<IEnumerable<Coord>> multipolygon in multipolygons)
        {
            var path = new GraphicsPath(FillMode.Alternate);
            foreach (IEnumerable<Coord> polygon in multipolygon)
                path.AddPolygon(polygon.Select(c =>
                    new PointF((float)c.X, (float)(_layerHeight-c.Y))).ToArray());
            _graphics.FillPath(brush, path);
            path.Dispose();
        }
        brush.Dispose();
    }

    public override void DrawText(string s, Coord coord,
        Color color, string fontName, double size,
        TextHAlign hAlign)
    {
        Font font = new Font(fontName, (float)size);
        Brush brush = new SolidBrush(color);

        SizeF stringSize = _graphics.MeasureString(s, font);
        double baseline = GetBaseline(font);

        // Flip Y coordinate
        coord = new Coord(coord.X, _layerHeight - coord.Y);

        double offsetX = 0;
        switch (hAlign)
        {
            case TextHAlign.Left: break; // no extra offset
            case TextHAlign.Center: offsetX = -stringSize.Width / 2.0; break;
            case TextHAlign.Right: offsetX = -stringSize.Width; break;
        }

#if EXTRADEBUG
        DrawFilledCircles([coord], 3, DebugColor);
        Coord[] coords = [new Coord(coord.X + offsetX, coord.Y), new Coord(coord.X + offsetX + stringSize.Width, coord.Y)];
        DrawLines([
                [new Coord(coord.X + offsetX, coord.Y- baseline), new Coord(coord.X + offsetX + stringSize.Width, coord.Y- baseline)],
                [new Coord(coord.X + offsetX, coord.Y+ stringSize.Height - baseline), new Coord(coord.X + offsetX + stringSize.Width, coord.Y+ stringSize.Height- baseline)],
                coords,],
                1, DebugColor);
#endif

        _graphics.DrawString(s, font, brush,
            new PointF((float)(coord.X + offsetX), (float)(coord.Y - baseline)));
    }

    #region Helpers

    private double GetBaseline(Font font)
    {
        FontFamily ff = font.FontFamily;
        float lineSpace = ff.GetLineSpacing(font.Style);
        float ascent = ff.GetCellAscent(font.Style);
        float baseline = font.GetHeight(_graphics) * ascent / lineSpace;
        return baseline;
    }

    #endregion
}