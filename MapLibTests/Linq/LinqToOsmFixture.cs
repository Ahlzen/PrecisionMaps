using MapLib.Linq;
using MapLib.Tests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[TestFixture]
public class LinqToOsmFixture : BaseFixture
{
    [Test]
    public void TestQueryOsm()
    {
        var provider = new OsmQueryProvider();

        // Query 1 - Filter by type and tags
        List<Point> nodes = new Osm<Point>(provider)
            .OfType<Point>()
            .Where(p => p.Tags.Contains(new KeyValuePair<string, string>("highway", "bus_stop")))
            .ToList();

        // Query 2 - Filter by type and lon/lat
        List<Point> nodes2 = new Osm<Point>(provider)
            .OfType<Point>()
            .Where(p =>
                p.Coord.Y >= 59.9 &&
                p.Coord.Y <= 60.0 &&
                p.Coord.X >= 10.7 &&
                p.Coord.X <= 10.8)
            .ToList();

        // Query 3 - Filter by type, tags and lon/lat
        List<Point> nodes3 = new Osm<Point>(provider)
            .OfType<Point>()
            .Where(p =>
                p.Tags.Contains(new KeyValuePair<string, string>("highway", "bus_stop")) &&
                p.Coord.Y >= 59.9 &&
                p.Coord.Y <= 60.0 &&
                p.Coord.X >= 10.7 &&
                p.Coord.X <= 10.8)
            .ToList();
    }
}