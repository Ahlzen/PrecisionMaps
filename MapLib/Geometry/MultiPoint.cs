using System.Collections;

namespace MapLib.Geometry;

/// <summary>
/// Collection of points. Immutable.
/// </summary>
public class MultiPoint : Shape, IEnumerable<Coord>
{
    // 
    public Coord[] Coords { get; }

    public MultiPoint(Coord coord) {
        Coords = new Coord[] { coord };
    }

    public MultiPoint(Coord[] coords) {
        Coords = coords;
    }
        
    public MultiPoint(IEnumerable<Coord> coords) {
        Coords = Coords.ToArray();
    }

    public MultiPoint(Point point) {
        Coords = new Coord[] { point.Coord };
    }

    public MultiPoint(IEnumerable<Point> points) {
        Coords = points.Select(p => p.Coord).ToArray();
    }

    public MultiPoint(IEnumerable<MultiPoint> multiPoints) {
        Coords = multiPoints.SelectMany(mp => mp.Coords).ToArray();
    }

    public virtual MultiPoint Transform(Func<Coord, Coord> transformation)
        => new MultiPoint(Coords.Select(c => transformation(c)));

    public override Coord GetCenter()
        => GetBounds().Center;

    public override Bounds GetBounds()
    {
        if (_bounds == null)
            _bounds = Bounds.FromCoords(Coords);
        return _bounds.Value;
    }
    private Bounds? _bounds; // cached bounds

    public int Count => Coords.Length;

    public Coord this[int i] {
        get => Coords[i];
    }

    public IEnumerator<Coord> GetEnumerator() =>
        (IEnumerator<Coord>)Coords.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public override MultiPolygon Buffer(double radius) {
        var mp = new MultiPolygon(
            Coords.Select(c => Point.CreateBufferPolygon(c, radius)));
        return mp.Merge();
    }
}