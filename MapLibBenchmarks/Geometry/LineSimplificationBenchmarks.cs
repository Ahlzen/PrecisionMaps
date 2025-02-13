using System;
using System.Collections.Generic;
using System.Diagnostics;

using BenchmarkDotNet.Attributes;
using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Geometry.Helpers;

namespace MapLib.Benchmarks.Geometry;

public class LineSimplificationBenchmarks
{
    public Coord[] SmallPolygon { get; }
    public Coord[] LargePolygon { get; }

    public LineSimplificationBenchmarks()
    {
        GdalUtils.Initialize();

        SmallPolygon = DataHelpers.LoadFistPolygonCoordsFromTestData("Aaron River Reservoir.geojson");
        Debug.Assert(SmallPolygon.Length > 10); // ensure we have the right polygon

        LargePolygon = DataHelpers.LoadFistPolygonCoordsFromTestData("Natural Earth/ne_110m_land.shp");
        Debug.Assert(LargePolygon.Length > 1000); // ensure we have the right polygon
    }


    [Benchmark]
    public Coord[] SimplifySmallMultipolygon_VisvalingamWhyatt_Naive_ByPointCount()
        => VisvalingamWhyatt_Naive.Simplify(
            SmallPolygon, SmallPolygon.Length / 6);

    [Benchmark]
    public Coord[] SimplifySmallMultipolygon_VisvalingamWhyatt_ByPointCount()
        => VisvalingamWhyatt.Simplify(
            SmallPolygon, SmallPolygon.Length / 6);

    [Benchmark]
    public Coord[] SimplifySmallMultipolygon_DouglasPeucker_ByPointCount()
        => DouglasPeucker.Simplify(
            SmallPolygon, SmallPolygon.Length / 6);


    [Benchmark]
    public Coord[] SimplifyLargeMultipolygon_VisvalingamWhyatt_Naive()
        => VisvalingamWhyatt_Naive.Simplify(
            LargePolygon, LargePolygon.Length / 6);

    [Benchmark]
    public Coord[] SimplifyLargeMultipolygon_VisvalingamWhyatt()
        => VisvalingamWhyatt.Simplify(
            LargePolygon, LargePolygon.Length / 6);

    [Benchmark]
    public Coord[] SimplifyLargeMultipolygon_DouglasPeucker_ByPointCount()
        => DouglasPeucker.Simplify(
            LargePolygon, LargePolygon.Length / 6);
}

