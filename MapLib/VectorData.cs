using MapLib.Geometry;

namespace MapLib;

/// <summary>
/// Immutable collection of vector features (shapes)
/// </summary>
/// <remarks>
/// This mostly implements the data structures for
/// OGC Simple Feature Access.
/// </remarks>
public class VectorData : GeoData
{
    /// <remarks>
    /// At least one of the geometry parameters must be non-empty.
    /// </remarks>
    public VectorData(
        Point[]? points, MultiPoint[]? multiPoints,
        Line[]? lines, MultiLine[]? multiLines,
        Polygon[]? polygons, MultiPolygon[]? multiPolygons)
    {
        Points = points ?? new Point[0];
        MultiPoints = multiPoints ?? new MultiPoint[0];

        Lines = lines ?? new Line[0];
        MultiLines = multiLines ?? new MultiLine[0];

        Polygons = polygons ?? new Polygon[0];
        MultiPolygons = multiPolygons ?? new MultiPolygon[0];

        Bounds = ComputeBounds();
    }

    public Point[] Points { get; }
    public MultiPoint[] MultiPoints { get; }
    
    public Line[] Lines { get; }
    public MultiLine[] MultiLines { get; }
    
    public Polygon[] Polygons { get; }
    public MultiPolygon[] MultiPolygons { get; }

    public override Bounds Bounds { get; }

    private Bounds ComputeBounds()
    {
        Bounds? bounds = null;
        if (Points.Any()) bounds += ComputeBounds(Points);
        if (MultiPoints.Any()) bounds += ComputeBounds(MultiPoints);
        if (Lines.Any()) bounds += ComputeBounds(Lines);
        if (MultiLines.Any()) bounds += ComputeBounds(MultiLines);
        if (Polygons.Any()) bounds += ComputeBounds(Polygons);
        if (MultiPolygons.Any()) bounds += ComputeBounds(MultiPolygons);

        if (bounds == null)
            throw new InvalidOperationException("No geometry to compute bounds.");
        return bounds.Value;
    }

    // TODO: This can probably be made more efficient. Refactor.
    private Bounds ComputeBounds(IEnumerable<Shape> shapes)
        => Bounds.FromBounds(shapes.Select(s => s.GetBounds()));

    /// <returns>
    /// Returns all geometry transformed as (X*scale+offsetX, Y*scale+offsetY)
    /// </returns>
    public virtual VectorData Transform(double scale, double offsetX, double offsetY)
        => new(
            Points.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray(),
            MultiPoints.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray(),
            Lines.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray(),
            MultiLines.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray(),
            Polygons.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray(),
            MultiPolygons.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray());
}
