using MapLib.Geometry.Helpers;
using MapLib.Output;
using MapLibTests;
using System.Drawing;
using System.IO;

namespace MapLib.Tests.Geometry;

[TestFixture]
[SupportedOSPlatform("windows")]
internal class PolygonFixture : BaseFixture
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

    private static readonly Polygon TestPolygon1 =
        new Polygon([(1,1),(8,-2),(7,5),(6,2),(5,3),(1,1)], null);

    private static readonly Line TestLine1 =
        new Line([(1, 1), (8, -2), (7, 5), (6, 2), (5, 3)], null);

    private static readonly Line TestLine2 =
        new Line([(1, 1), (5, 1), (7, 5)], null);

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
        Assert.That(offset4.Coords, Has.Exactly(0).Items);

        Visualizer.RenderAndShow(800, 500, TestPolygon1,
            offset1, offset2, offset3);
    }

    #region Line smoothing

    // Chaiking's algorithm, Fixed iteration-count. Open and closed paths.

    [Test]
    public void TestSmooth_ChaikinFixed_Line()
    {
        Line chaikin1 = TestLine1.Smooth_Chaikin(1).Transform(1, 10, 0);
        Line chaikin2 = TestLine1.Smooth_Chaikin(2).Transform(1, 20, 0);
        Line chaikin3 = TestLine1.Smooth_Chaikin(3).Transform(1, 30, 0);

        Assert.That(chaikin1.Count, Is.GreaterThan(TestLine1.Count));
        Assert.That(chaikin2.Count, Is.GreaterThan(chaikin1.Count));
        Assert.That(chaikin3.Count, Is.GreaterThan(chaikin2.Count));
        
        Visualizer.RenderAndShow(1600, 400,
            TestLine1, chaikin1, chaikin2, chaikin3,
            // highlight vertices
            new MultiPoint(TestLine1.Coords, null),
            new MultiPoint(chaikin1.Coords, null),
            new MultiPoint(chaikin2.Coords, null),
            new MultiPoint(chaikin3.Coords, null));
    }

    [Test]
    public void TestSmooth_ChaikinFixed_Polygon()
    {
        Polygon chaikin1 = TestPolygon1.Smooth_Chaikin(1).Transform(1, 10, 0);
        Polygon chaikin2 = TestPolygon1.Smooth_Chaikin(2).Transform(1, 20, 0);
        Polygon chaikin3 = TestPolygon1.Smooth_Chaikin(3).Transform(1, 30, 0);
        Assert.That(chaikin1.Count, Is.GreaterThan(TestPolygon1.Count));
        Assert.That(chaikin2.Count, Is.GreaterThan(chaikin1.Count));
        Assert.That(chaikin3.Count, Is.GreaterThan(chaikin2.Count));

        Visualizer.RenderAndShow(1600, 400,
            TestPolygon1, chaikin1, chaikin2, chaikin3,
            // highlight vertices
            new MultiPoint(TestLine1.Coords, null),
            new MultiPoint(chaikin1.Coords, null),
            new MultiPoint(chaikin2.Coords, null),
            new MultiPoint(chaikin3.Coords, null));
    }


    // Chaikin's algorithm, angle-based adaptive. Open and closed paths.

    [Test]
    public void TestSmooth_ChaikinAdaptive_Line()
    {
        Line chaikin1 = TestLine1.Smooth_ChaikinAdaptive(40).Transform(1, 10, 0);
        Line chaikin2 = TestLine1.Smooth_ChaikinAdaptive(10).Transform(1, 20, 0);
        Line chaikin3 = TestLine1.Smooth_ChaikinAdaptive(2).Transform(1, 30, 0);
        Visualizer.RenderAndShow(1600, 400,
            TestLine1, chaikin1, chaikin2, chaikin3,
            // highlight vertices
            new MultiPoint(TestLine1.Coords, null),
            new MultiPoint(chaikin1.Coords, null),
            new MultiPoint(chaikin2.Coords, null),
            new MultiPoint(chaikin3.Coords, null)
            );
    }

    [Test]
    public void TestSmooth_ChaikinAdaptive_Polygon()
    {
        Line chaikin1 = TestPolygon1.Smooth_ChaikinAdaptive(40).Transform(1, 10, 0);
        Line chaikin2 = TestPolygon1.Smooth_ChaikinAdaptive(10).Transform(1, 20, 0);
        Line chaikin3 = TestPolygon1.Smooth_ChaikinAdaptive(2).Transform(1, 30, 0);
        Visualizer.RenderAndShow(1600, 400,
            TestPolygon1, chaikin1, chaikin2, chaikin3,
            // highlight vertices
            new MultiPoint(TestLine1.Coords, null),
            new MultiPoint(chaikin1.Coords, null),
            new MultiPoint(chaikin2.Coords, null),
            new MultiPoint(chaikin3.Coords, null));
    }


    // Examples with some real data

    [Test]
    public void TestSmooth_FixedChaikin_RealData()
    {
        Visualizer.LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"),
            1200, 400, Color.AntiqueWhite, (canvas, multiPolygons) =>
            {
                CanvasLayer layer = canvas.AddNewLayer("water");
                foreach (MultiPolygon multipolygon in multiPolygons)
                    foreach (Coord[] polygon in multipolygon)
                    {
                        layer.DrawPolygon(polygon, 1.2, Color.Navy, LineJoin.Round);

                        // translate and smooth 1 iteration
                        Coord[] chaikin1 = Chaikin.Smooth_Fixed(
                            polygon.Transform(1, 400, 0), true, 1);
                        layer.DrawPolygon(chaikin1, 1.2, Color.DarkRed, LineJoin.Round);

                        // translate and smooth 2 iterations
                        Coord[] chaikin2 = Chaikin.Smooth_Fixed(
                            polygon.Transform(1, 800, 0), true, 2);
                        layer.DrawPolygon(chaikin2, 1.2, Color.DarkGreen, LineJoin.Round);
                    }
            });
    }

    #endregion
}
