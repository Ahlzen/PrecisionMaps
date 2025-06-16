using MapLib.GdalSupport;
using MapLib.Geometry;
using OSGeo.GDAL;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MapLib;

public abstract class RasterData2 : GeoData
{
    public override Bounds Bounds { get; }

    public int WidthPx { get; }
    public int HeightPx { get; }

    public RasterData2(string srs, Bounds bounds, int widthPx, int heightPx) : base(srs)
    {
        Bounds = bounds;
        WidthPx = widthPx;
        HeightPx = heightPx;
    }

    public double[] GetGeoTransform()
        => GdalUtils.GetGeoTransform(Bounds, WidthPx, HeightPx);

    public abstract Dataset ToInMemoryGdalDataset();
}

public class SingleBandRasterData : RasterData2
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

    public SingleBandRasterData(string srs, Bounds bounds, int widthPx, int heightPx,
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
}

public class ImageRasterData : RasterData2
{
    /// <summary>
    /// Image data for image data sources (aerial imagery, maps, shaded
    /// relief etc).
    /// WidthPx * HeightPx * 4 bytes (ARGB).
    /// (byte order: B, G, R, A on little endian)
    /// </summary>
    public byte[] ImageData { get; }

    public ImageRasterData(string srs, Bounds bounds, int widthPx, int heightPx, byte[] imageData)
        : base(srs, bounds, widthPx, heightPx)
    {
        ImageData = imageData;
        _bitmapBuilder = new Lazy<Bitmap>(BuildBitmap);
    }

    public Bitmap Bitmap => _bitmapBuilder.Value;
    private Lazy<Bitmap> _bitmapBuilder;
    private Bitmap BuildBitmap()
    {
        if (ImageData == null)
            throw new InvalidOperationException("Raster data is not an image.");
        Bitmap bitmap = new Bitmap(WidthPx, HeightPx, PixelFormat.Format32bppArgb);
        BitmapData bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.WriteOnly,
            bitmap.PixelFormat);
        IntPtr dataPointer = bitmapData.Scan0;
        Marshal.Copy(ImageData, 0, dataPointer, ImageData.Length);
        bitmap.UnlockBits(bitmapData);
        return bitmap;
    }

    public ImageRasterData CloneWithNewData(byte[] newImageData)
    {
        // Check that the length matches
        long expectedLength = HeightPx * WidthPx * 4;
        if (newImageData.LongLength != expectedLength)
            throw new ArgumentException("New data is not of correct length: " +
                $"Expected {expectedLength}, Was {newImageData.LongLength}",
                nameof(newImageData));

        ImageRasterData newData = new(
            Srs, Bounds, WidthPx, HeightPx, newImageData);
        return newData;
    }

    public override Dataset ToInMemoryGdalDataset()
    {
        (byte[] r, byte[] g, byte[] b, byte[] a) channels = SplitChannels(ImageData);
        double[] transform = GdalUtils.GetGeoTransform(Bounds, WidthPx, HeightPx);
        return GdalUtils.CreateInMemoryDataset(
            channels.r, channels.g, channels.b, channels.a,
            WidthPx, HeightPx, transform, Srs);
    }

    #region Helpers for splitting and merging channels

    private static float ByteToFloatScale = 1f / 255f;
    private static float FloatToByteScale = 255f / 1f;

    public static (byte[] r, byte[] g, byte[] b, byte[] a)
        SplitChannels(byte[] imageData) => (
            GetChannel(imageData, 2),
            GetChannel(imageData, 1),
            GetChannel(imageData, 0),
            GetChannel(imageData, 3));
    private static byte[] GetChannel(byte[] imageData, int byteOffset)
    {
        int pixelCount = imageData.Length / 4;
        byte[] channelData = new byte[pixelCount];
        for (int p = 0; p < pixelCount; p++)
            channelData[p] = imageData[p * 4 + byteOffset]; ;
        return channelData;
    }

    public static (float[] r, float[] g, float[] b, float[] a)
        SplitAndNormalizeChannels(byte[] imageData) => (
            SplitAndNormalizeChannel(imageData, 2),
            SplitAndNormalizeChannel(imageData, 1),
            SplitAndNormalizeChannel(imageData, 0),
            SplitAndNormalizeChannel(imageData, 3));
    private static float[] SplitAndNormalizeChannel(byte[] imageData, int byteOffset)
    {
        int pixelCount = imageData.Length / 4;
        float[] channelData = new float[pixelCount];
        for (int p = 0; p < pixelCount; p++)
        {
            byte sourceData = imageData[p * 4 + byteOffset];
            channelData[p] = ByteToFloatScale * sourceData;
        }
        return channelData;
    }

    public static byte[] MergeAndDenormalizeChannels(float[] r, float[] g, float[] b, float[] a)
    {
        byte[] dest = new byte[r.Length * 4];
        SetAndDenormalizeChannel(dest, 2, r);
        SetAndDenormalizeChannel(dest, 1, g);
        SetAndDenormalizeChannel(dest, 0, b);
        SetAndDenormalizeChannel(dest, 3, a);
        return dest;
    }
    private static void SetAndDenormalizeChannel(byte[] dest, int byteOffset, float[] channelData)
    {
        int pixelCount = channelData.Length;
        for (int p = 0; p < pixelCount; p++)
        {
            int destOffset = byteOffset + p * 4;
            dest[destOffset] = (byte)(channelData[p] * FloatToByteScale);
        }
    }

    #endregion
}