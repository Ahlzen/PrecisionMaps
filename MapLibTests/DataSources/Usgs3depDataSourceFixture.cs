using MapLib.DataSources.Raster;
using MapLib.RasterOps;

namespace MapLib.Tests.DataSources;

[TestFixture]
public class Usgs3depDataSourceFixture : BaseFixture
{
    [Test]
    [Explicit]
    public async Task TestDownloadMassachusetts()
    {
        Bounds bounds = new(-73.30, -69.56, 41.14, 42.53);
        
        // Download and save at quarter resolution (full res is too large)
        Usgs3depDataSource source = new(scaleFactor: 0.25);
        RasterData2 data = await source.GetData(bounds);
        
        SingleBandRasterData? demData = data as SingleBandRasterData;
        Assert.That(demData, Is.Not.Null);

        ImageRasterData imageData = demData!.ToImageRasterData(normalize: true);
        Assert.That(imageData, Is.Not.Null);

        SaveTempBitmap(imageData.Bitmap, "Massachusetts_DEM_3DEP", ".jpg");
    }
}
