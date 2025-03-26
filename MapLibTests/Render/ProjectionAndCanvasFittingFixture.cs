using MapLib.DataSources.Vector;
using MapLib.GdalSupport;
using MapLib.Output;
using MapLib.Render;
using MapLib.Util;
using System.Drawing;
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
        Transformer.WktWgs84,
        Transformer.WktWebMercator
    ];

    private static readonly SizeF[] CanvasSizes = [
        new(10,10),
        new(8,14),
        new(14,8)
    ];

    private static readonly AspectRatioMismatchStrategy[] Strategies = [
        AspectRatioMismatchStrategy.Stretch,
        AspectRatioMismatchStrategy.Center,
        AspectRatioMismatchStrategy.Crop,
        AspectRatioMismatchStrategy.ExtendBounds
    ];

    [Test]
    public void TestRenderMap(
        [ValueSource("Projections")] string projection,
        [ValueSource("CanvasSizes")] SizeF canvasSize,
        [ValueSource("Strategies")] AspectRatioMismatchStrategy strategy)
    {
        Map map = new(
            // Roughly square region in 3857
            new Bounds(-70.931500, -70.925782, 42.204346, 42.208510),
            projection);

        map.DataSources.Add(
            new VectorMapDataSource("osmdata",
            new VectorFileDataSource(Path.Join(TestDataPath, "osm-xml/Weymouth Detail.osm"))));
        map.Layers.Add(
            new VectorMapLayer("Water", "osmdata",
                filter: new TagFilter("natural", "water"),
                fillColor: Color.CornflowerBlue,
                strokeColor: Color.Blue,
                strokeWidth: 0.005));
        map.Layers.Add(
            new VectorMapLayer("Highway", "osmdata",
                filter: new TagFilter("highway"),
                strokeColor: Color.Black,
                strokeWidth: 0.02));
        map.Layers.Add(
            new VectorMapLayer("Building", "osmdata",
                filter: new TagFilter("building"),
                fillColor: Color.Tan));

        string projectionSummary = projection.Replace(":", "");
        string prefix = $"{projectionSummary}_{canvasSize.Width}x{canvasSize.Height}_{strategy}_";

        Canvas canvas = new BitmapCanvas(
            CanvasUnit.In, canvasSize.Width, canvasSize.Height,
            Color.White, 2.0);
        string filename = FileSystemHelpers.GetTempFileName(canvas.DefaultFileExtension, prefix);
        map.Render(canvas, strategy);
        canvas.SaveToFile(filename);
        Console.WriteLine(filename);
        canvas.Dispose();
    }
}

