using MapLib.DataSources.Raster;
using MapLib.RasterOps;

namespace MapLib.Tests.DataSources;

[TestFixture]
public class SrtmDataSourceFixture : BaseFixture
{
    [Test]
    public async Task TestDownloadUnitedKingdom()
    {
        // Download and save at quarter resolution (full res is too large)
        SrtmDataSource source = new(scaleFactor: 0.25);
        RasterData data = await source.GetData(UnitedKingdomBounds);

        SingleBandRasterData? demData = data as SingleBandRasterData;
        Assert.That(demData, Is.Not.Null);

        ImageRasterData imageData = demData!
            .Normalize()
            .ToImageRasterData();
        Assert.That(imageData, Is.Not.Null);

        SaveTempBitmap(imageData.Bitmap, "SrtmDataSourceFixture_UK", ".jpg");
    }

    [Test]
    public async Task TestDownloadGlasgow()
    {
        // Download and save at quarter resolution (full res is too large)
        SrtmDataSource source = new(scaleFactor: 0.25);
        Bounds glasgowBounds = new(
            xmin: -4.50,
            xmax: -4.10, 
            ymin: 55.80,
            ymax: 55.95);
        RasterData data = await source.GetData(glasgowBounds);
        Assert.That(data, Is.Not.Null);
    }
}
