using MapLib.FileFormats.Vector;
using System.IO;

namespace MapLib.Tests.FileFormats;

[TestFixture]
public class OgrDataReaderFixture : BaseFixture
{
    [Test]
    public void TestReadShapefile_Polygons()
    {
        // Read Natural Earth 10m Lakes
        OgrDataReader reader = new OgrDataReader();
        MapLib.VectorData data = reader.ReadFile(
            Path.Join(TestDataPath, "Natural Earth/ne_10m_lakes.shp"));
        Console.WriteLine(Visualizer.FormatVectorDataSummary(data));
    }

    [Test]
    public void TestReadGeoJson_Polygon()
    {
        // GeoJSON file with single object from OSM
        OgrDataReader reader = new OgrDataReader();
        MapLib.VectorData data = reader.ReadFile(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"));
        Console.WriteLine(Visualizer.FormatVectorDataSummary(data));
    }

    [Test]
    public void TestReadKml_Polygon()
    {
        // KML file with single object from OSM
        OgrDataReader reader = new OgrDataReader();
        MapLib.VectorData data = reader.ReadFile(
            Path.Join(TestDataPath, "Aaron River Reservoir.kml"));
        Console.WriteLine(Visualizer.FormatVectorDataSummary(data));
    }

}
