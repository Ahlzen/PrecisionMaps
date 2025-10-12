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


    ///// Filter by tags

    // Supported
    [Test] public void TestEval_FilterNodesByTags_New() =>
        new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => p.Tags.Contains(
                new KeyValuePair<string, string>("highway", "bus_stop")))
            .ToList();

    // Supported
    [Test] public void TestEval_FilterNodesByTags_IndexerEqual() =>
        new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => p["highway"] == "bus_stop")
            .ToList();

    [Test] public void TestEval_FilterNodesByTags_IndexerEqual_Union() =>
        new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p =>
                p["highway"] == "bus_stop" ||
                p["highway"] == "crossing")
            .ToList();

    [Test] public void TestEval_FilterNodesByTags_IndexerEqual_Intersection() =>
        new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p =>
                p["highway"] == "bus_stop" &&
                p["shelter"] == "yes")
            .ToList();

    [Test] public void TestEval_FilterNodesByTags_IndexerNotEqual_Intersection() =>
        new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p =>
                p["highway"] == "bus_stop" &&
                p["shelter"] != "no")
            .ToList();

    [Test] public void TestEval_FilterNodesByMultipleTags_IndexerEqual2() {
        List<Point> nodes = new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => new[] {"bus_stop", "crossing"}.Contains(p["highway"]))
            .ToList();
    }

    [Test] public void TestEval_FilterNodesByMultipleTags_IndexerEqual() {
        var highwayPoints = new[] { "bus_stop", "crossing" };
        List<Point> nodes = new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => highwayPoints.Contains(p["highway"]))
            .ToList();
    }

    // Supported
    [Test] public void TestEval_FilterNodesByTags_Object() {
        KeyValuePair<string, string> tag1b = new("highway", "bus_stop");
        List<Point> nodes = new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => p.Tags.Contains(tag1b))
            .ToList();
    }

    // Supported
    [Test] public void TestEval_FilterNodesByTags_Key() =>
        new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => p.HasTag("highway"))
            .ToList();


    ///// Filter by area

    // Supported
    [Test] public void TestEval_FilterNodesByLonLat_Constant() =>
        new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p =>
                p.Coord.Y >= 59.9 &&
                p.Coord.Y <= 60.0 &&
                p.Coord.X >= 10.7 &&
                p.Coord.X <= 10.8)
            .ToList();

    // Supported
    [Test] public void TestEval_FilterNodesByLonLat_Expression() {
        Bounds bounds = new(10.7, 10.8, 59.9, 60.0);
        List<Point> nodes = new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p =>
                p.Coord.Y >= bounds.XMin &&
                p.Coord.Y <= bounds.XMax &&
                p.Coord.X >= bounds.YMin &&
                p.Coord.X <= bounds.YMax)
            .ToList();
    }

    // Supported
    [Test] public void TestEval_FilterNodesByLonLat_WithinBounds() {
        Bounds bounds = new(10.7, 10.8, 59.9, 60.0);
        List<Point> nodes = new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p =>
                p.Coord.Y >= bounds.XMin &&
                p.Coord.Y <= bounds.XMax &&
                p.Coord.X >= bounds.YMin &&
                p.Coord.X <= bounds.YMax)
            .ToList();
    }

    // Supported
    [Test] public void TestEval_FilterNodesByIsWithin() {
        Bounds bounds = new(10.7, 10.8, 59.9, 60.0);
        List<Point> nodes = new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => p.IsWithin(bounds))
            .ToList();
    }

    // Supported
    [Test] public void TestEval_FilterNodesByIsWithin_New() =>
        new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => p.IsWithin(new(10.7, 10.8, 59.9, 60.0)))
            .ToList();


    ///// Combinations

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


}