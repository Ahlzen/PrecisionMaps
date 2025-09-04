using BenchmarkDotNet.Attributes;
using MapLib.Geometry;

namespace MapLib.Tests.Geometry;

/// <summary>
/// Measures performance offsetting polygons.
/// </summary>
public class OffsetBenchmark_Polygons : BaseGeometryBenchmark
{
    [Params(false, true)]
    public bool UseLargePolygon;

    [Params(1, 2, 5)]
    public int Iterations;

    [Params(false, true)]
    public bool Inward;

    public MultiPolygon Data => UseLargePolygon ? LargeMultiPolygon : SmallMultiPolygon;
    private double GeometrySize => Data.GetBounds().Size;

    [Benchmark]
    public MultiPolygon? OffsetPolygon_Clipper()
    {
        MultiPolygon? result = Data;
        double offsetAmount = GeometrySize / 50 * (Inward ? -1 : 1);
        for (int i = 0; i < Iterations; i++)
        {
            result = result?.Offset(offsetAmount);
            if (result == null)
                break;
        }
        return result;
    }
}
