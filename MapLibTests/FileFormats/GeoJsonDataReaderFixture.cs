using MapLib.FileFormats.Vector;
using System.IO;
using MapLib.FileFormats;

namespace MapLib.Tests.FileFormats;

[TestFixture(typeof(GeoJsonDataReader))]
[TestFixture(typeof(OgrDataReader))]
public class GeoJsonDataReaderFixture<TReader>
    : BaseFixture where TReader : IVectorFormatReader, new()
{
    public static string[] ExampleFilenames = {
        "Aaron River Reservoir.geojson", // single multipolygon
        "openlayers-line-samples.geojson",
        "openlayers-polygon-samples.geojson",
        "openlayers-vienna-streets.geojson",
        "openlayers-world-cities.geojson"
    };

    [Test]
    public void TestReadGeoJson_ExampleFiles(
        [ValueSource("ExampleFilenames")] string filename)
    {
        IVectorFormatReader reader = new TReader();
        VectorData data = reader.ReadFile(
            Path.Join(TestDataPath, "GeoJSON", filename));
        Assert.That(data.Count, Is.GreaterThan(0));
        Console.WriteLine(Visualizer.FormatVectorDataSummary(data));
    }
}
