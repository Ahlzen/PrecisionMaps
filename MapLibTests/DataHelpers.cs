using MapLib.FileFormats.Vector;
using System.IO;

namespace MapLib.Tests;

public static class BenchmarkDataHelpers
{
    // Typical Environment.CurrentDirectory under the BenchmarkRunner
    // is something like:
    // MapLibBenchmarks\bin\Release\net8.0-windows\a77ced2a-8c79-4049-b102-976c0a743555\bin\Release\net8.0-Windows7.0

    public static string TestDataPath =>
        "../../../../../../../../TestData";

    public static List<MultiPolygon> LoadMultiPolygonsFromTestData(
        string filename)
    {
        OgrDataReader reader = new OgrDataReader();
        VectorData data = reader.ReadFile(Path.Join(TestDataPath, filename));

        // Load all polygons as multipolygons
        List<MultiPolygon> multiPolygons = new();
        multiPolygons.AddRange(data.MultiPolygons);
        multiPolygons.AddRange(data.Polygons.Select(p => p.AsMultiPolygon()));

        return multiPolygons;
    }

    public static Polygon LoadFirstPolygonFromTestData(string filename)
    {
        List<MultiPolygon> MultiPolygons = LoadMultiPolygonsFromTestData(filename);
        return new Polygon(MultiPolygons[0].Coords[0], MultiPolygons[0].Tags);
    }

    public static Coord[] LoadFirstPolygonCoordsFromTestData(string filename)
    {
        List<MultiPolygon> MultiPolygons = LoadMultiPolygonsFromTestData(filename);
        return MultiPolygons[0].Coords[0];
    }
}

