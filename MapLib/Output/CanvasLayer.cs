using MapLib.Geometry;
using System.Drawing;

namespace MapLib.Output;

public abstract class CanvasLayer : IDisposable
{
    public abstract void Dispose();

    public string? Name { get; set; }

    // TODO: layer opacity, blending modes, mask

    public abstract void DrawBitmap(
        Bitmap bitmap,
        double x, double y, double width, double height,
        double opacity);

    public abstract void DrawLine(
        Coord[] coords,
        double width, Color color,
        LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Miter, // TODO: miter limit
        double[]? dasharray = null);

    public abstract void DrawLines(
        IEnumerable<Coord[]> lines,
        double width, Color color,
        LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Miter, // TODO: miter limit
        double[]? dasharray = null);

    /// <summary>
    /// Draws the outline of a polygon.
    /// This is similar to DrawLine with equal start and end point,
    /// except it guarantees the proper line join.
    /// </summary>
    public abstract void DrawPolygon(
        Coord[] coords,
        double width, Color color,
        LineJoin join = LineJoin.Miter, // TODO: miter limit
        double[]? dasharray = null);

    /// <summary>
    /// Fills a polygon specified by the given coordinates.
    /// </summary>
    public abstract void DrawFilledPolygon(
        //IEnumerable<Coord> polygon,
        Coord[] coords,
        Color color);

    /// <summary>
    /// Fills the polygons specified by the given coordinates.
    /// </summary>
    /// <remarks>
    /// NOTE: The polygons, including any CW (holes), are drawn
    /// individually. Use DrawFilledMultiPolygon if CW polygons
    /// should be rendered as holes.
    /// </remarks>
    public abstract void DrawFilledPolygons(
        //IEnumerable<IEnumerable<Coord>> polygons,
        IEnumerable<Coord[]> polygons,
        Color color);

    /// <summary>
    /// Fills the multipolygon specified by the given coordinates.
    /// </summary>
    /// <remarks>
    /// CCW polygons are outer polygons, CW are holes.
    /// </remarks>
    public abstract void DrawFilledMultiPolygon(
        //IEnumerable<IEnumerable<Coord>> multiPolygon,
        IEnumerable<Coord[]> multiPolygon,
        Color color);

    /// <summary>
    /// Fills the multipolygons specified by the given coordinates.
    /// </summary>
    public abstract void DrawFilledMultiPolygons(
        //IEnumerable<IEnumerable<IEnumerable<Coord>>> multipolygons,
        IEnumerable<IEnumerable<Coord[]>> multiPolygons,
        Color color);

    public abstract void DrawFilledCircles(
        IEnumerable<Coord> points, double radius, Color color);

    public abstract void DrawText(string s, Coord coord,
        Color color, string font, double emSizePt,
        TextHAlign hAlign, TextVAlign vAlign);
}