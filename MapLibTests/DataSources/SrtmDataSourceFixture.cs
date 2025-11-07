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
}
