using NUnit.Framework;
using MapLib.Geometry;
using System.Runtime.Versioning;

namespace MapLib.Tests.Geometry;

[TestFixture]
[SupportedOSPlatform("windows")]
internal class PolygonFixture
{

    [Test]
    public void TestValidPolygon() {
        var p1 = new Polygon([(0, 0), (0, 1), (2, 1), (2, 0), (0, 0)]);
    }

    [Test]
    public void TestInvalidPolygon()
    {
        // Last point must be same as first point
        Assert.Throws<ArgumentException>(() =>
            new Polygon([(0, 0), (0, 1), (2, 1), (2, 0)]));
    }

    [Test]
    public void TestArea_Square()
    {
        var p1cw = new Polygon([(0, 0), (0, 1), (2, 1), (2, 0), (0, 0)]);
        var p1ccw = p1cw.Reverse();
        Visualizer.RenderAndShow(800, 800, p1ccw);

        Assert.That(p1cw.Area, Is.EqualTo(-2.0).Within(0.001));
        Assert.That(p1cw.IsClockwise);
        Assert.That(p1cw.IsCounterClockwise, Is.False);

        Assert.That(p1ccw.Area, Is.EqualTo(2.0).Within(0.001));
        Assert.That(p1ccw.IsCounterClockwise);
        Assert.That(p1ccw.IsClockwise, Is.False);
    }

    private static readonly Polygon TestPolygon1 =
        new Polygon([(1,1),(8,-2),(7,5),(6,2),(5,3),(1,1)]);

    [Test]
    public void TestOffset_Outward()
    {
        MultiPolygon offset1 = TestPolygon1.Offset(0.2);
        MultiPolygon offset2 = offset1.Offset(0.4);
        MultiPolygon offset3 = offset2.Offset(0.8);
        Visualizer.RenderAndShow(800, 500, TestPolygon1,
            offset1, offset2, offset3);
    }

    [Test]
    public void TestOffset_Inward()
    {
        MultiPolygon offset1 = TestPolygon1.Offset(-0.2);
        MultiPolygon offset2 = offset1.Offset(-0.4);
        MultiPolygon offset3 = offset2.Offset(-0.85);

        // Offset past when there's no area left:
        MultiPolygon offset4 = offset3.Offset(-0.5);
        Assert.That(offset4, Has.Exactly(0).Items);

        Visualizer.RenderAndShow(800, 500, TestPolygon1,
            offset1, offset2, offset3);
    }
}
