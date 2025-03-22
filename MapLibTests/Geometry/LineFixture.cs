namespace MapLib.Tests.Geometry;

[TestFixture]
[SupportedOSPlatform("windows")]
internal class LineFixture : BaseFixture
{
    // intersecting lines
    private static readonly Line l1 = new Line([(1, 1), (4, 3)], null);
    private static readonly Line l2 = new Line([(1, 2), (6, 3)], null);
    private static readonly Line l3 = new Line([(1.5, 0), (1.2, 4)], null);

    // parallel lines
    private static readonly Line l4 = new Line([(1, 1), (6, 3)], null);
    private static readonly Line l5 = new Line([(1, 2), (6, 4)], null);

    // non-parallel disjoint
    private static readonly Line l6 = new Line([(1, 3), (6, 4)], null);
    private static readonly Line l7 = new Line([(1, 1), (6, 3)], null);

    [Test]
    public void TestOffsetLine() {
        var right = TestLine3.Offset(0.5);
        var left = TestLine3.Offset(-1.0);
        Visualizer.RenderAndShow(800, 500,
            TestLine3, right, left);
    }

    [Test]
    public void TestBufferLine() {
        var buffer1 = TestLine3.Buffer(0.5);
        var buffer2 = TestLine3.Buffer(2.5);
        Visualizer.RenderAndShow(800, 500,
            TestLine3, buffer1, buffer2);
    }

    [Test]
    public void TestBufferMultiLine() {
        var ml = new MultiLine(new[] { l1, l2, l3 }, null);
        MultiPolygon buffer1 = ml.Buffer(0.1);
        MultiPolygon buffer2 = ml.Buffer(2);
        Visualizer.RenderAndShow(800, 500,
            ml, buffer1, buffer2);
    }

    [Test]
    public void TestLineSegmentInsersect_Intersecting() {
        Coord i1 = Line.LineSegmentIntersection(l1[0], l1[1], l2[0], l2[1]);
        Coord i2 = Line.LineSegmentIntersection(l1[0], l1[1], l3[0], l3[1]);
        Coord i3 = Line.LineSegmentIntersection(l2[0], l2[1], l3[0], l3[1]);
        Visualizer.RenderAndShow(800, 500, l1, l2, l3,
            (Point)i1, (Point)i2, (Point)i3);
    }

    [Test]
    public void TestLineSegmentInsersect_Parallel() {
        Coord intersection =
            Line.LineSegmentIntersection(
                l4[0], l4[1], l5[0], l5[1]);
        Assert.That(intersection, Is.EqualTo(Coord.None));
    }

    [Test]
    public void TestLineSegmentInsersect_CollinearDisjoint() {
        Coord intersection =
            Line.LineSegmentIntersection(
                l6[0], l6[1], l7[0], l7[1]);
        Assert.That(intersection, Is.EqualTo(Coord.None));
    }
}
