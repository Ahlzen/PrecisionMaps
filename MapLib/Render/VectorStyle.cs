using MapLib.Output;
using System.Drawing;

namespace MapLib.Render;

/// <remarks>
/// Any sizes/distances are in map units unless otherwise specified.
/// </remarks>
public record class VectorStyle
{
    /// <summary>
    /// Optional. Name of mask this layer is masked by.
    /// </summary>
    //public List<string> MaskedBy { get; } = new();
    public List<string> MaskedBy { get; set; } = new();

    /// <summary>
    /// Optional. Name of the mask this layer creates.
    /// </summary>
    public string? MaskName { get; set; }

    // Polygon fill
    public Color? FillColor { get; set; } = null; // CartoCSS: polygon-fill
    public double? PolygonMaskWidth { get; set; } = null;

    // Polygon or line stroke
    public Color? LineColor { get; set; } = null; // CartoCSS: line-color
    public double? LineWidth { get; set; } = null; // CartoCSS: line-width
    public double? LineMaskWidth { get; set; } = null;

    // Symbols
    public SymbolType? Symbol { get; set; } = null;
    public double? SymbolSize { get; set; } = null;
    public Color? SymbolColor { get; set; } = null;
    public double? SymbolMaskWidth { get; set; } = null;
    // TODO: symbol styling
    // TODO: symbol image path

    // Text labels
    public string? TextTag { get; set; } = null; // CartoCSS: text-name
    public string? TextFont { get; set; } = null; // CartoCSS: text-face-name
    public double? TextSize { get; set; } = null; // CartoCSS: text-size
    public Color? TextColor { get; set; } = null; // CartoCSS: text-fill
    public double? TextMaskWidth { get; set; } = null;

}
