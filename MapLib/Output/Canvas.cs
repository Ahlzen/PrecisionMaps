using MapLib.Geometry;
using System.Drawing;

namespace MapLib.Output;

public abstract class Canvas(CanvasStack stack) : IDisposable
{
    // Masks are black on white (black is masked; white is transparent).
    public static readonly Color MaskBackgroundColor = Color.White;
    public static readonly Color MaskColor = Color.Black;

    public static readonly Color DebugColor = Color.Magenta;

    public abstract void Dispose();

    public string? Name { get; set; }

    public CanvasStack Stack { get; } = stack;
    public CanvasUnit Unit => Stack.Unit;
    public double Width => Stack.Width;
    public double Height => Stack.Height;

    public abstract void Clear(Color color);

    public abstract void DrawBitmap(
        Bitmap bitmap,
        double x, double y, double width, double height,
        double opacity);

    public abstract void DrawLine(
        Coord[] coords,
        double width, Color color,
        LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Round,
        // TODO: miter limit
        double[]? dasharray = null);

    public abstract void DrawLines(
        IEnumerable<Coord[]> lines,
        double width, Color color,
        LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Round,
        // TODO: miter limit
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
        IEnumerable<Coord[]> polygons,
        Color color);

    /// <summary>
    /// Fills the multipolygon specified by the given coordinates.
    /// </summary>
    /// <remarks>
    /// CCW polygons are outer polygons, CW are holes.
    /// </remarks>
    public abstract void DrawFilledMultiPolygon(
        IEnumerable<Coord[]> multiPolygon,
        Color color);

    /// <summary>
    /// Fills the multipolygons specified by the given coordinates.
    /// </summary>
    public abstract void DrawFilledMultiPolygons(
        IEnumerable<IEnumerable<Coord[]>> multiPolygons,
        Color color);


    public abstract void DrawCircles(IEnumerable<Coord> coords,
        double radius, double lineWidth, Color color);
    public virtual void DrawCircle(Coord coord,
        double radius, double lineWidth, Color color)
        => DrawCircles([coord], radius, lineWidth, color);

    public abstract void DrawFilledCircles(IEnumerable<Coord> coords,
        double radius, Color color);
    public virtual void DrawFilledCircle(Coord coord,
        double radius, Color color)
        => DrawFilledCircles([coord], radius, color);

    
    /// <returns>
    /// Measured width and height of the string (in canvas units)
    /// </returns>
    public abstract Coord GetTextSize(string font, double emSize, string s);

    /// <param name="emSize">
    /// Text em-size (total body height), in canvas units.
    /// </param>
    public abstract void DrawText(
        string fontName, double emSize, string s,
        Coord centerCoord, Color color);

    public abstract void DrawTextOutline(
        string fontName, double emSize, string s,
        Coord centerCoord, Color color, double lineWidth,
        LineJoin join = LineJoin.Round); // TODO: miter limit);

    public abstract void ApplyMasks(
        IList<Canvas> maskSources);
}