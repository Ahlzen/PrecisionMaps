using MapLib.FileFormats.Vector;
using System.IO;

namespace MapLib.Tests.FileFormats;

[TestFixture]
public class GeoJsonDataReaderFixture : BaseFixture
{
    [Test]
    public void TestReadGeoJson_Native_Polygons()
    {
        // GeoJSON file with single object from OSM
        GeoJsonDataReader reader = new();
        VectorData data = reader.ReadFile(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"));
        Console.WriteLine(Visualizer.FormatVectorDataSummary(data));
    }
}
