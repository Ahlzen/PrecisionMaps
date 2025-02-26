using MapLib.GdalSupport;
using MapLib.Geometry;
using System.Drawing;
using OSGeo.GDAL;

namespace MapLib.FileFormats.Raster;

internal class GdalDataReader : IRasterFormatReader
{
    /// <summary>
    /// Reads the specified raster file into a bitmap.
    /// </summary>
    /// <param name="bounds">
    /// If specified, only the specified (projected) area is read.
    /// TODO: If null, the entire file is read.
    /// </param>
    public RasterData ReadFile(string filename, Bounds projectedBounds)
    {
        using Dataset dataset = GdalUtils.GetRasterDataset(filename);
        Bitmap bitmap = GdalUtils.GetBitmap(dataset, projectedBounds);
        string srs = GdalUtils.GetSrsAsWkt(dataset);
        RasterData raster = new RasterData(srs, projectedBounds, bitmap);
        return raster;
    }
}