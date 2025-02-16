using BenchmarkDotNet.Attributes;
using MapLib.Geometry;
using MapLib.Geometry.Helpers;

namespace MapLib.Benchmarks.Geometry;

/// <summary>
/// Different line simplification algorithms on a "small" polygon.
/// </summary>
public class LineSimplificationBenchmarks_Polygons : BaseGeometryBenchmarks
{
    [Params(false, true)]
    public bool UseLargePolygon;

    [Params(0.5, 0.2, 0.1)]
    public double PointReductionFactor;

    public Coord[] Data => UseLargePolygon ? LargePolygonData : SmallPolygonData;
    public int TargetPointCount => (int)(Data.Length * PointReductionFactor);

    [Benchmark]
    public Coord[] SimplifySmallPolygon_VisvalingamWhyatt_Naive_ByPointCount()
        => VisvalingamWhyatt_Naive.Simplify(Data, TargetPointCount);

    [Benchmark]
    public Coord[] SimplifySmallPolygon_VisvalingamWhyatt_ByPointCount()
        => VisvalingamWhyatt.Simplify(Data, TargetPointCount);

    [Benchmark]
    public Coord[] SimplifySmallPolygon_DouglasPeucker_ByPointCount()
        => DouglasPeucker.Simplify(Data, TargetPointCount);
}
