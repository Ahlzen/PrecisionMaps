using Clipper2Lib;
using MapLib.Geometry.Helpers;

namespace MapLib.Geometry;

/// <summary>
/// Collection of 2D lines. Immutable.
/// </summary>
public class MultiLine : Shape, IEnumerable<Coord[]>
{
    public Coord[][] Coords { get; }

    public MultiLine(Coord[][] coords, TagDictionary? tags) : base(tags)
    {
        Coords = coords;
    }

    public MultiLine(Line line) : base(line.Tags)
    {
        Coords = [line.Coords];
    }

    public MultiLine(IEnumerable<Line> lines, TagDictionary? tags) : base(tags) {
        Coords = lines.Select(l => l.Coords).ToArray();
    }

    public MultiLine(IEnumerable<MultiLine> multiLines, TagDictionary? tags) : base(tags) {
        Coords = multiLines.SelectMany(ml => ml.Coords).ToArray();
    }

    /// <returns>
    /// Returns the lines transformed as (X*scale+offsetX, Y*scale+offsetY)
    /// </returns>
    public virtual MultiLine Transform(double scale, double offsetX, double offsetY)
        => new(Coords.Select(c => c.Transform(scale, offsetX, offsetY)).ToArray(), Tags);


    public virtual MultiLine Transform(Func<Coord, Coord> transformation)
        => new MultiLine(
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

    public override MultiPolygon Buffer(double radius)
    {
        PathsD paths = this.ToPathsD();
        PathsD result = Clipper.InflatePaths(paths, radius, JoinType.Round, EndType.Round);
        return result.ToMultiPolygon(Tags);
    }
}