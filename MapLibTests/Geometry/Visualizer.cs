﻿using System.Drawing;
using System.Diagnostics;
using MapLibTests;

namespace MapLib.Tests.Geometry;

[SupportedOSPlatform("windows")]
internal class Visualizer
{
    private List<Shape> _shapes = new List<Shape>();
    private int _width, _height;
    private float _scale;

    /// <summary>
    /// Margin on each side of bbox. 0.05 = 5% of bbox size
    /// </summary>
    private const float Margin = 0.05f;

    internal Visualizer(int width, int height)
    {
        _width = width;
        _height = height;
    }

    internal void Add(Shape s) {
        _shapes.Add(s);
    }

    internal void Show()
    {
        string filename = BaseFixture.GetTempFileName(".png");
        Save(filename);
        Process.Start(new ProcessStartInfo {
            FileName = filename,
            UseShellExecute = true
        });
        Debug.WriteLine(filename);
    }

    internal void Save(string filename) {
        Bitmap bitmap = Render();
        bitmap.Save(filename);
        Debug.WriteLine(filename);
        bitmap.Dispose();
    }

    internal static void RenderAndShow(
        int width, int height, params Shape[] shapes)
    {
        var v = new Visualizer(width, height);
        foreach (Shape s in shapes) v.Add(s);
        v.Show();
    }

    internal static void RenderAndShow(
        int width, int height, params Coord[] coords)
    {
        var v = new Visualizer(width, height);
        foreach (Coord c in coords) v.Add(new MapLib.Geometry.Point(c, null));
        v.Show();
    }

    #region Helpers

    // Draw using these colors, in turn:
    private static Color[] Colors = {
        Color.Black,
        Color.Red,
        Color.Green,
        Color.Blue,
        Color.Cyan,
        Color.Magenta,
        Color.Yellow,
        Color.Brown
    };

    private Bitmap Render() {
        var bitmap = new Bitmap(_width, _height);
        var g = Graphics.FromImage(bitmap);
        g.Clear(Color.White);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

        // TODO: set up coordinate system
        var bounds = Bounds.FromBounds(_shapes.Select(s => s.GetBounds()));

        _scale = (float)(Math.Min(_width / bounds.Width, _height / bounds.Height));
        _scale *= (1f / (1f + 2f * Margin)); // add margins

        // GDI+ back end has positive Y down, so invert Y and move offset
        g.ScaleTransform(_scale, -_scale); // uniform scale
        g.TranslateTransform(
            (float)(bounds.Width * Margin - bounds.XMin),
            (float)(-bounds.Height * Margin - bounds.YMax));
        var canvas = g.VisibleClipBounds;

        // for debugging
        var debugColor = Color.FromArgb(200, 200, 200);
        // bbox
        Render(g, debugColor, bounds.AsPolygon(), 1.0f); 
        // x
        Pen pen = new Pen(debugColor, 1.0f / _scale);
        pen.DashPattern = [5/_scale, 5/_scale];
        // axes
        g.DrawLine(pen, (float)canvas.Left, 0, (float)canvas.Right, 0);
        g.DrawLine(pen, 0, (float)canvas.Bottom, 0, (float)canvas.Top);

        int colorIndex = 0;
        foreach (Shape shape in _shapes) {
            Color c = Colors[colorIndex++];
            if (colorIndex >= Colors.Length) colorIndex = 0;
            switch (shape) {
                case Polygon polygon:
                    Render(g, c, polygon);
                    break;
                case MultiPolygon multiPolygon:
                    Render(g, c, multiPolygon);
                    break;
                case Line line:
                    Render(g, c, line);
                    break;
                case MultiLine multiLine:
                    Render(g, c, multiLine);
                    break;
                case MapLib.Geometry.Point point:
                    Render(g, c, point);
                    break;
                case MultiPoint multiPoint:
                    Render(g, c, multiPoint);
                    break;
                default:
                    throw new ApplicationException("Unsupported shape");
            }
        }

        g.ResetTransform(); // or text will draw upside down if y inverted
        using (var font = new Font(FontFamily.GenericSansSerif, 7f, FontStyle.Regular))
            g.DrawString("Bounds: " + bounds.ToString(),
                font, Brushes.Silver, new PointF(0, 0));

        g.Dispose();
        return bitmap;
    }

    private void RenderCoord(Graphics g, Color c, Coord coord, float diameterPixels = 6.0f)
    {
        Pen pen = new Pen(c, 1.0f / _scale);
        float diameter = diameterPixels / _scale;
        g.DrawEllipse(pen, (float)(coord.X - diameter / 2), (float)(coord.Y - diameter / 2),
            diameter, diameter);
        pen.Dispose();
    }

    private void RenderCoordsAsLine(Graphics g, Color c, Coord[] coords, float width = 1.0f)
    {
        Pen pen = new Pen(c, width / _scale);
        for (int i = 0; i < coords.Length - 1; i++)
        {
            g.DrawLine(pen,
                (float)coords[i].X, (float)coords[i].Y,
                (float)coords[i + 1].X, (float)coords[i + 1].Y);
        }
        pen.Dispose();
    }

    private void Render(Graphics g, Color c, MapLib.Geometry.Point point, float diameterPixels = 6.0f)
        => RenderCoord(g, c, point.Coord, diameterPixels);
    
    private void Render(Graphics g, Color c, Line line, float diameterPixels = 1.0f)
        => RenderCoordsAsLine(g, c, line.Coords, diameterPixels);
    private void Render(Graphics g, Color c, Polygon polygon, float diameterPixels = 1.0f)
        => RenderCoordsAsLine(g, c, polygon.Coords, diameterPixels);
    private void Render(Graphics g, Color c, MultiPoint multiPoint, float diameterPixels = 6.0f) {
        foreach (Coord coord in multiPoint.Coords)
            RenderCoord(g, c, coord, diameterPixels);
    }
    private void Render(Graphics g, Color c, MultiLine multiLine, float diameterPixels = 1.0f) {
        foreach (Coord[] coords in multiLine.Coords)
            RenderCoordsAsLine(g, c, coords, diameterPixels);
    }
    private void Render(Graphics g, Color c, MultiPolygon multiPolygon, float diameterPixels = 1.0f) {
        foreach (Coord[] coords in multiPolygon.Coords)
            RenderCoordsAsLine(g, c, coords, diameterPixels);
    }

    public Coord InvertY(Coord c) => new Coord(c.X, -c.Y);

    #endregion
}
