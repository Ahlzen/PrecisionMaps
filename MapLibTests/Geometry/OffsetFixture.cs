using System.Diagnostics;

namespace MapLib.Tests.Geometry;

[TestFixture]
public class OffsetFixture : BaseFixture
{
    [Test]
    public void TestOffset_Outward()
    {
        MultiPolygon offset1 = TestPolygon1.Offset(0.2);
        MultiPolygon offset2 = offset1.Offset(0.4);
        MultiPolygon offset3 = offset2.Offset(0.8);
        Visualizer.RenderAndShow(800, 500, TestPolygon1,
            offset1, offset2, offset3);

        Debug.WriteLine($"Point counts");
        Debug.WriteLine($"TestPolygon1: {TestPolygon1.Count}");
        Debug.WriteLine($"offset1: {offset1.Sum(p => p.Length)}");
        Debug.WriteLine($"offset2: {offset2.Sum(p => p.Length)}");
        Debug.WriteLine($"offset3: {offset3.Sum(p => p.Length)}");
    }

    [Test]
    public void TestOffset_Inward()
    {
        MultiPolygon? offset1 = TestPolygon1.Offset(-0.2);
        MultiPolygon? offset2 = offset1?.Offset(-0.4);
        MultiPolygon? offset3 = offset2?.Offset(-0.85);
        // Offset past when there's no area left:
        MultiPolygon? offset4 = offset3?.Offset(-0.5);
        Assert.That(offset1, Is.Not.Null);
        Assert.That(offset2, Is.Not.Null);
        Assert.That(offset3, Is.Not.Null);
        Assert.That(offset4, Is.Null);

        Visualizer.RenderAndShow(800, 500, TestPolygon1,
            offset1!, offset2!, offset3!);

        Debug.WriteLine($"Point counts");
        Debug.WriteLine($"TestPolygon1: {TestPolygon1.Count}");
        Debug.WriteLine($"offset1: {offset1!.Sum(p => p.Length)}");
        Debug.WriteLine($"offset2: {offset2!.Sum(p => p.Length)}");
        Debug.WriteLine($"offset3: {offset3!.Sum(p => p.Length)}");
    }
}
