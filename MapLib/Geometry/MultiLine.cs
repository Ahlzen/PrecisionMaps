using System.Collections;
using Clipper2Lib;
using MapLib.Geometry.Helpers;

namespace MapLib.Geometry;

/// <summary>
/// Collection of 2D lines. Immutable.
/// </summary>
public class MultiLine : Shape, IEnumerable<Coord[]> //IEnumerable<Line>
{
    //public Line[] Lines { get; }
    public Coord[][] Coords { get; }

    public MultiLine(Coord[][] coords, TagDictionary? tags) : base(tags)
    {
        //Lines = [line];
        Coords = coords;

    }

    //public MultiLine(Line line, TagDictionary? tags) : base(tags) {
    public MultiLine(Line line) : base(line.Tags)
    {
        //Lines = [line];
        Coords = [line.Coords];
    }

    //public MultiLine(Line[] lines, TagDictionary? tags) : base(tags) {
    //    //Lines = lines;
    //    Coords = lines.Select(l => l.Coords).ToArray();
    //}

    public MultiLine(IEnumerable<Line> lines, TagDictionary? tags) : base(tags) {
        //Lines = lines.ToArray();
        Coords = lines.Select(l => l.Coords).ToArray();
    }

    public MultiLine(IEnumerable<MultiLine> multiLines, TagDictionary? tags) : base(tags) {
        //Lines = multiLines.SelectMany(ml => ml.Lines).ToArray();
        Coords = multiLines.SelectMany(ml => ml.Coords).ToArray();
    }

    public virtual MultiLine Transform(Func<Coord, Coord> transformation)
        //=> new MultiLine(Lines.Select(l => l.Transform(transformation)), Tags);
        => new MultiLine(
            Coords.Select(l => l.Select(c => transformation(c)).ToArray()).ToArray(), Tags);

    public override Coord GetCenter()
        => GetBounds().Center;

    public override Bounds GetBounds() {
        if (_bounds == null)
            //_bounds = Bounds.FromBounds(Lines.Select(l => l.GetBounds()));
            _bounds = Bounds.FromBounds(Coords.Select(Bounds.FromCoords));
        return _bounds.Value;
    }
    private Bounds? _bounds; // cached bounds

    //public int Count => Lines.Length;
    public int Count => Coords.Length;

    //public Line this[int i] {
    //    get => Lines[i];
    //}
    public Coord[] this[int i]
    {
        get => Coords[i];
    }

    //public IEnumerator<Line> GetEnumerator() =>
    //    (IEnumerator<Line>)Lines.GetEnumerator();
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