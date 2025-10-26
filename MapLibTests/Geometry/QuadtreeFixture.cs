using MapLib.Geometry.Helpers;
using Microsoft.Diagnostics.Tracing.Parsers.Symbol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Tests.Geometry;

[TestFixture]
public class QuadtreeFixture : BaseFixture
{
    private const int ObjectCount = 1000;
    private const int Width = 1000;
    private const int Height = 1000;
    private readonly Bounds overallBounds = new(0, Width, 0, Height);

    /// <summary>
    /// Compares the Quadtree overlap test to the
    /// naive (slow) reference implementation in
    /// ObjectPlacementManager.
    /// </summary>
    [Test]
    public void TestRandomBoundsOverlap()
    {
        ObjectOverlapManager placementManager = new();
        QuadtreeNode quadtree = new(overallBounds, 10);

        int overlapCount = 0;
        Random random = new();
        for (int i = 0; i < ObjectCount; i++)
        {
            // Create new item (simulate e.g. a text label)
            double xmin = Math.Floor(random.NextDouble() * Width);
            double xmax = Math.Ceiling(xmin + 10 + random.NextDouble() * 30);
            double ymin = Math.Floor(random.NextDouble() * Height);
            double ymax = Math.Ceiling(ymin + 5 + random.NextDouble() * 10);
            xmax = Math.Min(Width, xmax);
            ymax = Math.Min(Height, ymax);
            Bounds item = new(xmin, xmax, ymin, ymax);
            Assert.That(item.IsFullyWithin(overallBounds));

            // Add item
            bool addedOpm = placementManager.TryAdd([item]) != null;
            bool addedQtree = quadtree.AddIfNotOverlapping(item);

            // Check that item was either added to both or neither.
            // (i.e. the quadtree and ObjectPlacementManager agree).
            Assert.That(addedOpm == addedQtree);

            if (!addedOpm)
                overlapCount++;
        }

        Console.WriteLine($"{ObjectCount} total objects, {overlapCount} overlaps.");
    }
}
