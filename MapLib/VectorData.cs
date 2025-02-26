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
        string srs,
        Point[]? points, MultiPoint[]? multiPoints,
        Line[]? lines, MultiLine[]? multiLines,
        Polygon[]? polygons, MultiPolygon[]? multiPolygons)
        : base(srs)
    {
        Points = points ?? [];
        MultiPoints = multiPoints ?? [];

        Lines = lines ?? [];
        MultiLines = multiLines ?? [];

        Polygons = polygons ?? [];
        MultiPolygons = multiPolygons ?? [];

        Bounds = ComputeBounds();
    }

    public Point[] Points { get; }
    public MultiPoint[] MultiPoints { get; }
    
    public Line[] Lines { get; }
    public MultiLine[] MultiLines { get; }
    
    public Polygon[] Polygons { get; }
    public MultiPolygon[] MultiPolygons { get; }

    public override Bounds Bounds { get; }

    public int Count =>
        Points.Length + MultiPoints.Length +
        Lines.Length + MultiLines.Length +
        Polygons.Length + MultiPolygons.Length;

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
        => new(this.Srs,
            Points.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray(),
            MultiPoints.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray(),
            Lines.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray(),
            MultiLines.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray(),
            Polygons.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray(),
            MultiPolygons.Select(feature => feature.Transform(scale, offsetX, offsetY)).ToArray());
}
