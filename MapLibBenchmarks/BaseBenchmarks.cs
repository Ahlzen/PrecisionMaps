using MapLib.GdalSupport;
using MapLib.Geometry;
using System.Diagnostics;

namespace MapLib.Benchmarks;

public class BaseBenchmarks
{
    public BaseBenchmarks()
    {
        GdalUtils.Initialize();
    }
}

public class BaseGeometryBenchmarks : BaseBenchmarks
{
    public Polygon SmallPolygon { get; }
    public Polygon LargePolygon { get; }
    public MultiPolygon SmallMultiPolygon { get; }
    public MultiPolygon LargeMultiPolygon { get; }

    public Coord[] SmallPolygonData { get; }
    public Coord[] LargePolygonData { get; }

    public BaseGeometryBenchmarks()
    {
        // Prepare test geometry
        SmallPolygon = DataHelpers.LoadFirstPolygonFromTestData("Aaron River Reservoir.geojson");
        SmallMultiPolygon = SmallPolygon.AsMultiPolygon();
        SmallPolygonData = DataHelpers.LoadFirstPolygonCoordsFromTestData("Aaron River Reservoir.geojson");
        Debug.Assert(SmallPolygonData.Length > 10); // ensure we have the right polygon

        LargePolygon = DataHelpers.LoadFirstPolygonFromTestData("Natural Earth/ne_110m_land.shp");
        LargeMultiPolygon = LargePolygon.AsMultiPolygon();
        LargePolygonData = DataHelpers.LoadFirstPolygonCoordsFromTestData("Natural Earth/ne_110m_land.shp");
        Debug.Assert(LargePolygonData.Length > 1000); // ensure we have the right polygon
    }
}