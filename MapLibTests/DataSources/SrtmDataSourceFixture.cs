using MapLib.DataSources.Raster;
using MapLib.GdalSupport;
using MapLib.RasterOps;

namespace MapLib.Tests.DataSources;

[TestFixture]
public class SrtmDataSourceFixture : BaseFixture
{
    private static IEnumerable<Srs?> TestProjections()
    {
        yield return null;
        yield return Srs.Robinson;
    }

    [Test]
    [TestCaseSource("TestProjections")]
    public async Task TestDownloadUnitedKingdom(Srs? srs)
    {
        // Download and save at quarter resolution (full res is too large)
        SrtmDataSource source = new(scaleFactor: 0.25);
        RasterData data;

        if (srs != null)
            data = await source.GetData(UnitedKingdomBounds, srs);
        else
            data = await source.GetData(UnitedKingdomBounds);

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
