using MapLib.Geometry.Helpers;
using MapLib.Output;
using System.Drawing;
using System.IO;

namespace MapLib.Tests.Geometry;

[TestFixture]
public class VisvalingamWhyattFixture : BaseFixture
{
    [Test]
    public void TestSimplify_VisvalingamWhyatt_ByPointCount()
    {
        Visualizer.LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "GeoJSON/Aaron River Reservoir.geojson"),
            400, 400, 1600, 800, Color.AntiqueWhite, (canvas, multiPolygons) =>
            {
                CanvasLayer layer = canvas.AddNewLayer("water");
                foreach (MultiPolygon multipolygon in multiPolygons)
                {
                    foreach (Coord[] polygon in multipolygon)
                    {
                        layer.DrawPolygon(polygon, 1.2, Color.Navy, LineJoin.Round);
                        int pointCount = polygon.Length;
                        Assert.That(pointCount, Is.GreaterThan(0));

                        Coord[] visvalN1 = VisvalingamWhyatt_Naive.Simplify(
                            polygon.Transform(1, 400, 0), maxPointCount: pointCount / 2);
                        Coord[] visval1 = VisvalingamWhyatt.Simplify(
                            polygon.Transform(1, 400, 400), maxPointCount: pointCount / 2);
                        layer.DrawPolygon(visvalN1, 1.2, Color.DarkRed, LineJoin.Round);
                        layer.DrawPolygon(visval1, 1.2, Color.DarkRed, LineJoin.Round);

                        Coord[] visvalN2 = VisvalingamWhyatt_Naive.Simplify(
                            polygon.Transform(1, 800, 0), maxPointCount: pointCount / 4);
                        Coord[] visval2 = VisvalingamWhyatt.Simplify(
                            polygon.Transform(1, 800, 400), maxPointCount: pointCount / 4);
                        layer.DrawPolygon(visvalN2, 1.2, Color.DarkGreen, LineJoin.Round);
                        layer.DrawPolygon(visval2, 1.2, Color.DarkGreen, LineJoin.Round);

                        Coord[] visvalN3 = VisvalingamWhyatt_Naive.Simplify(
                            polygon.Transform(1, 1200, 0), maxPointCount: pointCount / 8);
                        Coord[] visval3 = VisvalingamWhyatt.Simplify(
                            polygon.Transform(1, 1200, 400), maxPointCount: pointCount / 8);
                        layer.DrawPolygon(visvalN3, 1.2, Color.DarkBlue, LineJoin.Round);
                        layer.DrawPolygon(visval3, 1.2, Color.DarkBlue, LineJoin.Round);
                    }
                }
            });
    }
}
