using MapLib.GdalSupport;
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

    #region Transformations

    /// <returns>
    /// Returns all geometry transformed as (X*scale+offsetX, Y*scale+offsetY)
    /// </returns>
    public virtual VectorData Transform(double scale, double offsetX, double offsetY)
        => Transform(scale, scale, offsetX, offsetY);

    /// <returns>
    /// Returns all geometry transformed as (X*scaleX+offsetX, Y*scaleY+offsetY)
    /// </returns>
    public virtual VectorData Transform(double scaleX, double scaleY, double offsetX, double offsetY)
        => new(this.Srs,
            Points.Select(feature => feature.Transform(scaleX, scaleY, offsetX, offsetY)).ToArray(),
            MultiPoints.Select(feature => feature.Transform(scaleX, scaleY, offsetX, offsetY)).ToArray(),
            Lines.Select(feature => feature.Transform(scaleX, scaleY, offsetX, offsetY)).ToArray(),
            MultiLines.Select(feature => feature.Transform(scaleX, scaleY, offsetX, offsetY)).ToArray(),
            Polygons.Select(feature => feature.Transform(scaleX, scaleY, offsetX, offsetY)).ToArray(),
            MultiPolygons.Select(feature => feature.Transform(scaleX, scaleY, offsetX, offsetY)).ToArray());



    public VectorData Transform(Transformer transformer)
        => new VectorData(transformer.DestSrs,
            Points.Select(p => p.Transform(transformer)).ToArray(),
            MultiPoints.Select(mp => mp.Transform(transformer)).ToArray(),
            Lines.Select(l => l.Transform(transformer)).ToArray(),
            MultiLines.Select(ml => ml.Transform(transformer)).ToArray(),
            Polygons.Select(p => p.Transform(transformer)).ToArray(),
            MultiPolygons.Select(mp => mp.Transform(transformer)).ToArray());

    #endregion
}
