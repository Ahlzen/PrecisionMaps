using MapLib.Linq;
using MapLib.Tests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Tests.Linq;

[TestFixture]
public class LinqToOsmEvalFixture : BaseFixture
{
    private OsmQueryProvider _provider = null!; // inialized in SetUp

    [SetUp] public void SetUp() {
        _provider = new OsmQueryProvider();
        _provider.EvaluateOnly = true;
    }

    [Test] public void TestEval_FilterNodesByTags_New() =>
        new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => p.Tags.Contains(new KeyValuePair<string, string>("highway", "bus_stop")))
            .ToList();

    // TODO: implement support for this case
    [Test] public void TestEval_FilterNodesByTags_Object() {
        KeyValuePair<string, string> tag1b = new("highway", "bus_stop");
        List<Point> nodes1b = new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => p.Tags.Contains(tag1b))
            .ToList();
    }

    [Test] public void TestEval_FilterNodesByLonLat_Constant() =>
        new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p =>
                p.Coord.Y >= 59.9 &&
                p.Coord.Y <= 60.0 &&
                p.Coord.X >= 10.7 &&
                p.Coord.X <= 10.8)
            .ToList();

    // TODO: implement support for this case
    [Test] public void TestEval_FilterNodesByLonLat_Expression() {
        Bounds bounds = new(10.7, 10.8, 59.9, 60.0);
        List<Point> nodes2b = new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p =>
                p.Coord.Y >= bounds.XMin &&
                p.Coord.Y <= bounds.XMax &&
                p.Coord.X >= bounds.YMin &&
                p.Coord.X <= bounds.YMax)
            .ToList();
    }

    // TODO: implement support for this case
    [Test] public void TestEval_FilterNodesByLonLat_WithinBounds() {
        Bounds bounds = new(10.7, 10.8, 59.9, 60.0);
        List<Point> nodes2b = new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p =>
                p.Coord.Y >= bounds.XMin &&
                p.Coord.Y <= bounds.XMax &&
                p.Coord.X >= bounds.YMin &&
                p.Coord.X <= bounds.YMax)
            .ToList();
    }

    [Test] public void TestEval_FilterNodesByLonLatAndTags() =>
        new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p =>
                p.Tags.Contains(new KeyValuePair<string, string>("highway", "bus_stop")) &&
                p.Coord.Y >= 59.9 &&
                p.Coord.Y <= 60.0 &&
                p.Coord.X >= 10.7 &&
                p.Coord.X <= 10.8)
            .ToList();

    //[Test]
    //public void TestQueryOsm()
    //{
    //    //// Query 2 - Filter by type and lon/lat
    //    //List<Point> nodes2 = new Osm<Point>(_provider)
    //    //    .OfType<Point>()
    //    //    .Where(p =>
    //    //        p.Coord.Y >= 59.9 &&
    //    //        p.Coord.Y <= 60.0 &&
    //    //        p.Coord.X >= 10.7 &&
    //    //        p.Coord.X <= 10.8)
    //    //    .ToList();

    //    // Query 2b - Filter by type and lon/lat (expression)
    //    // TODO: Support
    //    Bounds bbox2 = new(10.7, 10.8, 59.9, 60.0);
    //    //List<Point> nodes2b = new Osm<Point>(provider)
    //    //    .OfType<Point>()
    //    //    .Where(p =>
    //    //        p.Coord.Y >= bbox2.XMin &&
    //    //        p.Coord.Y <= bbox2.XMax &&
    //    //        p.Coord.X >= bbox2.YMin &&
    //    //        p.Coord.X <= bbox2.YMax)
    //    //    .ToList();

    //    //// Query 2c - Filter by type and lon/lat (within bbox)
    //    //List<Point> nodes2c = new Osm<Point>(_provider)
    //    //    .OfType<Point>()
    //    //    .Where(p => p.Coord.IsWithin(bbox2))
    //    //    .ToList();

    //    // Query 3 - Filter by type, tags and lon/lat
    //    List<Point> nodes3 = new Osm<Point>(_provider)
    //        .OfType<Point>()
    //        .Where(p =>
    //            p.Tags.Contains(new KeyValuePair<string, string>("highway", "bus_stop")) &&
    //            p.Coord.Y >= 59.9 &&
    //            p.Coord.Y <= 60.0 &&
    //            p.Coord.X >= 10.7 &&
    //            p.Coord.X <= 10.8)
    //        .ToList();
    //}
}