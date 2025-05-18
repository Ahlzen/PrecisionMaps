using MapLib.Output;
using System.Drawing;

namespace MapLib.Render;

public record class VectorStyle
{
    // Polygon fill
    public Color? FillColor { get; set; } = null; // CartoCSS: polygon-fill

    // Polygon or line stroke
    public Color? LineColor { get; set; } = null; // CartoCSS: line-color
    public double? LineWidth { get; set; } = null; // in map units. CartoCSS: line-width

    // Symbols
    public SymbolType? Symbol { get; set; } = null;
    public double? SymbolSize { get; set; } = null; // in map units.
    public Color? SymbolColor { get; set; } = null; 
    // TODO: symbol styling
    // TODO: symbol image path

    // Text labels
    public string? TextTag { get; set; } = null; // CartoCSS: text-name
    public string? TextFont { get; set; } = null; // CartoCSS: text-face-name
    public double? TextSize { get; set; } = null; // in map units. CartoCSS: text-size
    public Color? TextColor { get; set; } = null; // CartoCSS: text-fill
}
