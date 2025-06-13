using System.Drawing;
using System.IO;
using MapLib.ColorSpace;
using MapLib.DataSources.Raster;
using MapLib.DataSources.Vector;
using MapLib.GdalSupport;
using MapLib.Output;
using MapLib.RasterOps;
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
    [TestCaseSource(nameof(LetterSizeCanvases))]
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
                style: new VectorStyle {
                    FillColor = Color.CornflowerBlue,
                    LineColor = Color.Blue,
                    LineWidth = 0.005 },
                filter: new TagFilter("natural", "water")));
        map.Layers.Add(
            new VectorMapLayer("Highway", "osmdata",
                style: new VectorStyle {
                    LineColor = Color.Black,
                    LineWidth = 0.02 },
                filter: new TagFilter("highway")));
        map.Layers.Add(
            new VectorMapLayer("Building", "osmdata",
                style: new VectorStyle {
                    FillColor = Color.Tan },
                filter: new TagFilter("building")));

        map.Render(canvas);
        string filename = FileSystemHelpers.GetTempOutputFileName(
            canvas.DefaultFileExtension, "RenderSimpleOsmData_");
        canvas.SaveToFile(filename);
        Console.WriteLine(filename);
        canvas.Dispose();
    }

    [Test]
    [TestCaseSource("LetterSizeCanvases")]
    public void RenderSimpleOsmDataAndRaster(Canvas canvas)
    {
        Map map = new Map(
            new Bounds(-70.944, -70.915, 42.187, 42.207),
            Transformer.WktWebMercator);

        map.DataSources.Add(
            new VectorMapDataSource("osmdata",
            new VectorFileDataSource(Path.Join(TestDataPath, "osm-xml/Weymouth Detail.osm"))));
        //map.DataSources.Add(
        //    new RasterMapDataSource("hillshading",
        //    new GdalDataSource(Path.Join(TestDataPath, "MassGIS LiDAR/be_19TCG340674.tif"))));
        map.DataSources.Add(
            new RasterMapDataSource2("ortophoto",
            new GdalDataSource2(Path.Join(TestDataPath, "MassGIS Aerial/19TCG390725.jp2"))));

        //map.Layers.Add(
        //    new RasterMapLayer("Hillshading", "hillshading"));
        map.Layers.Add(
            new RasterMapLayer("ortophoto", "ortophoto"));

        map.Layers.Add(
            new VectorMapLayer("Water", "osmdata",
                style: new VectorStyle {
                    FillColor = Color.CornflowerBlue,
                    LineColor = Color.Blue,
                    LineWidth = 0.005 },
                filter: new TagFilter("natural", "water")));
        
        VectorStyle roadCasing = new() { LineColor = Color.Black };
        map.Layers.Add(
            new VectorMapLayer("Highway-1", "osmdata",
                style: roadCasing with { LineWidth = 0.04 },
                filter: new TagFilter(("highway", "motorway"), ("highway", "trunk"), ("highway", "primary"))));
        map.Layers.Add(
            new VectorMapLayer("Highway-2", "osmdata",
                style: roadCasing with { LineWidth = 0.03 },
                filter: new TagFilter(("highway", "secondary"), ("highway", "tertiary"))));
        map.Layers.Add(
            new VectorMapLayer("Highway-3", "osmdata",
                style: roadCasing with { LineWidth = 0.02 },
                filter: new TagFilter(("highway", "residential"), ("highway", "unclassified"))));

        map.Layers.Add(
            new VectorMapLayer("Highway-1-fill", "osmdata",
                style: new VectorStyle {
                    LineColor = ColorUtil.FromHex("#fe9"),
                    LineWidth = 0.02 },
                filter: new TagFilter(("highway", "motorway"), ("highway", "trunk"), ("highway", "primary"))));
        map.Layers.Add(
            new VectorMapLayer("Highway-2-fill", "osmdata",
                style: new VectorStyle {
                    LineColor = ColorUtil.FromHex("#fb9"),
                    LineWidth = 0.015 },
                filter: new TagFilter(("highway", "secondary"), ("highway", "tertiary"))));
        map.Layers.Add(
            new VectorMapLayer("Highway-3-fill", "osmdata",
                style: new VectorStyle {
                    LineColor = ColorUtil.FromHex("#fee"),
                    LineWidth = 0.01 },
                filter: new TagFilter(("highway", "residential"), ("highway", "unclassified"))));

        map.Layers.Add(
            new VectorMapLayer("Building", "osmdata",
                style: new VectorStyle { FillColor = Color.Tan },
                filter: new TagFilter("building"))); 

        //Canvas canvas = new SvgCanvas(CanvasUnit.In, 11.0, 8.5, Color.White);
        map.Render(canvas);
        string filename = FileSystemHelpers.GetTempOutputFileName(
            canvas.DefaultFileExtension, "RenderSimpleOsmDataAndRaster_");
        canvas.SaveToFile(filename);
        Console.WriteLine(filename);
        canvas.Dispose();
    }
}
