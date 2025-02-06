using Clipper2Lib;
using MapLib.Geometry.Helpers;

namespace MapLib.Geometry;

/// <summary>
/// Collection of 2D polygons. Immutable.
/// </summary>
public class MultiPolygon : Shape, IEnumerable<Coord[]>
{
    public Coord[][] Coords { get; }

    public MultiPolygon(Coord[][] coords, TagList? tags) : base(tags)
    {
        Coords = coords;
    }

    public MultiPolygon(Polygon polygon, TagList? tags) : base(tags) {
        Coords = [polygon.Coords];
    }

    public MultiPolygon(IEnumerable<Polygon> polygons, TagList? tags) : base(tags)
    {
        Coords = polygons.Select(p => p.Coords).ToArray();
    }

    public MultiPolygon(IEnumerable<MultiPolygon> multiPolygons, TagList? tags) : base(tags)
    {
        Coords = multiPolygons.SelectMany(mp =>  mp.Coords).ToArray();
    }

    /// <returns>
    /// Returns the polygons transformed as (X*scale+offsetX, Y*scale+offsetY)
    /// </returns>
    public virtual MultiPolygon Transform(double scale, double offsetX, double offsetY)
        => new(Coords.Select(c => c.Transform(scale, offsetX, offsetY)).ToArray(), Tags);

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
        ((IEnumerable<Coord[]>)Coords).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public MultiPolygon Offset(double d) {
        PathsD paths = this.ToPathsD();
        PathsD result = Clipper.InflatePaths(paths, d, JoinType.Round, EndType.Polygon);
        return result.ToMultiPolygon(Tags);
    }

    public override MultiPolygon Buffer(double radius)
        => Offset(radius);

    public new MultiPolygon Smooth_Chaikin(int iterations)
        => throw new NotImplementedException();

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