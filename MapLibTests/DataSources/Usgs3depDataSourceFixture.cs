using MapLib.DataSources.Raster;
using MapLib.RasterOps;

namespace MapLib.Tests.DataSources;

[TestFixture]
public class Usgs3depDataSourceFixture : BaseFixture
{
    [Test]
    public async Task TestDownloadMassachusetts()
    {
        // Download and save at quarter resolution (full res is too large)
        Usgs3depDataSource source = new(scaleFactor: 0.25);
        RasterData data = await source.GetData(MassachusettsBounds);
        
        SingleBandRasterData? demData = data as SingleBandRasterData;
        Assert.That(demData, Is.Not.Null);

        ImageRasterData imageData = demData!.ToImageRasterData(normalize: true);
        Assert.That(imageData, Is.Not.Null);

        SaveTempBitmap(imageData.Bitmap, "Usgs3depDataSourceFixture_3DEP", ".jpg");
    }
}
