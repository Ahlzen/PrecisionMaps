using MapLib.DataSources.Raster;

namespace MapLib.Tests.DataSources;

[TestFixture]
public class Usgs3depDataSourceFixture
{
    [Test]
    [Explicit]
    public async Task TestDownloadMassachusetts()
    {
        Bounds bounds = new(-73.30, -69.56, 41.14, 42.53);
        Usgs3depDataSource source = new();
        await source.GetData(bounds);
    }
}
