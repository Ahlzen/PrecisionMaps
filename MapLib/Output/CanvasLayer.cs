using MapLib.Geometry;
using System.Drawing;

namespace MapLib.Output;

public abstract class CanvasLayer
{
    public string? Name { get; set; }

    // TODO: layer opacity, blending modes, mask

    public abstract void DrawBitmap(
        Bitmap bitmap,
        double x, double y, double width, double height,
        double opacity);

    public abstract void DrawLines(
        IEnumerable<Coord[]> lines,
        double width, Color color,
        LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Miter, // TODO: miter limit
        double[]? dasharray = null);

    public abstract void DrawFilledPolygons(
        IEnumerable<IEnumerable<Coord>> polygons,
        Color color);

    public abstract void DrawFilledMultiPolygons(
        IEnumerable<IEnumerable<IEnumerable<Coord>>> multipolygons,
        Color color);

    public abstract void DrawFilledCircles(
        IEnumerable<Coord> points, double radius, Color color);

    public abstract void DrawText(string s, Coord coord,
        Color color, string font, double size,
        TextHAlign hAlign);
}