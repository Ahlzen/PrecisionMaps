using MapLib.ColorSpace;
using MapLib.DataSources.Raster;
using MapLib.RasterOps;
using OSGeo.GDAL;

namespace MapLib.Tests.RasterOps;

[TestFixture]
public class HillshadeFixture : BaseFixture
{
    [Test]
    public async Task TestHillshade()
    {
        SingleBandRasterData demData = await GetTestDemData();
        SingleBandRasterData hillshade = demData
            .Scale(10)
            .Hillshade_Basic()
            .Offset(128f);
        ImageRasterData imageData = hillshade
            .Normalize()
            .ToImageRasterData();
        SaveTempBitmap(imageData.Bitmap, "TestHillshade", ".jpg");
    }

    [Test]
    public async Task TestShadedGradientMap()
    {
        // Run hillshade
        SingleBandRasterData demData = await GetTestDemData();
        SingleBandRasterData hillshadeData = demData
            .Scale(10)
            .Hillshade_Basic()
            .Offset(128f);
        ImageRasterData hillshadeImage = hillshadeData
            .Normalize()
            .ToImageRasterData();

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
        var lightHillshadeData = demData
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
