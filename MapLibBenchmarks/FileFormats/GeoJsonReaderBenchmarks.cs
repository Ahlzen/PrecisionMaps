using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using MapLib.FileFormats;
using MapLib.FileFormats.Vector;

namespace MapLib.Benchmarks.FileFormats;

/// <summary>
/// Benchmarks the various classes for reading GeoJSON
/// files of different sizes and types.
/// </summary>
// TODO: Supposedly GenericTypeArguments can be supplied to a benchmark, but it's not recognized?
//[GenericTypeArguments(
//    typeof(OgrDataReader),
//    typeof(GeoJsonDataReader))]
//public class GeoJsonReaderBenchmarks<TReader> : BaseBenchmarks where TReader : IVectorFormatReader, new()
public class GeoJsonReaderBenchmarks : BaseBenchmarks
{
    [Params(
        "openlayers-line-samples.geojson",
        "openlayers-polygon-samples.geojson",
        "openlayers-vienna-streets.geojson",
        "openlayers-world-cities.geojson")]
    public string? Filename;

    public string BasePath = Path.Join(DataHelpers.TestDataPath, "GeoJSON");

    //[Benchmark]
    //public VectorData ReadGeoJson()
    //{
    //    IVectorFormatReader reader = new TReader();
    //    VectorData data = reader.ReadFile(Path.Join(BasePath, Filename));
    //    Console.WriteLine($"{Filename}: Feature count: {data.Count}");
    //    return data;
    //}

    [Benchmark]
    public VectorData ReadGeoJson_GeoJsonDataReader()
    {
        GeoJsonDataReader reader = new();
        VectorData data = reader.ReadFile(Path.Join(BasePath, Filename));
        Console.WriteLine($"{Filename}: Feature count: {data.Count}");
        return data;
    }

    [Benchmark]
    public VectorData ReadGeoJson_OgrDataReader()
    {
        OgrDataReader reader = new();
        VectorData data = reader.ReadFile(Path.Join(BasePath, Filename));
        Console.WriteLine($"{Filename}: Feature count: {data.Count}");
        return data;
    }
}
