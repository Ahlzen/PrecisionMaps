using MapLib.DataSources.Vector;
using MapLib.GdalSupport;
using MapLib.Output;
using MapLib.Render;
using MapLib.Util;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace MapLib.Tests.Render;

/// <summary>
/// Renders the same map data onto differently sized canvases,
/// using different strategies to scale/fit and with
/// different projections.
/// </summary>
[TestFixture]
public class ProjectionAndCanvasFittingFixture : BaseFixture
{
    private static readonly string[] Projections = [
        Epsg.Wgs84,
        Epsg.WebMercator
    ];

    private static readonly SizeF[] CanvasSizes = [
        new(10,10),
        new(8,14),
        new(14,8)
    ];

    private static readonly AspectRatioMismatchStrategy[] Strategies = [
        AspectRatioMismatchStrategy.StretchToFillCanvas,
        AspectRatioMismatchStrategy.CenterOnCanvas,
        AspectRatioMismatchStrategy.CropBounds,
        AspectRatioMismatchStrategy.ExtendBounds
    ];

    [Test]
    public void TestRenderMap(
        [ValueSource("Projections")] string projection,
        [ValueSource("CanvasSizes")] SizeF canvasSize,
        [ValueSource("Strategies")] AspectRatioMismatchStrategy strategy)
    {
        var sw1 = new QuickStopwatch("Creating Map");
        Map map = new(
            // Roughly square region in 3857
            new Bounds(-70.931500, -70.925782, 42.204346, 42.208510),
            projection);
        sw1.Dispose();

        var sw2 = new QuickStopwatch("Adding data sources");
        map.VectorDataSources.Add("osmdata",
            new VectorFileDataSource(Path.Join(TestDataPath, "osm-xml/Weymouth Detail.osm")));
        sw2.Dispose();

        var sw3 = new QuickStopwatch("Adding layers");
        map.MapLayers.Add(
            new VectorMapLayer("Water", "osmdata",
                style: new VectorStyle {
                    FillColor = Color.CornflowerBlue,
                    LineColor = Color.Blue,
                    LineWidth = 0.005 },
                filter: new TagFilter("natural", "water")));
        map.MapLayers.Add(
            new VectorMapLayer("Highway", "osmdata",
                style: new VectorStyle { 
                    LineColor = Color.Black,
                    LineWidth = 0.02 },
                filter: new TagFilter("highway")));
        map.MapLayers.Add(
            new VectorMapLayer("Building", "osmdata",
                style: new VectorStyle { FillColor = Color.Tan },
                filter: new TagFilter("building")));
        sw3.Dispose();

        string projectionSummary = projection.Replace(":", "");
        string prefix = $"{projectionSummary}_{canvasSize.Width}x{canvasSize.Height}_{strategy}_";

        var sw4 = new QuickStopwatch("Creating canvas");
        CanvasStack canvas = new BitmapCanvasStack(
            CanvasUnit.In, canvasSize.Width, canvasSize.Height,
            Color.White, 2.0);
        sw4.Dispose();

        string filename = FileSystemHelpers.GetTempOutputFileName(canvas.DefaultFileExtension, prefix);

        var sw5 = new QuickStopwatch("Render");
        map.Render(canvas, strategy);
        sw5.Dispose();

        var sw6 = new QuickStopwatch("Save to file");
        canvas.SaveToFile(filename);
        sw6.Dispose();

        Console.WriteLine(filename);
        canvas.Dispose();
    }
}

