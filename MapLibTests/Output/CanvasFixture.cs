using System.Drawing;
using System.IO;
using MapLib;
using MapLib.FileFormats.Vector;
using MapLib.Output;

namespace MapLibTests.Output;

[TestFixture]
public class CanvasFixture : BaseFixture
{
    [Test]
    public void TestRenderMultipolygon()
    {
        LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"),
            600, 600, Color.AntiqueWhite, (canvas, multiPolygons) => {
                CanvasLayer layer = canvas.AddNewLayer("water");
                foreach (var multipolygon in multiPolygons)
                    layer.DrawLines(multipolygon, 1.2, Color.Navy, LineCap.Round, LineJoin.Round);
            });
    }

    [Test]
    public void TestRenderShorelinePolygons()
    {
        LoadOgrDataAndDrawPolygons(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"),
            600, 600, Color.AntiqueWhite, (canvas, multiPolygons) =>
            {
                CanvasLayer layer = canvas.AddNewLayer("water");
                foreach (var multipolygon in multiPolygons)
                {
                    MultiPolygon currentPolygon = multipolygon;
                    layer.DrawLines(currentPolygon.Coords, 1.6, Color.Navy,
                            LineCap.Round, LineJoin.Round);

                    double opacity = 1;
                    double lineWidth = 1.2;
                    for (int i = 0; i < 15; i++)
                    {
                        Color color = Color.FromArgb((int)(255 * opacity), Color.Navy);
                        layer.DrawLines(currentPolygon.Coords, lineWidth, color,
                            LineCap.Round, LineJoin.Round);

                        // If the polygon is small enough, offsetting it inward may result in
                        // nothing. In that case we're done.
                        if (currentPolygon.Count == 0)
                            break;

                        opacity *= 0.7;
                        lineWidth *= 0.9;
                        currentPolygon = currentPolygon.Offset(-7);
                    }
                }
            });
    }


    ///// Helpers

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
