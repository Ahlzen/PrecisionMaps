using MapLib.Geometry;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MapLib;

public class RasterData : GeoData
{
    /// <param name="srs">SRS of raster data.</param>
    /// <param name="bounds">Bounds (in source/dataset SRS)</param>
    /// <param name="bitmap">Bitmap containing the raster data/layer.</param>
    public RasterData(string srs, Bounds bounds, Bitmap bitmap)
        : base(srs)
    {
        // TODO: implement properly
        Bounds = bounds;
        Bitmap = bitmap;
    }
    public override Bounds Bounds { get; }
    public Bitmap Bitmap { get; }
}

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
}

public class SingleBandRasterData : RasterData2
{
    /// <summary>
    /// Values  for data sources that contain a single band of values
    /// rather than images, such as DEMs, bathymetry and other grid measurements.
    /// WidthPx * HeightPx * 4 bytes (Float32).
    /// </summary>
    public float[]? SingleBandData { get; }

    public SingleBandRasterData(string srs, Bounds bounds, int widthPx, int heightPx, float[] singleBandData)
    : base(srs, bounds, widthPx, heightPx)
    {
        SingleBandData = singleBandData;
    }
}

public class ImageRasterData : RasterData2
{
    /// <summary>
    /// Image data for image data sources (aerial imagery, maps, shaded
    /// relief etc).
    /// WidthPx * HeightPx * 4 bytes (ARGB).
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
}