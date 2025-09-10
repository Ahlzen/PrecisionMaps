using MapLib.GdalSupport;
using MapLib.Geometry;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OSGeo.GDAL;

namespace MapLib;

public class ImageRasterData : RasterData
{
    /// <summary>
    /// Image data for image data sources (aerial imagery, maps, shaded
    /// relief etc).
    /// WidthPx * HeightPx * 4 bytes (ARGB).
    /// (byte order: B, G, R, A on little endian)
    /// </summary>
    public byte[] ImageData { get; }

    public ImageRasterData(Srs srs, Bounds bounds, int widthPx, int heightPx, byte[] imageData)
        : base(srs, bounds, widthPx, heightPx)
    {
        ImageData = imageData;
        _bitmapBuilder = new Lazy<Bitmap>(BuildBitmap);
    }

    public ImageRasterData(Srs srs, Bounds bounds, Bitmap bitmap)
        : base(srs, bounds, bitmap.Width, bitmap.Height)
    {
        int byteCount = WidthPx * HeightPx * 4;
        ImageData = new byte[byteCount];

        Bitmap argbBitmap = bitmap;

        // Convert to ARGB (if needed)
        if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
        {
            argbBitmap = new Bitmap(WidthPx, HeightPx, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(argbBitmap))
                g.DrawImage(bitmap, 0, 0, WidthPx, HeightPx);
        }

        // Copy raw data
        BitmapData bmpData = argbBitmap.LockBits(
            new Rectangle(0, 0, WidthPx, HeightPx),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            Marshal.Copy(bmpData.Scan0, ImageData, 0, byteCount);
        }
        finally
        {
            argbBitmap.UnlockBits(bmpData);
        }
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

    /// <summary>
    /// Converts this image to a raster of values.
    /// </summary>
    /// <remarks>
    /// The default weights are the standard luminance weights
    /// for converting RGB to grayscale. To extract a single band, use
    /// 1.0 for the desired channel and 0.0 for the others.
    /// </remarks>
    /// <param name="rWeight">Red channel weight.</param>
    /// <param name="gWeight">Green channel weight.</param>
    /// <param name="bWeight">Blue channel weight.</param>
    /// <param name="scale">The result is scaled by this value. By default,
    /// the image's range [0, 255] maps to [0.0, 1.0] (i.e. scaled by 1/255)
    /// </param>
    /// <param name="nodataForFullyTransparent">
    /// If set, any pixel that is fully transparent (alpha = 0)
    /// is assigned this no-data value. (the default -9999 is
    /// commonly used in rasters).
    /// </param>
    public SingleBandRasterData ToSingleBandRasterData(
        float rWeight = 0.2126f,
        float gWeight = 0.7152f,
        float bWeight = 0.0722f,
        float scale = 1f / 255f,
        float? noDataValue = -9999f)
    {
        long pixelCount = WidthPx * HeightPx;
        if (pixelCount > Array.MaxLength)
            throw new InvalidOperationException("Data is too large to convert to image.");

        float[] singleBandData = new float[pixelCount];
        for (long i = 0; i < pixelCount; i++)
        {
            long offset = i * 4;
            byte r = ImageData[offset + 2];
            byte g = ImageData[offset + 1];
            byte b = ImageData[offset + 0];
            byte a = ImageData[offset + 3];
            float value = scale * (
                r * rWeight +
                g * gWeight +
                b * bWeight);
            if (a == 0 && noDataValue != null)
                value = noDataValue.Value;
            singleBandData[i] = value;
        }
        return new SingleBandRasterData(Srs, Bounds, WidthPx, HeightPx,
            singleBandData, noDataValue);
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