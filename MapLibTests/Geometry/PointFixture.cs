using MapLibTests;

namespace MapLib.Tests.Geometry;

[TestFixture]
[SupportedOSPlatform("windows")]
public class PointFixture
{
    private List<Coord> _testCoords = null!; // initialized in SetUp
    private MultiPoint _testMultiPoint = null!; // initialized in SetUp

    [SetUp]
    public void SetUp()
    {
        Random random = new Random(12345); // use known seed for predictable points
        _testCoords = new List<Coord>(
            Enumerable.Range(1, 50).Select(n => new Coord(
                random.NextDouble() * 20 - 10,
                random.NextDouble() * 20 - 10)));
        _testMultiPoint = new MultiPoint(_testCoords, null);
    }

    [Test]
    public void TestPoints_Show() {
        Visualizer.RenderAndShow(800, 800, _testCoords.ToArray());
    }

    [Test]
    public void TestMultiPoint_Show() {
        Visualizer.RenderAndShow(800, 800, _testMultiPoint);
    }

    [Test]
    public void TestBufferMultiPoint() {
        MultiPolygon buffer1 = _testMultiPoint.Buffer(0.1);
        MultiPolygon buffer2 = _testMultiPoint.Buffer(1);
        MultiPolygon buffer3 = _testMultiPoint.Buffer(10);
        Visualizer.RenderAndShow(800, 800, _testMultiPoint,
            buffer1, buffer2, buffer3);
    }
}
