using MapLib.Geometry.Helpers;
using MapLib.Output;
using System.Drawing;
using System.IO;

namespace MapLib.Tests.Geometry;

[TestFixture]
public class DouglasPeuckerFixture : BaseFixture
{
    [Test]
    public void TestSimplify_DouglasPeucker_ByTolerance()
    {
        Visualizer.LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"),
            400, 400, 1600, 400, Color.AntiqueWhite, (canvas, multiPolygons) =>
            {
                CanvasLayer layer = canvas.AddNewLayer("water");
                foreach (MultiPolygon multipolygon in multiPolygons)
                {
                    double size = multipolygon.GetBounds().Size; // overall size of feature

                    foreach (Coord[] polygon in multipolygon)
                    {
                        layer.DrawPolygon(polygon, 1.2, Color.Navy, LineJoin.Round);

                        Coord[] douglas1 = DouglasPeucker.Simplify(
                            polygon.Transform(1, 400, 0), tolerance: size / 5000);
                        layer.DrawPolygon(douglas1, 1.2, Color.DarkRed, LineJoin.Round);

                        Coord[] douglas2 = DouglasPeucker.Simplify(
                            polygon.Transform(1, 800, 0), tolerance: size / 500);
                        layer.DrawPolygon(douglas2, 1.2, Color.DarkGreen, LineJoin.Round);

                        Coord[] douglas3 = DouglasPeucker.Simplify(
                            polygon.Transform(1, 1200, 0), tolerance: size / 50);
                        layer.DrawPolygon(douglas3, 1.2, Color.DarkBlue, LineJoin.Round);
                    }
                }
            });
    }

    [Test]
    public void TestSimplify_DouglasPeucker_ByPointCount()
    {
        Visualizer.LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"),
            400, 400, 1600, 400, Color.AntiqueWhite, (canvas, multiPolygons) =>
            {
                CanvasLayer layer = canvas.AddNewLayer("water");
                foreach (MultiPolygon multipolygon in multiPolygons)
                {
                    foreach (Coord[] polygon in multipolygon)
                    {
                        layer.DrawPolygon(polygon, 1.2, Color.Navy, LineJoin.Round);
                        int pointCount = polygon.Length;

                        Coord[] douglas1 = DouglasPeucker.Simplify(
                            polygon.Transform(1, 400, 0), maxPointCount: pointCount / 2);
                        layer.DrawPolygon(douglas1, 1.2, Color.DarkRed, LineJoin.Round);

                        Coord[] douglas2 = DouglasPeucker.Simplify(
                            polygon.Transform(1, 800, 0), maxPointCount: pointCount / 4);
                        layer.DrawPolygon(douglas2, 1.2, Color.DarkGreen, LineJoin.Round);

                        Coord[] douglas3 = DouglasPeucker.Simplify(
                            polygon.Transform(1, 1200, 0), maxPointCount: pointCount / 8);
                        layer.DrawPolygon(douglas3, 1.2, Color.DarkBlue, LineJoin.Round);
                    }
                }
            });
    }
}
