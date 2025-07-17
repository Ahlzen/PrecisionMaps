using System.Drawing;
using System.IO;
using MapLib.DataSources.Vector;
using MapLib.GdalSupport;
using MapLib.Output;
using MapLib.Render;
using MapLib.Util;

namespace MapLib.Tests.Render;

[TestFixture]
public class WorldMapFixture : BaseFixture
{
    private static Color WaterColor = Color.CornflowerBlue;

    public static IEnumerable<CanvasStack> A3CanvasStacks()
    {
        yield return new BitmapCanvasStack(CanvasUnit.Mm, 420.0, 297.0, WaterColor, 1.0);
        yield return new BitmapCanvasStack(CanvasUnit.Mm, 420.0, 297.0, WaterColor, 4.0);
        yield return new SvgCanvasStack(CanvasUnit.Mm, 420.0, 297.0, WaterColor);
    }

    [Test]
    [Explicit]
    [TestCaseSource(nameof(A3CanvasStacks))]
    public void RenderWorldCountriesMap(CanvasStack canvasStack)
    {
        // Try something different: Van Der Grinten projection
        // (not uncommon for world maps)
        string srs = Transformer.WktVanDerGrinten;
        Map map = new Map(
            new Bounds(-180.0, 180.0, -80.0, 80.0),
            srs);

        // Add data
        map.VectorDataSources.Add("land",
            new NaturalEarthVectorDataSource(NaturalEarthVectorDataSet.LandPolygons_110m));
        map.VectorDataSources.Add("countries",
            new NaturalEarthVectorDataSource(NaturalEarthVectorDataSet.Admin0_Countries_110m));
        map.VectorDataSources.Add("graticule",
            new GraticuleDataSource { XInterval = 10, YInterval = 10 });

        // Add styles
        map.MapLayers.Add(new VectorMapLayer(
            "graticule", "graticule", new VectorStyle {
                LineColor = Color.Navy,
                LineWidth = 0.15
            }));
        map.MapLayers.Add(new VectorMapLayer(
            "landFill", "land", new VectorStyle {
                FillColor = ColorUtil.FromHex("#efc"),
            }));
        map.MapLayers.Add(new VectorMapLayer(
            "borders", "countries", new VectorStyle {
                LineColor = Color.Silver,
                LineWidth = 0.2
            }));
        map.MapLayers.Add(new VectorMapLayer(
            "coastline", "land", new VectorStyle {
                LineColor = Color.Navy,
                LineWidth = 0.15
            }));
        map.MapLayers.Add(new VectorMapLayer(
            "country labels", "countries", new VectorStyle {
                Symbol = SymbolType.Circle,
                SymbolSize = 1,
                TextColor = Color.Black,
                TextSize = 2,
                TextTag = "NAME"
            }));;

        // Render and save
        map.Render(canvasStack,
            ratioMismatchStrategy: AspectRatioMismatchStrategy.CenterOnCanvas);
        string filename = FileSystemHelpers.GetTempOutputFileName(
            canvasStack.DefaultFileExtension, "WorldCountries");
        canvasStack.SaveToFile(filename);
    }

    [Test]
    [Explicit]
    [TestCaseSource(nameof(A3CanvasStacks))]
    public void RenderWorldCountriesMap_WithMasks(CanvasStack canvasStack)
    {
        // Try something different: Van Der Grinten projection
        // (not uncommon for world maps)
        string srs = Transformer.WktVanDerGrinten;
        Map map = new Map(
            new Bounds(-180.0, 180.0, -80.0, 80.0),
            srs);

        // Add data
        map.VectorDataSources.Add("land",
            new VectorFileDataSource(Path.Join(TestDataPath,
            "Natural Earth/ne_110m_land.shp")));
        map.VectorDataSources.Add("countries",
            new VectorFileDataSource(Path.Join(TestDataPath,
            "Natural Earth/ne_110m_admin_0_countries.shp")));
        map.VectorDataSources.Add("graticule",
            new GraticuleDataSource { XInterval = 10, YInterval = 10 });

        // Add styles
        map.MapLayers.Add(new VectorMapLayer(
            "graticule", "graticule", new VectorStyle {
                LineColor = Color.Navy,
                LineWidth = 0.15,
                MaskedBy = { "landMask", "labelsMask" }
            }));
        map.MapLayers.Add(new VectorMapLayer(
            "landFill", "land", new VectorStyle {
                MaskName = "landMask",
                FillColor = ColorUtil.FromHex("#efc"),
                PolygonMaskWidth = 0.5
            }));
        map.MapLayers.Add(new VectorMapLayer(
            "borders", "countries", new VectorStyle {
                LineColor = Color.Silver,
                LineWidth = 0.15,
                MaskedBy = { "labelsMask" }
            }));
        map.MapLayers.Add(new VectorMapLayer(
            "coastline", "land", new VectorStyle {
                LineColor = Color.Navy,
                LineWidth = 0.15,
                MaskedBy = { "labelsMask" }
            }));
        map.MapLayers.Add(new VectorMapLayer(
            "labels", "countries", new VectorStyle {
                MaskName = "labelsMask",
                Symbol = SymbolType.Circle,
                SymbolSize = 1,
                SymbolMaskWidth = 0.5,
                TextColor = Color.Black,
                TextSize = 2,
                TextTag = "NAME",
                TextMaskWidth = 0.5
            }));

        // Render and save
        map.Render(canvasStack,
            ratioMismatchStrategy: AspectRatioMismatchStrategy.CenterOnCanvas);

        canvasStack.SaveAllLayersToFile("WorldCountriesWithMasks");

        string filename = FileSystemHelpers.GetTempOutputFileName(
            canvasStack.DefaultFileExtension, "WorldCountriesWithMasks");
        canvasStack.SaveToFile(filename);
    }
}