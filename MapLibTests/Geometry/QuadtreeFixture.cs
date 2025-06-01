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
        ObjectPlacementManager placementManager = new();
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
            Bounds? overlapOpm = placementManager.GetOverlappingItem(item);
            Bounds? overlapQtree = quadtree.GetOverlappingItem(item);

            // TEST CODE
            Bounds existingItem = new Bounds(727, 750, 621, 636);
            bool qtreeHasExistingItem = quadtree.Contains(existingItem);
            QuadtreeNode? containingNode = quadtree.GetQuadtreeContaining(existingItem);

            bool addedOpm = placementManager.TryAdd([item]) != null;
            bool addedQtree = quadtree.AddIfNotOverlapping(item);

            // Check that item was either added to both or neither.
            // (i.e. the quadtree and ObjectPlacementManager agree).
            if (addedQtree)
                Assert.That(quadtree.Contains(item));
            Assert.That(placementManager.Count == quadtree.Count);
            Assert.That(addedOpm == addedQtree);
            
            if (!addedOpm)
                overlapCount++;
        }

        Console.WriteLine($"{ObjectCount} total objects, {overlapCount} overlaps.");
    }
}
