using MapLib.Geometry.Helpers;
using MapLib.Output;
using System.Drawing;
using System.IO;

namespace MapLib.Tests.Geometry;

[TestFixture]
internal class ChaikinFixture : BaseFixture
{
    // Chaikin's algorithm, Fixed iteration-count. Open and closed paths.

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
    public void TestSmooth_ChaikinFixed_RealData()
    {
        Visualizer.LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "GeoJSON/Aaron River Reservoir.geojson"),
            400, 400, 1200, 400, Color.AntiqueWhite, (canvas, multiPolygons) =>
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
}
