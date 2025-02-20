namespace MapLib.Tests.Geometry;

[TestFixture]
[SupportedOSPlatform("windows")]
public class PolygonFixture : BaseFixture
{

    [Test]
    public void TestValidPolygon() {
        var p1 = new Polygon([(0, 0), (0, 1), (2, 1), (2, 0), (0, 0)], null);
    }

    [Test]
    public void TestInvalidPolygon()
    {
        // Last point must be same as first point
        Assert.Throws<ArgumentException>(() => new
            Polygon([(0, 0), (0, 1), (2, 1), (2, 0)], null));
    }

    [Test]
    public void TestArea_Square()
    {
        var p1cw = new Polygon([(0, 0), (0, 1), (2, 1), (2, 0), (0, 0)], null);
        var p1ccw = p1cw.Reverse();
        Visualizer.RenderAndShow(800, 800, p1ccw);

        Assert.That(p1cw.Area, Is.EqualTo(-2.0).Within(0.001));
        Assert.That(p1cw.IsClockwise);
        Assert.That(p1cw.IsCounterClockwise, Is.False);

        Assert.That(p1ccw.Area, Is.EqualTo(2.0).Within(0.001));
        Assert.That(p1ccw.IsCounterClockwise);
        Assert.That(p1ccw.IsClockwise, Is.False);
    }
}
