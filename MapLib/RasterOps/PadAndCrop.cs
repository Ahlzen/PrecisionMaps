namespace MapLib.RasterOps;

/// <summary>
/// Helper methods for padding (extending) and cropping (reducing)
/// raw image data.
/// </summary>
public static class PadAndCrop
{
    /// <summary>
    /// Crops the raster to the specified dimensions.
    /// </summary>
    public static float[] Crop(
        SingleBandRasterData source,
        int x, int y, int width, int height)
        => Crop(source.SingleBandData, source.WidthPx, source.HeightPx,
            x, y, width, height);
    public static float[] Crop(
        float[] source, int sourceWidth, int sourceHeight,
        int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || width <= 0 || height <= 0 ||
            x + width > sourceWidth || y + height > sourceHeight)
            throw new ArgumentOutOfRangeException(
                "Crop rectangle is out of bounds of source image.");

        float[] cropped = new float[width * height];
        for (int row = 0; row < height; row++)
            Array.Copy(
                source, (y + row) * sourceWidth + x,
                cropped, row * width,
                width);
        return cropped;
    }

    /// <summary>
    /// Pads (extends) the raster with the specified amount,
    /// filling new pixels with the given value.
    /// </summary>
    public static float[] PadWithSingleValue(
        SingleBandRasterData source,
        int top, int right, int bottom, int left, float value)
        => PadWithSingleValue(
            source.SingleBandData, source.WidthPx, source.HeightPx,
            top, right, bottom, left, value);
    public static float[] PadWithSingleValue(
        float[] source, int sourceWidth, int sourceHeight,
        int top, int right, int bottom, int left, float value)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
            throw new ArgumentOutOfRangeException(
                "Source width and height must be positive.");
        if (top < 0 || right < 0 || bottom < 0 || left < 0)
            throw new ArgumentOutOfRangeException(
                "Padding amounts must be non-negative.");

        float[] padded = new float[
            (sourceHeight + top + bottom) * (sourceWidth + left + right)];
        Array.Fill(padded, value);
        for (int row = 0; row < sourceHeight; row++)
            Array.Copy(
                source, row * sourceWidth,
                padded, (row + top) * (sourceWidth + left + right) + left,
                sourceWidth);
        return padded;
    }

    /// <summary>
    /// Pads (extends) the raster with the specified amount,
    /// filling new pixels with the edge and corner values from source.
    /// </summary>
    public static float[] PadExtendingEdges(
        SingleBandRasterData source,
        int top, int right, int bottom, int left)
        => PadExtendingEdges(
            source.SingleBandData, source.WidthPx, source.HeightPx,
            top, right, bottom, left);
    public static float[] PadExtendingEdges(
        float[] source, int sourceWidth, int sourceHeight,
        int top, int right, int bottom, int left)
    {
        float[] padded = PadWithSingleValue(
            source, sourceWidth, sourceHeight,
            top, right, bottom, left, 0f);
        // fill left/right edges
        for (int row = 0; row < sourceHeight; row++)
        {
            float leftValue = source[row * sourceWidth];
            for (int col = 0; col < left; col++)
                padded[(row + top) * (sourceWidth + left + right) + col] = leftValue;
            float rightValue = source[row * sourceWidth + sourceWidth - 1];
            for (int col = 0; col < right; col++)
                padded[(row + top) * (sourceWidth + left + right) + left + sourceWidth + col] = rightValue;
        }
        // fill top/bottom edges
        for (int col = 0; col < sourceWidth; col++)
        {
            float topValue = source[col];
            for (int row = 0; row < top; row++)
                padded[row * (sourceWidth + left + right) + left + col] = topValue;
            float bottomValue = source[(sourceHeight - 1) * sourceWidth + col];
            for (int row = 0; row < bottom; row++)
                padded[(row + top + sourceHeight) * (sourceWidth + left + right) + left + col] = bottomValue;
        }
        // fill corners
        float topLeftValue = source[0];
        for (int row = 0; row < top; row++)
            for (int col = 0; col < left; col++)
                padded[row * (sourceWidth + left + right) + col] = topLeftValue;
        float topRightValue = source[sourceWidth - 1];
        for (int row = 0; row < top; row++)
            for (int col = 0; col < right; col++)
                padded[row * (sourceWidth + left + right) + left + sourceWidth + col] = topRightValue;
        float bottomLeftValue = source[(sourceHeight - 1) * sourceWidth];
        for (int row = 0; row < bottom; row++)
            for (int col = 0; col < left; col++)
                padded[(row + top + sourceHeight) * (sourceWidth + left + right) + col] = bottomLeftValue;
        float bottomRightValue = source[(sourceHeight - 1) * sourceWidth + sourceWidth - 1];
        for (int row = 0; row < bottom; row++)
            for (int col = 0; col < right; col++)
                padded[(row + top + sourceHeight) * (sourceWidth + left + right) + left + sourceWidth + col] = bottomRightValue;
        return padded;
    }
}
