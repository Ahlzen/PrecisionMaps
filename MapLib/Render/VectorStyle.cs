using System.Drawing;

namespace MapLib.Render;

public record class VectorStyle
{
    // Polygon fill
    public Color? FillColor { get; set; } = null; // CartoCSS: polygon-fill

    // Polygon or line stroke
    public Color? LineColor { get; set; } = null; // CartoCSS: line-color
    public double? LineWidth { get; set; } = null; // CartoCSS: line-width

    // Text labels
    public string? TextTag { get; set; } = null; // CartoCSS: text-name
    public string? TextFont { get; set; } = null; // CartoCSS: text-face-name
    public double? TextSize { get; set; } = null; // CartoCSS: text-size

    // Symbols
    public string? Symbol { get; set; } = null;
}