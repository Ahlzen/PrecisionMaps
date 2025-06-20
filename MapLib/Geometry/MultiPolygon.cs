using Clipper2Lib;
using MapLib.GdalSupport;
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
        Validate();
    }

    public MultiPolygon(Polygon polygon, TagList? tags) : base(tags) {
        Coords = [polygon.Coords];
        Validate();
    }

    public MultiPolygon(IEnumerable<Polygon> polygons, TagList? tags) : base(tags)
    {
        Coords = polygons.Select(p => p.Coords).ToArray();
        Validate();
    }

    public MultiPolygon(IEnumerable<MultiPolygon> multiPolygons, TagList? tags) : base(tags)
    {
        Coords = multiPolygons.SelectMany(mp =>  mp.Coords).ToArray();
        Validate();
    }

    private void Validate() => Validate(Coords);

    internal static void Validate(Coord[][] coords)
    {
        if (coords.Length == 0)
            throw new ArgumentException("A MultiPolygon requires at least one ring");
        foreach (Coord[] ring in coords)
            Polygon.Validate(ring);
    }

    #region Transformations

    /// <returns>
    /// Returns the polygons transformed as (X*scale+offsetX, Y*scale+offsetY)
    /// </returns>
    public virtual MultiPolygon Transform(double scale, double offsetX, double offsetY)
        => Transform(scale, scale, offsetX, offsetY);

    /// <returns>
    /// Returns the polygons transformed as (X*scaleX+offsetX, Y*scaleY+offsetY)
    /// </returns>
    public virtual MultiPolygon Transform(double scaleX, double scaleY, double offsetX, double offsetY)
        => new(Coords.Select(c => c.Transform(scaleX, scaleY, offsetX, offsetY)).ToArray(), Tags);

    public virtual MultiPolygon Transform(Func<Coord, Coord> transformation)
        => new MultiPolygon(
            Coords.Select(l => l.Select(c => transformation(c)).ToArray()).ToArray(), Tags);

    public MultiPolygon Transform(Transformer transformer)
        => new MultiPolygon(transformer.Transform(Coords), Tags);

    #endregion

    public override Coord GetCenter()
        => GetBounds().Center;

    public override Bounds GetBounds()
    {
        _bounds ??= Bounds.FromCoords(Coords);
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
        PathsD result, paths = this.ToPathsD();
        // Should call SimplifyPaths before InflatePaths,
        // see http://www.angusj.com/clipper2/Docs/Units/Clipper/Functions/SimplifyPaths.htm
        paths = Clipper.SimplifyPaths(paths,
            Clipper2Utils.GetSimplifyEpsilon(GetBounds()), true);
        result = Clipper.InflatePaths(paths, d, JoinType.Round, EndType.Polygon);
        return result.ToMultiPolygon(Tags);
    }

    public override MultiPolygon Buffer(double radius)
        => Offset(radius);

    public MultiPolygon Smooth_Chaikin(int iterations)
        => new MultiPolygon(Coords.Select(c => Chaikin.Smooth_Fixed(c, true, iterations)).ToArray(), Tags);

    public MultiPolygon Smooth_ChaikinAdaptive(double maxAngleDegrees)
        => new MultiPolygon(Coords.Select(c => Chaikin.Smooth_Adaptive(c, true, maxAngleDegrees)).ToArray(), Tags);


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