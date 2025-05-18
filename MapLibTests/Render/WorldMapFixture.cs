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
public class WorldMapFixture : BaseFixture
{
    private static Color WaterColor = Color.CornflowerBlue;

    public static IEnumerable<Canvas> A3Canvases()
    {
        yield return new BitmapCanvas(CanvasUnit.Mm, 420.0, 297.0, WaterColor, 1.0);
        yield return new BitmapCanvas(CanvasUnit.Mm, 420.0, 297.0, WaterColor, 4.0);
        yield return new SvgCanvas(CanvasUnit.Mm, 420.0, 297.0, WaterColor);
    }

    [Test]
    [TestCaseSource(nameof(A3Canvases))]
    public void RenderWorldCountriesMap(Canvas canvas)
    {
        // Try something different: Van Der Grinten projection
        // (not uncommon for world maps)
        string srs = Transformer.WktVanDerGrinten;
        Map map = new Map(
            new Bounds(-180.0, 180.0, -80.0, 80.0),
            srs);

        // Add data
        map.DataSources.Add(
            new VectorMapDataSource("land",
                new VectorFileDataSource(Path.Join(TestDataPath,
                "Natural Earth/ne_110m_land.shp"))));
        map.DataSources.Add(
            new VectorMapDataSource("countries",
                new VectorFileDataSource(Path.Join(TestDataPath,
                "Natural Earth/ne_110m_admin_0_countries.shp"))));

        // Add styles
        map.Layers.Add(new VectorMapLayer(
            "landFill", "land", new VectorStyle {
                FillColor = Color.WhiteSmoke,
            }));
        map.Layers.Add(new VectorMapLayer(
            "borders", "countries", new VectorStyle {
                LineColor = Color.Silver,
                LineWidth = 0.2
            }));
        map.Layers.Add(new VectorMapLayer(
            "coastline", "land", new VectorStyle {
                LineColor = Color.Navy,
                LineWidth = 0.15
            }));
        map.Layers.Add(new VectorMapLayer(
            "country labels", "countries", new VectorStyle {
                TextColor = Color.Black,
                TextSize = 8,
                TextTag = "Name" // todo
            }));

        // Render and save
        map.Render(canvas, ratioMismatchStrategy: AspectRatioMismatchStrategy.CenterOnCanvas);
        string filename = FileSystemHelpers.GetTempOutputFileName(
            canvas.DefaultFileExtension, "WorldCountries");
        canvas.SaveToFile(filename);
    }
}