using MapLib.FileFormats.Vector;
using System.IO;

namespace MapLibTests.FileFormats;

[TestFixture]
public class OgrDataReaderFixture : BaseFixture
{
    [Test]
    public void TestReadNaturalEarthPolygons()
    {
        // Read Natural Earth 10m Lakes
        OgrDataReader reader = new OgrDataReader();
        MapLib.VectorData data = reader.ReadFile(
            Path.Join(TestDataPath, "Natural Earth/ne_10m_lakes.shp"));
        Console.WriteLine(FormatVectorDataSummary(data));
    }

}
