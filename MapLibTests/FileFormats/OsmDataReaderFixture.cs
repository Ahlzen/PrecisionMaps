using MapLib.FileFormats.Vector;
using System.IO;

namespace MapLib.Tests.FileFormats;

[TestFixture]
internal class OsmDataReaderFixture : BaseFixture
{
    [Test]
    public void TestReadOsmData()
    {
        OsmDataReader reader = new();
        VectorData map = reader.ReadFile(Path.Join(TestDataPath, "map.osm"));

        Assert.That(map.Bounds.Width, Is.GreaterThan(0));
        Assert.That(map.Bounds.Height, Is.GreaterThan(0));
        Assert.That(map.Points, Is.Not.Empty);
        Assert.That(map.Lines, Is.Not.Empty);
        Assert.That(map.MultiPolygons, Is.Not.Empty);
    }
}
