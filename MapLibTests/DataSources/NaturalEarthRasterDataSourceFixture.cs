using MapLib.DataSources.Raster;
using MapLib.DataSources.Vector;
using System.Diagnostics.CodeAnalysis;

namespace MapLib.Tests.DataSources;

[TestFixture]
public class NaturalEarthRasterDataSourceFixture : BaseFixture
{
    /// <summary>
    /// Downloads and caches all NE raster data sets.
    /// </summary>
    [Test]
    [Explicit]
    public async Task DownloadAllNaturalEarthRasterData()
    {
        foreach (NaturalEarthRasterDataSet dataSet in Enum.GetValues<NaturalEarthRasterDataSet>())
        {
            Console.WriteLine("Downloading " + dataSet);
            try
            {
                await new NaturalEarthRasterDataSource(dataSet).GetData();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
