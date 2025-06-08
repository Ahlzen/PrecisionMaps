using MapLib.ColorSpace;
using MapLib.DataSources.Raster;
using MapLib.RasterOps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Tests.RasterOps;

[TestFixture]
public class SimpleRasterDataOpsFixture : BaseFixture
{
    [Test]
    [Explicit]
    public async Task TestHillshade()
    {
        // Get 3DEP DEM data
        Usgs3depDataSource source = new(scaleFactor: 0.25);
        RasterData2 data = await source.GetData(MassachusettsBounds);
        SingleBandRasterData? demData = data as SingleBandRasterData;
        Assert.That(demData, Is.Not.Null);

        // Run hillshade
        SingleBandRasterData hillshade = demData!
            .GetScaled(10)
            .GetHillshade_Basic()
            .GetWithOffset(128f);
        ImageRasterData imageData = hillshade.ToImageRasterData(normalize: false);

        SaveTempBitmap(imageData.Bitmap, "TestHillshade", ".jpg");
    }

    [Test]
    [Explicit]
    public async Task TestGradientMap()
    {
        // Get 3DEP DEM data
        Usgs3depDataSource source = new(scaleFactor: 0.25);
        RasterData2 data = await source.GetData(MassachusettsBounds);
        SingleBandRasterData? demData = data as SingleBandRasterData;
        Assert.That(demData, Is.Not.Null);

        // Build hypsometric tint gradient
        Gradient gradient = new();
        gradient.Add(0.0f, (0.4f, 0.7f, 0.3f));
        gradient.Add(0.2f, (0.6f, 0.7f, 0.2f));
        gradient.Add(0.4f, (0.7f, 0.4f, 0.1f));
        gradient.Add(0.9f, (0.7f, 0.7f, 0.7f));
        gradient.Add(1.0f, (0.9f, 0.95f, 1.0f));

        ImageRasterData hypso = demData!
            .GetNormalized()
            .GetGradientMap(gradient);
        SaveTempBitmap(hypso.Bitmap, "TestGradientMap", ".jpg");
    }
}
