using System.Collections;
using Clipper2Lib;
using MapLib.Geometry.Helpers;

namespace MapLib.Geometry;

/// <summary>
/// Collection of 2D polygons. Immutable.
/// </summary>
public class MultiPolygon : Shape, IEnumerable<Polygon>
{
    public Polygon[] Polygons { get; }

    public MultiPolygon(Polygon polygon) {
        Polygons = [polygon];
    }

    public MultiPolygon(Polygon[] polygons) {
        Polygons = polygons;
    }

    public MultiPolygon(IEnumerable<Polygon> polygons) {
        Polygons = polygons.ToArray();
    }

    public MultiPolygon(IEnumerable<MultiPolygon> multiPolygons) {
        Polygons = multiPolygons.SelectMany(mp => mp.Polygons).ToArray();
    }

    public virtual MultiPolygon Transform(Func<Coord, Coord> transformation)
        => new MultiPolygon(Polygons.Select(p => p.Transform(transformation)));

    public override Coord GetCenter()
        => GetBounds().Center;

    public override Bounds GetBounds() {
        if (_bounds == null)
            _bounds = Bounds.FromBounds(Polygons.Select(p => p.GetBounds()));
        return _bounds.Value;
    }
    private Bounds? _bounds; // cached bounds

    public int Count => Polygons.Length;

    public Polygon this[int i] {
        get => Polygons[i];
    }

    public IEnumerator<Polygon> GetEnumerator() {
        foreach (Polygon polygon in Polygons)
            yield return polygon;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public MultiPolygon Offset(double d) {
        PathsD paths = this.ToPathsD();
        PathsD result = Clipper.InflatePaths(paths, d, JoinType.Round, EndType.Round);
        return result.ToMultiPolygon();
    }

    public override MultiPolygon Buffer(double radius)
        => Offset(radius);

    /// <summary>
    /// Merges any overlapping areas (self-union).
    /// </summary>
    public MultiPolygon Merge()
    {
        PathsD paths = this.ToPathsD();
        PathsD result = Clipper.Union(paths, FillRule.Positive);
        return result.ToMultiPolygon();
    }
}