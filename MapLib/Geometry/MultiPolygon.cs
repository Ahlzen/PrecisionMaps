using Clipper2Lib;
using MapLib.Geometry.Helpers;

namespace MapLib.Geometry;

/// <summary>
/// Collection of 2D polygons. Immutable.
/// </summary>
public class MultiPolygon : Shape, IEnumerable<Coord[]>
{
    public Coord[][] Coords { get; }

    public MultiPolygon(Coord[][] coords, TagDictionary? tags) : base(tags)
    {
        Coords = coords;
    }

    public MultiPolygon(Polygon polygon, TagDictionary? tags) : base(tags) {
        Coords = [polygon.Coords];
    }

    public MultiPolygon(IEnumerable<Polygon> polygons, TagDictionary? tags) : base(tags)
    {
        Coords = polygons.Select(p => p.Coords).ToArray();
    }

    public MultiPolygon(IEnumerable<MultiPolygon> multiPolygons, TagDictionary? tags) : base(tags)
    {
        Coords = multiPolygons.SelectMany(mp =>  mp.Coords).ToArray();
    }

    public virtual MultiPolygon Transform(Func<Coord, Coord> transformation)
        => new MultiPolygon(
            Coords.Select(l => l.Select(c => transformation(c)).ToArray()).ToArray(), Tags);

    public override Coord GetCenter()
        => GetBounds().Center;

    public override Bounds GetBounds() {
        if (_bounds == null)
            _bounds = Bounds.FromBounds(Coords.Select(Bounds.FromCoords));
        return _bounds.Value;
    }
    private Bounds? _bounds; // cached bounds

    public int Count => Coords.Length;

    public Coord[] this[int i]
    {
        get => Coords[i];
    }

    public IEnumerator<Coord[]> GetEnumerator() =>
        (IEnumerator<Coord[]>)Coords.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public MultiPolygon Offset(double d) {
        PathsD paths = this.ToPathsD();
        PathsD result = Clipper.InflatePaths(paths, d, JoinType.Round, EndType.Polygon);
        return result.ToMultiPolygon(Tags);
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
        return result.ToMultiPolygon(Tags);
    }
}