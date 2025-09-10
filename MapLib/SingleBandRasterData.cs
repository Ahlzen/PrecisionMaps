using MapLib.GdalSupport;
using MapLib.Geometry;
using OSGeo.GDAL;

namespace MapLib;

public class SingleBandRasterData : RasterData
{
    /// <summary>
    /// Values  for data sources that contain a single band of values
    /// rather than images, such as DEMs, bathymetry and other grid measurements.
    /// WidthPx * HeightPx * 4 bytes (Float32).
    /// </summary>
    public float[] SingleBandData { get; }

    /// <summary>
    /// If non-null, this value represents a point
    /// in the raster with no data.
    /// </summary>
    public float? NoDataValue { get; }

    public SingleBandRasterData(Srs srs, Bounds bounds, int widthPx, int heightPx,
        float[] singleBandData, float? noDataValue)
    : base(srs, bounds, widthPx, heightPx)
    {
        SingleBandData = singleBandData;
        NoDataValue = noDataValue;
    }

    /// <summary>
    /// Creates a clone of this ImageRasterData, but with new
    /// content (keeping all other metadata).
    /// </summary>
    public SingleBandRasterData CloneWithNewData(float[] newImageData)
    {
        // Check that the length matches
        long expectedLength = HeightPx * WidthPx;
        if (newImageData.LongLength != expectedLength)
            throw new ArgumentException("New data is not of correct length: " +
                $"Expected {expectedLength}, Was {newImageData.LongLength}",
                nameof(newImageData));

        SingleBandRasterData newData = new SingleBandRasterData(
            Srs, Bounds, WidthPx, HeightPx, newImageData, NoDataValue);
        return newData;
    }

    public override Dataset ToInMemoryGdalDataset()
    {
        return GdalUtils.CreateInMemoryDataset(
            SingleBandData, WidthPx, HeightPx,
            GdalUtils.GetGeoTransform(Bounds, WidthPx, HeightPx),
            Srs, NoDataValue);
    }

    /// <summary>
    /// Converts this data to a monochrome RGB image.
    /// </summary>
    /// <param name="scale">
    /// Multipler to scale input values by. By default, values are
    /// scaled so that the range [0.0, 1.0] maps to [0, 255].
    /// </param>
    public ImageRasterData ToImageRasterData(float scale = 255)
    {
        long pixelCount = WidthPx * HeightPx;
        if ((pixelCount * 4) > Array.MaxLength)
            throw new InvalidOperationException("Data is too large to convert to image.");

        byte[] imageData = new byte[pixelCount * 4];
        for (long i = 0; i < pixelCount; i++)
        {
            float v = SingleBandData[i];
            byte byteValue = 0;
            byte opacity = 255;
            if (NoDataValue != null && v == NoDataValue.Value)
                opacity = 0; // No data -> transparent
            else
                byteValue = (byte)Math.Clamp(Math.Round(v * scale), 0, 255);
            long offset = i * 4;
            imageData[offset + 0] = byteValue; // B
            imageData[offset + 1] = byteValue; // G
            imageData[offset + 2] = byteValue; // R
            imageData[offset + 3] = opacity;   // A
        }
        return new ImageRasterData(Srs, Bounds, WidthPx, HeightPx, imageData);
    }
}