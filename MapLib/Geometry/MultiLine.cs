using System.Collections;
using Clipper2Lib;
using MapLib.Geometry.Helpers;

namespace MapLib.Geometry;

/// <summary>
/// Collection of 2D lines. Immutable.
/// </summary>
public class MultiLine : Shape, IEnumerable<Line>
{
    public Line[] Lines { get; }

    public MultiLine(Line line) {
        Lines = [line];
    }

    public MultiLine(Line[] lines) {
        Lines = lines;
    }

    public MultiLine(IEnumerable<Line> lines) {
        Lines = lines.ToArray();
    }

    public MultiLine(IEnumerable<MultiLine> multiLines) {
        Lines = multiLines.SelectMany(ml => ml.Lines).ToArray();
    }

    public virtual MultiLine Transform(Func<Coord, Coord> transformation)
        => new MultiLine(Lines.Select(l => l.Transform(transformation)));

    public override Coord GetCenter()
        => GetBounds().Center;

    public override Bounds GetBounds() {
        if (_bounds == null)
            _bounds = Bounds.FromBounds(Lines.Select(l => l.GetBounds()));
        return _bounds.Value;
    }
    private Bounds? _bounds; // cached bounds

    public int Count => Lines.Length;

    public Line this[int i] {
        get => Lines[i];
    }

    public IEnumerator<Line> GetEnumerator() =>
        (IEnumerator<Line>)Lines.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public override MultiPolygon Buffer(double radius)
    {
        PathsD paths = this.ToPathsD();
        PathsD result = Clipper.InflatePaths(paths, radius, JoinType.Round, EndType.Round);
        return result.ToMultiPolygon();
    }
}