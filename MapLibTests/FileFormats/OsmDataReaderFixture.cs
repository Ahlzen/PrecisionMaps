using MapLib;
using MapLib.FileFormats.Vector;

namespace MapLibTests.FileFormats;

[TestFixture]
internal class OsmDataReaderFixture
{
    [Test]
    public void TestReadOsmData()
    {
        OsmDataReader reader = new();
        Console.WriteLine(Environment.CurrentDirectory);
        VectorData map = reader.ReadFile("../../../data/map.osm");
        Assert.That(map.Bounds.Width, Is.GreaterThan(0));
        Assert.That(map.Bounds.Height, Is.GreaterThan(0));
        Assert.That(map.Points, Is.Not.Empty);
        Assert.That(map.Lines, Is.Not.Empty);
        Assert.That(map.MultiPolygons, Is.Not.Empty);
    }
}
