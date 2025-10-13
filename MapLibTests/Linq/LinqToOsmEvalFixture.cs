using MapLib.Linq;

namespace MapLib.Tests.Linq;

[TestFixture]
public class LinqToOsmEvalFixture : BaseFixture
{
    private OsmQueryProvider _provider = null!; // inialized in SetUp

    [SetUp] public void SetUp() {
        _provider = new OsmQueryProvider();
        _provider.EvaluateOnly = true;
    }

    
    [Test]
    public void TestDictionaryEquality()
    {
        // Just verifying that dictionary equality works
        // as we expect it to...

        Dictionary<string, string>
            dict1 = new() { { "key1", "val1" }, { "key2", "val2" } },
            dict2 = new() { { "key1", "val1" }, { "key2", "val2" } },
            dict3 = new() { { "key1", "val3" }, { "key2", "val4" } };
        
        Assert.That(dict1, Is.EqualTo(dict2));
        //Assert.That(dict1.GetHashCode(),
        //    Is.EqualTo(dict2.GetHashCode())); // NOT true
        Assert.That(OsmExpressionVisitor.CalcDictHash(dict1),
            Is.EqualTo(OsmExpressionVisitor.CalcDictHash(dict2)));

        Assert.That(dict1, Is.Not.EqualTo(dict3));
        //Assert.That(dict1.GetHashCode(),
        //    Is.Not.EqualTo(dict3.GetHashCode())); // NOT true
        Assert.That(OsmExpressionVisitor.CalcDictHash(dict1),
            Is.Not.EqualTo(OsmExpressionVisitor.CalcDictHash(dict3)));
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

    // Supported
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

    // In progress (should be union, not intersection)
    [Test] public void TestEval_FilterNodesByMultipleTags_IndexerEqual2() {
        List<Point> nodes = new Osm<Point>(_provider)
            .OfType<Point>()
            .Where(p => new[] {"bus_stop", "crossing"}.Contains(p["highway"]))
            .ToList();
    }

    // In progress (should be union, not intersection)
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