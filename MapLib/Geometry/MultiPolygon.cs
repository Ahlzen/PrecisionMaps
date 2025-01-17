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
        Polygons = new Polygon[] { polygon };
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

        //// Convert to clipper's int coords
        //double scale = ClipperUtils.GetScale(d, this);
        //List<List<IntPoint>> inPolys = this.ToIntPoints(scale);

        //// Perform the offset
        //List<List<IntPoint>> outPolyPoints =
        //    Clipper.OffsetPolygons(inPolys, (long)(d * scale));

        //// Convert back to fp coords
        //var mp = outPolyPoints.ToMultiPolygon(scale);
        //return mp;
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

        //var clipper = new ClipperD(); // TODO: set rounding decimal precision?
        //foreach (Polygon polygon in this)
        //    clipper.AddSubject(polygon.ToPathD());
        //PathsD solution = new();
        //bool result = clipper.Execute(ClipType.Union, FillRule.EvenOdd, solution);
        //// TODO: check result?
        //return solution.ToMultiPolygon();

        //double scale = ClipperUtils.GetScale(0, this);
        //Clipper clipper = new Clipper();
        //foreach (Polygon polygon in this)
        //    clipper.AddPolygon(polygon.ToIntPoints(scale), PolyType.ptSubject);
        //var outPolyPoints = new List<List<IntPoint>>();
        //clipper.Execute(ClipType.ctUnion, outPolyPoints);
        //return outPolyPoints.ToMultiPolygon(scale);
    }
}