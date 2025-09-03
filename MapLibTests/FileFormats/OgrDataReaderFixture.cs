using MapLib.DataSources.Vector;
using MapLib.FileFormats.Vector;
using MapLib.Util;
using System.IO;

namespace MapLib.Tests.FileFormats;

[TestFixture]
public class OgrDataReaderFixture : BaseFixture
{
    [Test]
    public async Task TestReadShapefile_Polygons()
    {
        // Read 10m Lakes shapefile (from Natural Earth)
        await new NaturalEarthVectorDataSource(NaturalEarthVectorDataSet.LandPolygons_10m).Download();
        string sourcePath = Path.Join(FileSystemHelpers.SourceCachePath,
            "NaturalEarth_Vector/ne_10m_lakes.shp");
        OgrDataReader reader = new OgrDataReader();
        MapLib.VectorData data = reader.ReadFile(sourcePath);
        Console.WriteLine(Visualizer.FormatVectorDataSummary(data));
    }

    [Test]
    public void TestReadGeoJson_Polygon()
    {
        // GeoJSON file with single object from OSM
        OgrDataReader reader = new OgrDataReader();
        MapLib.VectorData data = reader.ReadFile(
            Path.Join(TestDataPath, "Aaron River/Aaron River Reservoir.geojson"));
        Console.WriteLine(Visualizer.FormatVectorDataSummary(data));
    }

    [Test]
    public void TestReadKml_Polygon()
    {
        // KML file with single object from OSM
        OgrDataReader reader = new OgrDataReader();
        MapLib.VectorData data = reader.ReadFile(
            Path.Join(TestDataPath, "Aaron River/Aaron River Reservoir.kml"));
        Console.WriteLine(Visualizer.FormatVectorDataSummary(data));
    }

}
