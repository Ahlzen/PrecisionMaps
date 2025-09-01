using System.Drawing;
using System.IO;
using MapLib.DataSources.Vector;
using MapLib.GdalSupport;
using MapLib.Render;
using MapLib.Output;
using MapLib.Geometry.Helpers;
using MapLib.Util;
using Point = MapLib.Geometry.Point;

namespace MapLib.Tests.Geometry;

[TestFixture]
public class PointInPolygonFixture : BaseFixture
{
    /// <summary>
    /// Generates a world map and renders
    /// various points for each country polygon:
    ///  * Center of bounding box (red)
    ///  * Pole of inaccessibility (PolyLabel) (green)
    ///  * TODO: Centroid (center of gravity) (blue)
    /// </summary>
    [Test]
    public async Task VisualizePointsInPolygons()
    {
        const double pointSize = 0.8;
        Srs srs = Srs.WebMercator;
        TagList tags = [];

        // Get data
        VectorFileDataSource dataSource = new(Path.Join(TestDataPath,
            "Natural Earth/ne_110m_admin_0_countries.shp"));
        VectorData countryData = await dataSource.GetData(srs);

        var vb = new VectorDataBuilder();
        vb.Points.AddRange(countryData.Polygons.Select(p => new Point(p.GetBounds().Center, tags)));
        vb.Points.AddRange(countryData.MultiPolygons.Select(mp => new Point(mp.GetBounds().Center, tags)));
        VectorData bboxCenterPoints = vb.ToVectorData(srs);

        vb = new VectorDataBuilder();
        vb.Points.AddRange(countryData.Polygons.Select(p => new Point(PolyLabel.Calculate(p), tags)));
        vb.Points.AddRange(countryData.MultiPolygons.Select(mp => new Point(PolyLabel.Calculate(mp), tags)));
        VectorData polyLabelPoints = vb.ToVectorData(srs);

        BitmapCanvasStack canvas = new(CanvasUnit.Mm, 420.0, 297.0, Color.White, 4.0);
        Map map = new(new Bounds(-180.0, 180.0, -80.0, 80.0), srs);
        map.VectorDataSources.Add("countries", new ExistingVectorDataSource(countryData));
        map.VectorDataSources.Add("bboxCenterPoints", new ExistingVectorDataSource(bboxCenterPoints));
        map.VectorDataSources.Add("polyLabelPoints", new ExistingVectorDataSource(polyLabelPoints));
        map.MapLayers.Add(new VectorMapLayer(
            "borders", "countries", new VectorStyle {
                LineColor = Color.DimGray,
                LineWidth = 0.3
            }));
        map.MapLayers.Add(new VectorMapLayer(
            "bboxCenterPoints", "bboxCenterPoints", new VectorStyle {
                SymbolSize = pointSize,
                Symbol = SymbolType.Circle,
                SymbolColor = Color.Red
            }));
        map.MapLayers.Add(new VectorMapLayer(
            "bboxCenterPoints", "polyLabelPoints", new VectorStyle {
                SymbolSize = pointSize,
                Symbol = SymbolType.Circle,
                SymbolColor = Color.Green
            }));
        await map.Render(canvas,
            ratioMismatchStrategy: AspectRatioMismatchStrategy.CenterOnCanvas);
        string filename = FileSystemHelpers.GetTempOutputFileName(
            canvas.DefaultFileExtension, "PointsInPolygons");
        canvas.SaveToFile(filename);
    }
}