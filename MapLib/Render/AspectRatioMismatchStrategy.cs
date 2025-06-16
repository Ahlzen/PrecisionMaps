namespace MapLib.Render;

/// <summary>
/// Strategies for handling the (common) case where the projection
/// of the requested area doesn't exactly match the aspect ratio
/// of the destination canvas.
/// </summary>
public enum AspectRatioMismatchStrategy
{
    /// <summary>
    /// Stretch the data to fit the canvas. Typically results
    /// in skewed objects.
    /// </summary>
    StretchToFillCanvas,

    /// <summary>
    /// Include only the requested area, centering the data
    /// on the canvas. Typically results in empty space on
    /// the left/right or top/bottom.
    /// </summary>
    CenterOnCanvas,

    /// <summary>
    /// Crop (in either x or y) the requested area to
    /// match the canvas aspect ratio. Typically results in parts
    /// of the requested area not being included on the map.
    /// </summary>
    CropBounds,

    /// <summary>
    /// Extend (in either x or y) the requested area to
    /// match the canvas aspect ratio. Typically results in a larger
    /// area than requested included on the map.
    /// </summary>
    ExtendBounds,
}