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
    public static IEnumerable<CanvasStack> LetterSizeCanvases()
    {
        yield return new BitmapCanvasStack(CanvasUnit.In, 11.0, 8.5, Color.White, 1.0);
        yield return new BitmapCanvasStack(CanvasUnit.In, 11.0, 8.5, Color.White, 4.0);
        yield return new SvgCanvasStack(CanvasUnit.In, 11.0, 8.5, Color.White);
    }

    [Test]
    [TestCaseSource(nameof(LetterSizeCanvases))]
    public async Task RenderSimpleOsmData(CanvasStack canvas)
    {
        Map map = new Map(
            new Bounds(-70.944, -70.915, 42.187, 42.207),
            Srs.WebMercator);

        map.VectorDataSources.Add("osmdata",
            new VectorFileDataSource(Path.Join(TestDataPath, "osm-xml/Weymouth Detail.osm")));

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
                style: new VectorStyle {
                    FillColor = Color.Tan },
                filter: new TagFilter("building")));

        await map.Render(canvas);
        string filename = FileSystemHelpers.GetTempOutputFileName(
            canvas.DefaultFileExtension, "RenderSimpleOsmData_");
        canvas.SaveToFile(filename);
        Console.WriteLine(filename);
        canvas.Dispose();
    }

    [Test]
    [TestCaseSource("LetterSizeCanvases")]
    public async Task RenderSimpleOsmDataAndRaster(CanvasStack canvas)
    {
        Map map = new Map(
            new Bounds(-70.944, -70.915, 42.187, 42.207),
            Srs.WebMercator);

        map.VectorDataSources.Add("osmdata",
            new VectorFileDataSource(Path.Join(TestDataPath, "osm-xml/Weymouth Detail.osm")));
        map.RasterDataSources.Add("ortophoto",
            new GdalDataSource(Path.Join(TestDataPath, "MassGIS Aerial/19TCG390725.jp2")));

        map.MapLayers.Add(
            new RasterMapLayer("ortophoto", "ortophoto", new RasterStyle()));

        map.MapLayers.Add(
            new VectorMapLayer("Water", "osmdata",
                style: new VectorStyle {
                    FillColor = Color.CornflowerBlue,
                    LineColor = Color.Blue,
                    LineWidth = 0.005 },
                filter: new TagFilter("natural", "water")));
        
        VectorStyle roadCasing = new() { LineColor = Color.Black };
        map.MapLayers.Add(
            new VectorMapLayer("Highway-1", "osmdata",
                style: roadCasing with { LineWidth = 0.04 },
                filter: new TagFilter(("highway", "motorway"), ("highway", "trunk"), ("highway", "primary"))));
        map.MapLayers.Add(
            new VectorMapLayer("Highway-2", "osmdata",
                style: roadCasing with { LineWidth = 0.03 },
                filter: new TagFilter(("highway", "secondary"), ("highway", "tertiary"))));
        map.MapLayers.Add(
            new VectorMapLayer("Highway-3", "osmdata",
                style: roadCasing with { LineWidth = 0.02 },
                filter: new TagFilter(("highway", "residential"), ("highway", "unclassified"))));

        map.MapLayers.Add(
            new VectorMapLayer("Highway-1-fill", "osmdata",
                style: new VectorStyle {
                    LineColor = ColorUtil.FromHex("#fe9"),
                    LineWidth = 0.02 },
                filter: new TagFilter(("highway", "motorway"), ("highway", "trunk"), ("highway", "primary"))));
        map.MapLayers.Add(
            new VectorMapLayer("Highway-2-fill", "osmdata",
                style: new VectorStyle {
                    LineColor = ColorUtil.FromHex("#fb9"),
                    LineWidth = 0.015 },
                filter: new TagFilter(("highway", "secondary"), ("highway", "tertiary"))));
        map.MapLayers.Add(
            new VectorMapLayer("Highway-3-fill", "osmdata",
                style: new VectorStyle {
                    LineColor = ColorUtil.FromHex("#fee"),
                    LineWidth = 0.01 },
                filter: new TagFilter(("highway", "residential"), ("highway", "unclassified"))));

        map.MapLayers.Add(
            new VectorMapLayer("Building", "osmdata",
                style: new VectorStyle { FillColor = Color.Tan },
                filter: new TagFilter("building"))); 

        await map.Render(canvas);
        string filename = FileSystemHelpers.GetTempOutputFileName(
            canvas.DefaultFileExtension, "RenderSimpleOsmDataAndRaster_");
        canvas.SaveToFile(filename);
        Console.WriteLine(filename);
        canvas.Dispose();
    }

    [Test]
    [TestCaseSource("LetterSizeCanvases")]
    public async Task TestRenderMassachusettsTopoMap(CanvasStack canvas)
    {
        // Get 3DEP DEM data
        Usgs3depDataSource source = new(scaleFactor: 0.1);
        RasterData data = await source.GetData(MassachusettsBounds);
        SingleBandRasterData? demData = data as SingleBandRasterData;
        Assert.That(demData, Is.Not.Null);

        // Run hillshade
        var lightHillshadeData = demData!
            .Scale(3)
            .Hillshade_Basic()
            .Offset(128f);

        // Build hypsometric tint gradient
        Gradient gradient = new();
        gradient.Add(0.0f, (0.6f, 1.0f, 0.3f));
        gradient.Add(0.2f, (0.9f, 1.0f, 0.1f));
        gradient.Add(0.4f, (1.0f, 0.6f, 0.1f));
        gradient.Add(0.9f, (0.9f, 0.9f, 0.9f));
        gradient.Add(1.0f, (0.8f, 0.9f, 1.0f));
        ImageRasterData steppedHypso = demData!
            .GenerateSteps(100, 0)
            .Normalize()
            .GradientMap(gradient);

        // Create hillshade/hypsometric tint composite
        ImageRasterData compositeSteppedHypso = lightHillshadeData
            .Normalize()
            .ToImageRasterData()
            .BlendWith(steppedHypso, BlendMode.Normal, 0.3f);
        SaveTempBitmap(compositeSteppedHypso.Bitmap, "compositeSteppedHypso", ".jpg");

        // Generate contour lines
        string contourTempPath = FileSystemHelpers.GetTempOutputFileName(
            ".shp", "ma_contours");
        await GdalContourGenerator.GenerateContours(source, 1,
            100, 0, contourTempPath, MassachusettsBounds);

        Map map = new Map(MassachusettsBounds, Srs.WebMercator);

        map.RasterDataSources.Add("hillshadeComposite",
            new ExistingRasterDataSource(compositeSteppedHypso));
        map.VectorDataSources.Add("contours",
            new VectorFileDataSource(contourTempPath));

        map.MapLayers.Add(new RasterMapLayer("hillshadeComposite", "hillshadeComposite", new RasterStyle()));
        map.MapLayers.Add(new VectorMapLayer("contours", "contours",
            style: new VectorStyle {
                LineColor = Color.FromArgb(120, 0, 0, 0),
                LineWidth = 0.003f}));

        await map.Render(canvas);
        string filename = FileSystemHelpers.GetTempOutputFileName(
            canvas.DefaultFileExtension, "MassachusettsTopo");
        canvas.SaveToFile(filename);
        Console.WriteLine(filename);
        canvas.Dispose();
    }
}
