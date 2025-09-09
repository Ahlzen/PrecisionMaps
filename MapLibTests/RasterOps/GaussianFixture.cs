using MapLib.ColorSpace;
using MapLib.DataSources.Raster;
using MapLib.RasterOps;
using OSGeo.GDAL;

namespace MapLib.Tests.RasterOps;

[TestFixture]
public class GaussianFixture : BaseFixture
{
    [Test]
    [TestCase(0)]
    [TestCase(0.3)]
    [TestCase(1)]
    [TestCase(5)]
    [TestCase(50)]
    [TestCase(500)]
    public async Task TestGaussianBlur(float radius)
    {
        ImageRasterData blurredImage = GetSingleBandTestImage()
            .GaussianBlur(radius)
            .ToImageRasterData();
        SaveTempBitmap(blurredImage.Bitmap, "TestGaussianBlur_" + radius, ".jpg");
    }
}
