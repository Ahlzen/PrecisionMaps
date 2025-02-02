using System.Drawing;
using System.IO;
using MapLib;
using MapLib.FileFormats.Vector;
using MapLib.Output;

namespace MapLibTests.Output;

[TestFixture]
public class CanvasFixture : BaseFixture
{
    #region Artificial tests

    /// <summary>
    /// Test that renders a canvas with many different kinds of drawing
    /// primitives, to visually check that they work.
    /// </summary>
    [Test]
    public void DrawTestCanvas()
    {
        int width = 1600;
        int height = 800;
        var bitmapCanvas = new BitmapCanvas(width, height, Color.Transparent);
        var svgCanvas = new SvgCanvas(width, height, Color.Transparent);

        OgrDataReader reader = new();
        VectorData reservoirData = reader.ReadFile(Path.Join(TestDataPath, "Aaron River Reservoir.geojson"));
        Assert.That(reservoirData.Polygons.Count, Is.EqualTo(0));
        Assert.That(reservoirData.MultiPolygons.Count, Is.EqualTo(1));
        VectorData scaledReservoirData = TransformToFit(reservoirData, 180, 180);
        MultiPolygon reservoirPolygon = scaledReservoirData.MultiPolygons[0];

        Assert.That(reservoirPolygon.Count(cs => cs.IsCounterClockwise()), Is.EqualTo(1)); // outer ring (perimeter)
        Assert.That(reservoirPolygon.Count(cs => cs.IsClockwise()), Is.GreaterThan(1)); // inner rings (islands)

        foreach (Canvas canvas in new Canvas[] { bitmapCanvas, svgCanvas })
        {
            CanvasLayer layer = canvas.AddNewLayer("main");

            // Try polygon styles

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

        }

        // Write bitmap
        Bitmap bitmap = bitmapCanvas.GetBitmap();
        string pngFilename = GetTempFileName(".png");
        bitmap.Save(pngFilename);
        ShowFile(pngFilename);

        // Write SVG
        string svg = svgCanvas.GetSvg();
        string svgFilename = GetTempFileName(".svg");
        File.WriteAllText(svgFilename, svg);
    }

    #endregion


    #region Tests that render real data

    [Test]
    public void TestRenderMultipolygon()
    {
        LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"),
            600, 600, Color.AntiqueWhite, (canvas, multiPolygons) => {
                CanvasLayer layer = canvas.AddNewLayer("water");
                foreach (MultiPolygon multipolygon in multiPolygons)
                    //layer.DrawLines(multipolygon, 1.2, Color.Navy, LineCap.Round, LineJoin.Round);
                    foreach (Coord[] polygon in multipolygon)
                        layer.DrawPolygon(polygon, 1.2, Color.Navy, LineJoin.Round);
            });
    }

    [Test]
    public void TestRenderFilledMultipolygon()
    {
        LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"),
            600, 600, Color.AntiqueWhite, (canvas, multiPolygons) => {
                CanvasLayer layer = canvas.AddNewLayer("water");
                foreach (var multipolygon in multiPolygons)
                    //layer.DrawFilledPolygons(multipolygon, Color.CornflowerBlue);
                    layer.DrawFilledMultiPolygon(multipolygon, Color.CornflowerBlue);
            });
    }

    [Test]
    public void TestRenderShorelinePolygons_AaronRiver()
    {
        LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"),
            600, 600, Color.AntiqueWhite, (canvas, multiPolygons) => {
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
        LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Natural Earth/ne_110m_land.shp"),
            1200, 600, Color.AntiqueWhite, (canvas, multiPolygons) => {
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

    private void LoadOgrDataAndDrawPolygons(string inputFilename, int canvasWidth, int canvasHeight,
        Color background, Action<Canvas, IEnumerable<MultiPolygon>> drawingFunc)
    {
        // Read data
        OgrDataReader reader = new OgrDataReader();
        VectorData data = reader.ReadFile(inputFilename);
        Console.WriteLine(FormatVectorDataSummary(data));
        VectorData transformedData = TransformToFit(data, canvasWidth, canvasHeight);
        BitmapCanvas bitmapCanvas = new BitmapCanvas(canvasWidth, canvasHeight, background);
        SvgCanvas svgCanvas = new SvgCanvas(canvasWidth, canvasHeight, background);

        // Use multipolygons for everything
        List<MultiPolygon> multiPolygons = new();
        multiPolygons.AddRange(transformedData.MultiPolygons);
        multiPolygons.AddRange(transformedData.Polygons.Select(p => p.AsMultiPolygon()));

        drawingFunc(bitmapCanvas, multiPolygons);
        drawingFunc(svgCanvas, multiPolygons);

        // Write bitmap
        Bitmap bitmap = bitmapCanvas.GetBitmap();
        string pngFilename = GetTempFileName(".png");
        bitmap.Save(pngFilename);
        ShowFile(pngFilename);

        // Write SVG
        string svg = svgCanvas.GetSvg();
        string svgFilename = GetTempFileName(".svg");
        File.WriteAllText(svgFilename, svg);
    }

    // TODO: If this is useful elsewhere, it should be moved
    private VectorData TransformToFit(VectorData source, double width, double height)
    {
        Bounds sourceBounds = source.Bounds;
        double scale = Math.Min(
            width / sourceBounds.Width,
            height / sourceBounds.Height);
        double offsetX = -(sourceBounds.XMin * scale);
        double offsetY = -(sourceBounds.YMin * scale);
        return source.Transform(scale, offsetX, offsetY);
    }
}
