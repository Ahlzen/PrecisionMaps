using System.Drawing;
using System.IO;
using MapLib.DataSources.Vector;
using MapLib.GdalSupport;
using MapLib.Output;
using MapLib.Render;
using MapLib.Util;

namespace MapLib.Tests.Render;

[TestFixture]
public class MapRenderFixture : BaseFixture
{
    [Test]
    public void RenderSimpleOsmData()
    {
        Map map = new Map(
            new Bounds(-70.944, -70.915, 42.187, 42.207),
            Transformer.WktWebMercator);

        map.DataSources.Add(
            new VectorMapDataSource("osmdata",
            new VectorFileDataSource(Path.Join(TestDataPath, "map.osm"))));
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

        Canvas canvas = new SvgCanvas(CanvasUnit.In, 11.0, 8.5, System.Drawing.Color.White);
        map.Render(canvas);
        string filename = FileSystemHelpers.GetTempFileName(".svg");
        canvas.SaveToFile(filename);
        Console.WriteLine(filename);
    }
}
