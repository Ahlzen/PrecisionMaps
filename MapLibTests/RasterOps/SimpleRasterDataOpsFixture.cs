using MapLib.ColorSpace;
using MapLib.DataSources.Raster;
using MapLib.RasterOps;

namespace MapLib.Tests.RasterOps;

[TestFixture]
public class SimpleRasterDataOpsFixture : BaseFixture
{
    [Test]
    public async Task TestGradientMap()
    {
        // Get 3DEP DEM data
        Usgs3depDataSource source = new(scaleFactor: 0.25);
        RasterData data = await source.GetData(MassachusettsBounds);
        SingleBandRasterData? demData = data as SingleBandRasterData;
        Assert.That(demData, Is.Not.Null);
        RasterDataOpsHelpers.PrintDebugInfo(demData!.SingleBandData, null, null);

        // Build hypsometric tint gradient
        Gradient gradient = new();
        gradient.Add(0.0f, (0.4f, 0.7f, 0.3f));
        gradient.Add(0.2f, (0.7f, 0.7f, 0.3f));
        gradient.Add(0.4f, (0.7f, 0.4f, 0.0f));
        gradient.Add(0.9f, (0.7f, 0.7f, 0.7f));
        gradient.Add(1.0f, (0.85f, 0.95f, 1.0f));

        ImageRasterData hypso = demData!
            .Normalize()
            .GradientMap(gradient);
        SaveTempBitmap(hypso.Bitmap, "TestGradientMap", ".jpg");

        ImageRasterData steppedHypso = demData!
            .GenerateSteps(100, 0)
            .Normalize()
            .GradientMap(gradient);
        SaveTempBitmap(steppedHypso.Bitmap, "TestGradientMap_stepped", ".jpg");
    }
}
