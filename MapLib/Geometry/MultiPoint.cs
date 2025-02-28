using MapLib.GdalSupport;

namespace MapLib.Geometry;

/// <summary>
/// Collection of points. Immutable.
/// </summary>
public class MultiPoint : Shape, IEnumerable<Coord>
{
    public Coord[] Coords { get; }

    public MultiPoint(Coord coord, TagList? tags) : base(tags) {
        Coords = [coord];
    }

    public MultiPoint(Coord[] coords, TagList? tags) : base(tags)
    {
        Coords = coords;
    }
        
    public MultiPoint(IEnumerable<Coord> coords, TagList? tags) : base(tags)
    {
        Coords = coords.ToArray();
    }

    public MultiPoint(Point point, TagList? tags) : base(tags)
    {
        Coords = [point.Coord];
    }

    public MultiPoint(IEnumerable<Point> points, TagList? tags) : base(tags)
    {
        Coords = points.Select(p => p.Coord).ToArray();
    }

    public MultiPoint(IEnumerable<MultiPoint> multiPoints, TagList? tags) : base(tags)
    {
        Coords = multiPoints.SelectMany(mp => mp.Coords).ToArray();
    }

    #region Transformations

    /// <returns>
    /// Returns the points transformed as (X*scale+offsetX, Y*scale+offsetY)
    /// </returns>
    public virtual MultiPoint Transform(double scale, double offsetX, double offsetY)
        => Transform(scale, scale, offsetX, offsetY);

    /// <returns>
    /// Returns the points transformed as (X*scaleX+offsetX, Y*scaleY+offsetY)
    /// </returns>
    public virtual MultiPoint Transform(double scaleX, double scaleY, double offsetX, double offsetY)
        => new(Coords.Transform(scaleX, scaleY, offsetX, offsetY), Tags);

    public virtual MultiPoint Transform(Func<Coord, Coord> transformation)
        => new MultiPoint(Coords.Select(c => transformation(c)), Tags);

    public MultiPoint Transform(Transformer transformer)
        => new MultiPoint(transformer.Transform(Coords), Tags);

    #endregion

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
            Coords.Select(c => Point.CreateBuffer(c, radius)).ToArray(), Tags);
        return mp.Merge();
    }
}