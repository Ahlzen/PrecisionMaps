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
            new VectorMapLayer("Osm Data", "osmdata"));
        
        Canvas canvas = new SvgCanvas(CanvasUnit.In, 11.0, 8.5, System.Drawing.Color.White);
        map.Render(canvas);
        string filename = FileSystemHelpers.GetTempFileName(".svg");
        canvas.SaveToFile(filename);
        Console.WriteLine(filename);
    }
}
