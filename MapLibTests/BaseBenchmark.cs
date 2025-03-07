using MapLib.GdalSupport;
using System.Diagnostics;

namespace MapLib.Tests;

public class BaseBenchmark
{
    public BaseBenchmark()
    {
        GdalUtils.Initialize();
    }
}

public class BaseGeometryBenchmark : BaseBenchmark
{
    public Polygon SmallPolygon { get; }
    public Polygon LargePolygon { get; }
    public MultiPolygon SmallMultiPolygon { get; }
    public MultiPolygon LargeMultiPolygon { get; }

    public Coord[] SmallPolygonData { get; }
    public Coord[] LargePolygonData { get; }

    public BaseGeometryBenchmark()
    {
        // Prepare test geometry
        SmallPolygon = BenchmarkDataHelpers.LoadFirstPolygonFromTestData("GeoJSON/Aaron River Reservoir.geojson");
        SmallMultiPolygon = SmallPolygon.AsMultiPolygon();
        SmallPolygonData = BenchmarkDataHelpers.LoadFirstPolygonCoordsFromTestData("GeoJSON/Aaron River Reservoir.geojson");
        Debug.Assert(SmallPolygonData.Length > 10); // ensure we have the right polygon

        LargePolygon = BenchmarkDataHelpers.LoadFirstPolygonFromTestData("Natural Earth/ne_110m_land.shp");
        LargeMultiPolygon = LargePolygon.AsMultiPolygon();
        LargePolygonData = BenchmarkDataHelpers.LoadFirstPolygonCoordsFromTestData("Natural Earth/ne_110m_land.shp");
        Debug.Assert(LargePolygonData.Length > 1000); // ensure we have the right polygon
    }
}