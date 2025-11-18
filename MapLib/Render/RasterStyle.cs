namespace MapLib.Render;

/// <remarks>
/// Any sizes/distances are in map units unless otherwise specified.
/// </remarks>
public record class RasterStyle
{
    /// <summary>
    /// Optional. Name of mask this layer is masked by.
    /// </summary>
    public List<string> MaskedBy { get; set; } = new();

    /// <summary>
    /// Optional. Name of the mask this layer creates.
    /// </summary>
    public string? MaskName { get; set; }


    // TODO: add properties for raster style
}
