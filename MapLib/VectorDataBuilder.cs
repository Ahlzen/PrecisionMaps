using MapLib.Geometry;
using Point = MapLib.Geometry.Point;

namespace MapLib;

/// <summary>
/// Mutable collection of vector features (shapes).
/// </summary>
public class VectorDataBuilder
{
    public List<Point> Points { get; set; } = new();
    public List<MultiPoint> MultiPoints { get; set; } = new();

    public List<Line> Lines { get; set; } = new();
    public List<MultiLine> MultiLines { get; set; } = new();

    public List<Polygon> Polygons { get; set; } = new();
    public List<MultiPolygon> MultiPolygons { get; set; } = new();

    public VectorData ToVectorData(string srs)
    {
        return new VectorData(srs,
            Points.ToArray(),
            MultiPoints.ToArray(),
            Lines.ToArray(),
            MultiLines.ToArray(),
            Polygons.ToArray(),
            MultiPolygons.ToArray());
    }
}
