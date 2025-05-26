using System.Diagnostics;
using System.Drawing;
using System.IO;
using MapLib.FileFormats.Vector;
using MapLib.Output;

namespace MapLib.Tests.Output;

[TestFixture]
public class CanvasFixture : BaseFixture
{
    #region Artificial tests

    public static IEnumerable<Canvas> TestCanvasFactory()
    {
        int width = 1600;
        int height = 800;
        yield return new BitmapCanvas(CanvasUnit.Pixel, width, height, Color.Transparent, 1.0);
        yield return new BitmapCanvas(CanvasUnit.Pixel, width, height, Color.Transparent, 5.0);
        yield return new SvgCanvas(CanvasUnit.Pixel, width, height, Color.Transparent);
    }

    /// <summary>
    /// Test that renders a canvas with many different kinds of drawing
    /// primitives, to visually check that they work.
    /// </summary>
    [Test]
    [TestCaseSource("TestCanvasFactory")]
    public void DrawTestCanvas(Canvas canvas)
    {
        OgrDataReader reader = new();

        string path = Path.Join(TestDataPath, "Aaron River/Aaron River Reservoir.geojson");
        path = Path.GetFullPath(path);

        VectorData reservoirData = reader.ReadFile(path);
        Assert.That(reservoirData.Polygons.Count, Is.EqualTo(0));
        Assert.That(reservoirData.MultiPolygons.Count, Is.EqualTo(1));
        VectorData scaledReservoirData = Visualizer.TransformToFit(reservoirData, 180, 180);
        MultiPolygon reservoirPolygon = scaledReservoirData.MultiPolygons[0];

        Assert.That(reservoirPolygon.Count(cs => cs.IsCounterClockwise()), Is.EqualTo(1)); // outer ring (perimeter)
        Assert.That(reservoirPolygon.Count(cs => cs.IsClockwise()), Is.GreaterThan(1)); // inner rings (islands)

        CanvasLayer layer = canvas.AddNewLayer("main");

        ///// Polygon styles

        // Solid light blue fill
        layer.DrawFilledPolygons(reservoirPolygon.Transform(1.0, 10, 10).Coords, Color.CadetBlue);

        // Transparent dark green fill
        layer.DrawFilledPolygons(reservoirPolygon.Transform(1.0, 210, 10).Coords, Color.FromArgb(128, Color.DarkGreen));

        // Transparent dark green fill
        layer.DrawFilledMultiPolygon(reservoirPolygon.Transform(1.0, 410, 10).Coords, Color.FromArgb(128, Color.DarkGreen));

        // Line thicknesses
        layer.DrawLines(reservoirPolygon.Transform(1.0, 610, 10).Coords, 2, Color.Navy);
        layer.DrawLines(reservoirPolygon.Transform(1.0, 610, 10).Offset(-8) .Coords, 1, Color.Navy);
        layer.DrawLines(reservoirPolygon.Transform(1.0, 610, 10).Offset(-16).Coords, 0.5, Color.Navy);
        layer.DrawLines(reservoirPolygon.Transform(1.0, 610, 10).Offset(-24).Coords, 0.25, Color.Navy);

        // Line dasharrays and joins/caps
        layer.DrawLines(reservoirPolygon.Transform(1.0, 810, 10).Coords, 3, Color.DarkOliveGreen,
            LineCap.Round, LineJoin.Round);
        layer.DrawLines(reservoirPolygon.Transform(1.0, 810, 10).Offset(-6).Coords, 3, Color.DarkOliveGreen,
            LineCap.Square, LineJoin.Miter);
        layer.DrawLines(reservoirPolygon.Transform(1.0, 810, 10).Offset(-12).Coords, 1, Color.DarkOliveGreen,
            LineCap.Butt, LineJoin.Miter, [6, 2, 2, 2]);
        layer.DrawLines(reservoirPolygon.Transform(1.0, 810, 10).Offset(-18).Coords, 1, Color.DarkOliveGreen,
            LineCap.Butt, LineJoin.Miter, [10, 10]);

        ///// Filled circles

        layer.DrawFilledCircles(reservoirPolygon
            .Transform(1.0, 10, 210)
            .Coords
            .SelectMany(cs => cs),
            2, Color.Red);

        ///// Text



        // HAlign
        //layer.DrawText("Label Left", (300, 380), Color.DarkOliveGreen, "Calibri", 8, TextHAlign.Left, TextVAlign.Center);
        //layer.DrawText("Label Center", (300, 360), Color.DarkOliveGreen, "Calibri", 8, TextHAlign.Center, TextVAlign.Center);
        //layer.DrawText("Label Right", (300, 340), Color.DarkOliveGreen, "Calibri", 8, TextHAlign.Right, TextVAlign.Center);
        //layer.DrawFilledCircles([(300, 380), (300, 360), (300, 340)], 2, Color.Magenta);
        // VAlign
        //layer.DrawText("Label Bottom", (300, 300), Color.DarkOliveGreen, "Calibri", 8, TextHAlign.Center, TextVAlign.Bottom);
        //layer.DrawText("Label Baseline", (300, 280), Color.DarkOliveGreen, "Calibri", 8, TextHAlign.Center, TextVAlign.Baseline);
        //layer.DrawText("Label Center", (300, 260), Color.DarkOliveGreen, "Calibri", 8, TextHAlign.Center, TextVAlign.Center);
        //layer.DrawText("Label Top", (300, 240), Color.DarkOliveGreen, "Calibri", 8, TextHAlign.Center, TextVAlign.Top);
        //layer.DrawFilledCircles([(300, 300), (300, 280), (300, 260), (300, 240)], 2, Color.Magenta);

        DrawTextWithBbox(layer, "Calibri", 8, "Test Text", (300, 300), Color.Cyan);


        ///// Bitmap

        string bitmapPath = Path.Join(TestDataPath, "Misc", "me.jpg");
        Bitmap bitmap = (Bitmap) Bitmap.FromFile(bitmapPath);
        
        // Fully opaque bitmap
        layer.DrawBitmap(bitmap, 410, 210, 180, 180, 1.0);

        // Semi-transparent bitmap
        layer.DrawBitmap(bitmap, 610, 210, 180, 180, 0.3);

        Visualizer.SaveCanvas(canvas, false);
        canvas.Dispose();
    }

    private void DrawTextWithBbox(CanvasLayer layer, string fontName, double emSize,
        string s, Coord centerCoord, Color color)
    {
        Coord textSize = layer.GetTextSize(fontName, emSize, s);

        // Outline
        var bottomLeftCoord = centerCoord - textSize * 0.5;
        var topRightCoord = centerCoord + textSize * 0.5;
        Bounds bounds = new Bounds(bottomLeftCoord, topRightCoord);
        layer.DrawPolygon(bounds.AsLine().Coords, emSize * 0.05, Color.Red);

        // Point
        layer.DrawFilledCircle(centerCoord, emSize * 0.3, Color.Red);

        // Text
        layer.DrawText(fontName, emSize, s, centerCoord, color);
    }

    #endregion


    #region Tests that render real data

    [Test]
    public void TestRenderSimpleMultipolygon()
    {
        var outerRing = new Coord[]{
            new(100, 400),
            new(100, 100),
            new(400, 100),
            new(400, 400),
            new(100, 400)
        };
        var innerRing = new Coord[]{
            new(200, 200),
            new(300, 200),
            new(300, 300),
            new(200, 300),
            new(200, 200)
        };
        MultiPolygon multipolygon = new([outerRing, innerRing], null);

        using BitmapCanvas bitmapCanvas = new(CanvasUnit.Pixel, 500, 500, Color.White);
        using SvgCanvas svgCanvas = new(CanvasUnit.Pixel, 500, 500, Color.White);

        foreach (Canvas canvas in new Canvas[] { bitmapCanvas, svgCanvas })
        {
            CanvasLayer layer = canvas.AddNewLayer("test layer");
            layer.DrawFilledMultiPolygon(multipolygon, Color.CadetBlue);
            layer.DrawPolygon(outerRing, 5.0, Color.DarkRed);
            layer.DrawPolygon(innerRing, 5.0, Color.DarkGreen);
            Visualizer.SaveCanvas(canvas, false);
        }
    }

    [Test]
    public void TestRenderMultipolygon()
    {
        Visualizer.LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Aaron River/Aaron River Reservoir.geojson"),
            600, 600, 600, 600, Color.AntiqueWhite, (canvas, multiPolygons) => {
                CanvasLayer layer = canvas.AddNewLayer("water");
                foreach (MultiPolygon multipolygon in multiPolygons)
                    foreach (Coord[] polygon in multipolygon)
                        layer.DrawPolygon(polygon, 1.2, Color.Navy, LineJoin.Round);
            });
    }

    [Test]
    public void TestRenderFilledMultipolygon()
    {
        Visualizer.LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Aaron River/Aaron River Reservoir.geojson"),
            600, 600, 600, 600, Color.AntiqueWhite, (canvas, multiPolygons) => {
                CanvasLayer layer = canvas.AddNewLayer("water");
                foreach (var multipolygon in multiPolygons)
                    layer.DrawFilledMultiPolygon(multipolygon, Color.CornflowerBlue);
            });
    }

    [Test]
    public void TestRenderShorelinePolygons_AaronRiver()
    {
        Visualizer.LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Aaron River/Aaron River Reservoir.geojson"),
            600, 600, 600, 600, Color.AntiqueWhite, (canvas, multiPolygons) => {
                CanvasLayer layer = canvas.AddNewLayer("water");
                foreach (var multipolygon in multiPolygons)
                {
                    DrawShorelineFromPolygon(layer, multipolygon,
                        Color.Navy, 1.6, 1.2, 0.9, 0.7, -7, 15);
                }
            });
    }

    [Test]
    public void TestRenderShorelinePolygons_World()
    {
        Visualizer.LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Natural Earth/ne_110m_land.shp"),
            600, 600, 1200, 600, Color.AntiqueWhite, (canvas, multiPolygons) => {
                MultiPolygon world = new(multiPolygons, null);
                CanvasLayer layer = canvas.AddNewLayer("shore");
                DrawShorelineFromPolygon(layer, world,
                        Color.Navy, 1.3, 1.0, 0.8, 0.6, 3.5, 5);
            });
    }

    #endregion


    ///// Helpers

    private void DrawShorelineFromPolygon(CanvasLayer layer,
        MultiPolygon polygon, Color baseColor,
        double firstLineWidth, double secondLineWidth,
        double lineWidthMultiplier, double opacityMultiplier,
        double waveDistance, int waveCount)
    {
        layer.DrawLines(polygon.Coords, firstLineWidth, baseColor,
                LineCap.Round, LineJoin.Round);

        double opacity = 1;
        double lineWidth = secondLineWidth;
        for (int i = 0; i < waveCount; i++)
        {
            Debug.WriteLine($"Drawing polygon with {polygon.Sum(p => p.Length)} points");

            Color color = Color.FromArgb((int)(255 * opacity), baseColor);
            layer.DrawLines(polygon.Coords, lineWidth, color,
                LineCap.Round, LineJoin.Round);

            // If the polygon is small enough, offsetting it inward may result in
            // nothing. In that case we're done.
            if (polygon.Count == 0)
                break;

            opacity *= opacityMultiplier;
            lineWidth *= lineWidthMultiplier;
            polygon = polygon.Offset(waveDistance);
        }
    }
}

