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
        // (not that uncommon for world maps)
        string srs = Transformer.WktVanDerGrinten;
        Map map = new Map(
            new Bounds(-180.0, 180.0, -80.0, 80.0),
            srs);

        map.DataSources.Add(
            new VectorMapDataSource("land",
                new VectorFileDataSource(Path.Join(TestDataPath, "Natural Earth/ne_110m_land.shp"))));

        map.Layers.Add(new VectorMapLayer("landLayer", "land", null, Color.WhiteSmoke, Color.Navy, 0.1));
    }
}