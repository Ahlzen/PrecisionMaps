using System.Drawing;
using System.IO;
using MapLib.DataSources.Raster;
using MapLib.DataSources.Vector;
using MapLib.GdalSupport;
using MapLib.Output;
using MapLib.Render;
using MapLib.Util;

namespace MapLib.Tests.Render;

[TestFixture]
public class MapRenderFixture : BaseFixture
{
    public static IEnumerable<Canvas> LetterSizeCanvases()
    {
        yield return new BitmapCanvas(CanvasUnit.In, 11.0, 8.5, Color.White, 1.0);
        yield return new BitmapCanvas(CanvasUnit.In, 11.0, 8.5, Color.White, 4.0);
        yield return new SvgCanvas(CanvasUnit.In, 11.0, 8.5, Color.White);
    }

    [Test]
    [TestCaseSource("LetterSizeCanvases")]
    public void RenderSimpleOsmData(Canvas canvas)
    {
        Map map = new Map(
            new Bounds(-70.944, -70.915, 42.187, 42.207),
            Transformer.WktWebMercator);

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

        map.Render(canvas);
        string filename = FileSystemHelpers.GetTempFileName(
            canvas.DefaultFileExtension, "RenderSimpleOsmData_");
        canvas.SaveToFile(filename);
        Console.WriteLine(filename);
        canvas.Dispose();
    }

    [Test]
    public void RenderSimpleOsmData2()
    {
        Map map = new Map(
            new Bounds(-70.944, -70.915, 42.187, 42.207),
            Transformer.WktWebMercator);

        map.DataSources.Add(
            new VectorMapDataSource("osmdata",
            new VectorFileDataSource(Path.Join(TestDataPath, "map.osm"))));
        map.DataSources.Add(
            new RasterMapDataSource("hillshading",
            //new GdalDataSource(Path.Join(TestDataPath, "MA Shaded Relief 5k 3857 enhanced.tif"))));
            new GdalDataSource(Path.Join(TestDataPath, "MassGIS LiDAR/be_19TCG340674.tif"))));

        map.Layers.Add(
            new RasterMapLayer("Hillshading", "hillshading"));

        map.Layers.Add(
            new VectorMapLayer("Water", "osmdata",
                filter: new TagFilter("natural", "water"),
                fillColor: Color.CornflowerBlue,
                strokeColor: Color.Blue,
                strokeWidth: 0.005));
        
        map.Layers.Add(
            new VectorMapLayer("Highway-1", "osmdata",
                filter: new TagFilter(("highway", "motorway"), ("highway", "trunk"), ("highway", "primary")),
                strokeColor: Color.Black,
                strokeWidth: 0.04));
        map.Layers.Add(
            new VectorMapLayer("Highway-2", "osmdata",
                filter: new TagFilter(("highway", "secondary"), ("highway", "tertiary")),
                strokeColor: Color.Black,
                strokeWidth: 0.03));
        map.Layers.Add(
            new VectorMapLayer("Highway-3", "osmdata",
                filter: new TagFilter(("highway", "residential"), ("highway", "unclassified")),
                strokeColor: Color.Black,
                strokeWidth: 0.02));

        map.Layers.Add(
            new VectorMapLayer("Highway-1-case", "osmdata",
                filter: new TagFilter(("highway", "motorway"), ("highway", "trunk"), ("highway", "primary")),
                strokeColor: ColorUtil.FromHex("#fe9"),
                strokeWidth: 0.02));
        map.Layers.Add(
            new VectorMapLayer("Highway-2-case", "osmdata",
                filter: new TagFilter(("highway", "secondary"), ("highway", "tertiary")),
                strokeColor: ColorUtil.FromHex("#fb9"),
                strokeWidth: 0.015));
        map.Layers.Add(
            new VectorMapLayer("Highway-3-case", "osmdata",
                filter: new TagFilter(("highway", "residential"), ("highway", "unclassified")),
                strokeColor: ColorUtil.FromHex("#fee"),
                strokeWidth: 0.01));

        map.Layers.Add(
            new VectorMapLayer("Building", "osmdata",
                filter: new TagFilter("building"),
                fillColor: Color.Tan));

        Canvas canvas = new SvgCanvas(CanvasUnit.In, 11.0, 8.5, System.Drawing.Color.White);
        map.Render(canvas);
        string filename = FileSystemHelpers.GetTempFileName(".svg", "RenderSimpleOsmData2_");
        canvas.SaveToFile(filename);
        Console.WriteLine(filename);
        canvas.Dispose();
    }
}
