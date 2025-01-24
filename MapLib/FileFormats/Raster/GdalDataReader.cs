using MapLib.Gdal;
using MapLib.Geometry;
using System.Drawing;

namespace MapLib.FileFormats.Raster;

internal class GdalDataReader : IRasterFormatReader
{
    /// <summary>
    /// Reads the specified raster file into a bitmap.
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="bounds">
    /// If specified, only the specified (projected) area is read.
    /// TODO: If null, the entire file is read.
    /// </param>
    /// <returns></returns>
    public RasterData ReadFile(string filename, Bounds projectedBounds)
    {
        // TODO: Specify/support projected vs screen bounds

        Bitmap bitmap = GdalUtils.GetBitmap(
            filename, projectedBounds);
        RasterData raster = new RasterData(
            projectedBounds, bitmap);
        return raster;
    }
}