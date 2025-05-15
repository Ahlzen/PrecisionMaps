using MapLib.DataSources.Raster;
using System.IO;
using MapLib.GdalSupport;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace MapLib.Tests.DataSources;

[Obsolete]
[TestFixture]
public class GdalDataSourceFixture : BaseFixture
{
    [Test]
    public void TestReadGdalRaster()
    {
        GdalDataSource dataSource = new(Path.Join(TestDataPath, "MassGIS LiDAR/be_19TCG339672.tif"));
        Console.WriteLine("SRS: " + dataSource.Srs);
        Console.WriteLine("Bounds: " + dataSource.Bounds);
        Console.WriteLine("Bounds (WGS84): " + dataSource.Bounds?.ToWgs84(dataSource.Srs));
        Console.WriteLine("Dimensions: " + dataSource.WidthPx + " x " + dataSource.HeightPx);

        RasterData data = dataSource.GetData();
        Console.WriteLine("Bitmap PixelFormat: " + data.Bitmap.PixelFormat);

        Assert.That(data.Bitmap, Is.Not.Null);
        Assert.That(data.Bitmap.Width, Is.EqualTo(dataSource.WidthPx));
        Assert.That(data.Bitmap.Height, Is.EqualTo(dataSource.HeightPx));
    }

    [Test]
    public void TestTransform()
    {
        string sourceFilename = Path.Join(
            TestDataPath, "MassGIS LiDAR/be_19TCG339672.tif");

        // Source is "EPSG:6348 - NAD83(2011) / UTM zone 19N"
        // Transform to EPSG:3857
        string destFilename = GdalUtils.Warp(sourceFilename, "EPSG:3857");

        Console.WriteLine(destFilename);

        using Dataset destDataset = GdalUtils.OpenDataset(destFilename);
        SpatialReference sr = GdalUtils.GetSpatialReference(destDataset);

        // Check that dest is indeed 3857
        Assert.That(sr.AutoIdentifyEPSG(), Is.EqualTo(3857));
    }
}
