using MapLib.Geometry;

namespace MapLib;

/// <summary>
/// Partial implementation of OGC Simple Feature Access
/// using the geometry classes in this namespace.
/// </summary>
public class VectorData
{
    public List<Point> Points { get; } = new();
    public List<MultiPoint> MultiPoints { get; } = new();

    public List<Line> Lines { get; } = new();
    public List<MultiLine> MultiLines { get; } = new();

    public List<Polygon> Polygons { get; } = new();
    public List<MultiPolygon> MultiPolygons { get; } = new();
}
