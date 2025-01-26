using MapLib.Geometry;
using Point = MapLib.Geometry.Point;

namespace MapLib;

/// <summary>
/// Multable collection of vectory features (shapes).
/// </summary>
public class VectorDataBuilder
{
    public List<Point> Points { get; set; } = new();
    public List<MultiPoint> MultiPoints { get; set; } = new();

    public List<Line> Lines { get; set; } = new();
    public List<MultiLine> MultiLines { get; set; } = new();

    public List<Polygon> Polygons { get; set; } = new();
    public List<MultiPolygon> MultiPolygons { get; set; } = new();

    public VectorData ToVectorData()
    {
        return new VectorData(
            Points.ToArray(),
            MultiPoints.ToArray(),
            Lines.ToArray(),
            MultiLines.ToArray(),
            Polygons.ToArray(),
            MultiPolygons.ToArray());
    }
}
