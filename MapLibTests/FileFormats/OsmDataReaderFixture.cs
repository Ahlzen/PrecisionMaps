using MapLib.FileFormats.Vector;
using System.IO;

namespace MapLib.Tests.FileFormats;

[TestFixture]
internal class OsmDataReaderFixture : BaseFixture
{
    [Test]
    [TestCase("osm-xml/Aaron River Reservoir.osm")]
    [TestCase("osm-xml/Weymouth Detail.osm")]
    public void TestReadOsmData(string filename)
    {
        OsmDataReader reader = new();
        VectorData map = reader.ReadFile(Path.Join(TestDataPath, filename));

        // These should be non-null (by may be empty)
        Assert.That(map.Bounds, Is.Not.Null);
        Assert.That(map.Bounds.Width, Is.Not.Null);
        Assert.That(map.Bounds.Height, Is.Not.Null);
        Assert.That(map.Points, Is.Not.Null);
        Assert.That(map.Lines, Is.Not.Null);
        Assert.That(map.MultiPolygons, Is.Not.Null);

        // Check that there is at least _some_ geometry
        Assert.That(map.Points.Any() || map.Lines.Any() ||
            map.Polygons.Any() || map.MultiPolygons.Any());
    }
}
