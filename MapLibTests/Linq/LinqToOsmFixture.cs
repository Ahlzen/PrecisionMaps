using MapLib.Linq;

namespace MapLib.Tests.Linq;

[TestFixture]
public class LinqToOsmFixture : BaseFixture
{
    private OsmQueryProvider _provider = null!; // inialized in SetUp

    /// <summary>
    /// Small test area for querying. I believe this is part of Oslo, Norway.
    /// </summary>
    protected Bounds TestArea = new(10.7, 10.8, 59.9, 60.0);

    [SetUp]
    public void SetUp() {
        _provider = new OsmQueryProvider();
    }


    [Test]
    public void TestQueryPoints_BusStops()
    {
        List<Point> busStops = new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => p.IsWithin(TestArea))
            .Where(p => p["highway"] == "bus_stop")
            .ToList();
        PrintCount(busStops);
    }

    private static void PrintCount<T>(List<T> items) =>
        Console.Write($"Count ({typeof(T)}: {items.Count})");
}
