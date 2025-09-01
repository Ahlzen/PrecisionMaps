using MapLib.ColorSpace;
using MapLib.DataSources.Raster;
using MapLib.RasterOps;

namespace MapLib.Tests.RasterOps;

[TestFixture]
public class SimpleRasterDataOpsFixture : BaseFixture
{
    [Test]
    public async Task TestHillshade()
    {
        // Get 3DEP DEM data
        Usgs3depDataSource source = new(scaleFactor: 0.25);
        RasterData data = await source.GetData(MassachusettsBounds);
        SingleBandRasterData? demData = data as SingleBandRasterData;
        Assert.That(demData, Is.Not.Null);

        // Run hillshade
        SingleBandRasterData hillshade = demData!
            .Scale(10)
            .Hillshade_Basic()
            .Offset(128f);
        ImageRasterData imageData = hillshade.ToImageRasterData(normalize: false);

        SaveTempBitmap(imageData.Bitmap, "TestHillshade", ".jpg");
    }

    [Test]
    public async Task TestGradientMap()
    {
        // Get 3DEP DEM data
        Usgs3depDataSource source = new(scaleFactor: 0.25);
        RasterData data = await source.GetData(MassachusettsBounds);
        SingleBandRasterData? demData = data as SingleBandRasterData;
        Assert.That(demData, Is.Not.Null);
        SimpleRasterDataOps.PrintMinMax(demData!.SingleBandData, null, null);

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

    [Test]
    public async Task TestShadedGradientMap()
    {
        // Get 3DEP DEM data
        Usgs3depDataSource source = new(scaleFactor: 0.25);
        RasterData data = await source.GetData(MassachusettsBounds);
        SingleBandRasterData? demData = data as SingleBandRasterData;
        Assert.That(demData, Is.Not.Null);

        // Run hillshade
        SingleBandRasterData hillshadeData = demData!
            .Scale(10)
            .Hillshade_Basic()
            .Offset(128f);
        ImageRasterData hillshadeImage = hillshadeData.ToImageRasterData(normalize: false);

        // Build hypsometric tint gradient
        Gradient gradient = new();
        gradient.Add(0.0f, (0.6f, 1.0f, 0.3f));
        gradient.Add(0.2f, (0.9f, 1.0f, 0.1f));
        gradient.Add(0.4f, (1.0f, 0.6f, 0.1f));
        gradient.Add(0.9f, (0.9f, 0.9f, 0.9f));
        gradient.Add(1.0f, (0.8f, 0.9f, 1.0f));
        ImageRasterData hypso = demData!
            .Normalize()
            .GradientMap(gradient);
        ImageRasterData steppedHypso = demData!
            .GenerateSteps(100, 0)
            .Normalize()
            .GradientMap(gradient);

        // Lighten, and blend using multiply
        //LevelAdjustment lighten = LevelAdjustment.AdjustMidpoint(0.6f);
        //var lightHillshadeData = lighten.Apply(hillshadeData);
        var lightHillshadeData = demData!
            .Scale(10)
            .Hillshade_Basic()
            .Normalize()
            .AdjustMidpoint(0.6f)
            .Offset(128f);
        ImageRasterData compositeMultiply = lightHillshadeData
            .ToImageRasterData()
            .BlendWith(hypso, BlendMode.Multiply, 0.5f);
        ImageRasterData compositeSteppedMultiply = lightHillshadeData
            .ToImageRasterData()
            .BlendWith(steppedHypso, BlendMode.Multiply, 0.5f);
        ImageRasterData compositeNormal = hillshadeImage
            .BlendWith(hypso, BlendMode.Normal, 0.3f);

        SaveTempBitmap(hillshadeImage.Bitmap, "TestShadedGradientMap_hillshade", ".jpg");
        SaveTempBitmap(lightHillshadeData.ToImageRasterData().Bitmap, "TestShadedGradientMap_lightHillshade", ".jpg");
        SaveTempBitmap(hypso.Bitmap, "TestShadedGradientMap_hypso", ".jpg");
        SaveTempBitmap(compositeMultiply.Bitmap, "TestShadedGradientMap_compositeMultiply", ".jpg");
        SaveTempBitmap(compositeNormal.Bitmap, "TestShadedGradientMap_compositeNormal", ".jpg");
        SaveTempBitmap(compositeSteppedMultiply.Bitmap, "TestShadedGradientMap_compositeSteppedMultiply", ".jpg");
    }
}
