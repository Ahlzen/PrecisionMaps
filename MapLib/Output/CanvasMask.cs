using MapLib.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Output;

[Obsolete]
public abstract class CanvasMask : IDisposable
{
    public abstract void Dispose();

    public string? Name { get; set; }

    public abstract void DrawLines(
        IEnumerable<Coord[]> lines, double width,
        LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Miter, // TODO: miter limit
        double[]? dasharray = null);
    public virtual void DrawLine(
        Coord[] coords, double width,
        LineCap cap = LineCap.Butt,
        LineJoin join = LineJoin.Miter, // TODO: miter limit
        double[]? dasharray = null)
        => DrawLines([coords], width, cap, join, dasharray);

    public abstract void DrawCircles(IEnumerable<Coord> coords,
        double radius, double lineWidth);
    public virtual void DrawCircle(Coord coord,
        double radius, double lineWidth)
        => DrawCircles([coord], radius, lineWidth);

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
}
