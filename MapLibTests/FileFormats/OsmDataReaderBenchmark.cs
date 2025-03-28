using BenchmarkDotNet.Attributes;
using MapLib.FileFormats.Vector;
using System.IO;

namespace MapLib.Tests.FileFormats;

public class OsmDataReaderBenchmark : BaseBenchmark
{
    public string BasePath = Path.Join(
        BenchmarkDataHelpers.TestDataPath, "osm-xml");

    [Benchmark]
    public VectorData ReadOsm_OsmDataReader()
    {
        string filename = "Weymouth Detail.osm";
        OsmDataReader reader = new();
        VectorData data = reader.ReadFile(
            Path.Join(BasePath, filename));
        Console.WriteLine($"{filename}: Feature count: {data.Count}");
        return data;
    }
}
