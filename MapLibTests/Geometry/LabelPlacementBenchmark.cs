using BenchmarkDotNet.Attributes;
using MapLib.Geometry.Helpers;

namespace MapLib.Tests.Geometry;

/// <summary>
/// Compares the ObjectPlacementManager (naive, brute-force reference
/// implementation) to the more scalable Quadtree structure for preventing
/// label overlap at various object counts.
/// 
/// Also benchmarks parameters for tuning the quadtree.
/// </summary>
public class LabelPlacementBenchmark : BaseBenchmark
{
    private const int RandomSeed = 12345;
    private readonly Random Random = new(RandomSeed);

    private List<Bounds> LabelBounds = new();

    private const int Width = 1000;
    private const int Height = 1000;
    private readonly Bounds OverallBounds = new Bounds(0, Width, 0, Height);

    [Params(100, 1000, 10000, 100000)]
    public int ObjectCount;

    [Params(4, 16, 64, 256, 1024)]
    public int QuadTreeMaxItemsPerNode;

    [GlobalSetup]
    public void Setup()
    {
        // Generate label bounds
        for (int i = 0; i < ObjectCount; i++)
        {
            // Wide bbox, e.g. a text label
            double xmin = Random.NextDouble() * Width;
            double xmax = xmin + 10 + Random.NextDouble() * 30;
            double ymin = Random.NextDouble() * Height;
            double ymax = ymin + 5 + Random.NextDouble() * 10;
            xmax = Math.Min(Width, xmax);
            ymax = Math.Min(Height, ymax);
            Bounds item = new(xmin, xmax, ymin, ymax);
            LabelBounds.Add(item);
        }
    }

    [Benchmark]
    public void PlaceLabels_ObjectPlacementManager()
    {
        ObjectPlacementManager placementManager = new();
        int overlapCount = 0;
        foreach (Bounds bounds in LabelBounds)
        {
            if (placementManager.TryAdd([bounds]) == null)
                overlapCount++;
        }
        //Console.WriteLine($"{ObjectCount} total objects, {overlapCount} overlaps.");
    }

    [Benchmark]
    public void PlaceLabels_QuadTree()
    {
        QuadtreeNode quadtree = new(QuadTreeMaxItemsPerNode, OverallBounds);
        int overlapCount = 0;
        foreach (Bounds bounds in LabelBounds)
        {
            if (!quadtree.AddIfNotOverlapping(bounds))
                overlapCount++;
        }
        //Console.WriteLine($"{ObjectCount} total objects, {overlapCount} overlaps.");
    }
}

/*
| Method                             | ObjectCount | QuadTreeMaxItemsPerNode | Mean          | Error         | StdDev        |
|----------------------------------- |------------ |------------------------ |--------------:|--------------:|--------------:|
| PlaceLabels_ObjectPlacementManager | 100         | 4                       |      31.33 us |      0.583 us |      0.545 us |
| PlaceLabels_QuadTree               | 100         | 4                       |      39.49 us |      0.351 us |      0.329 us |
| PlaceLabels_ObjectPlacementManager | 100         | 16                      |      31.20 us |      0.608 us |      0.724 us |
| PlaceLabels_QuadTree               | 100         | 16                      |      26.49 us |      0.342 us |      0.320 us |
| PlaceLabels_ObjectPlacementManager | 100         | 64                      |      29.95 us |      0.408 us |      0.361 us |
| PlaceLabels_QuadTree               | 100         | 64                      |      19.44 us |      0.236 us |      0.197 us |
| PlaceLabels_ObjectPlacementManager | 100         | 256                     |      29.06 us |      0.238 us |      0.223 us |
| PlaceLabels_QuadTree               | 100         | 256                     |      16.37 us |      0.065 us |      0.058 us |
| PlaceLabels_ObjectPlacementManager | 100         | 1024                    |      28.89 us |      0.333 us |      0.311 us |
| PlaceLabels_QuadTree               | 100         | 1024                    |      16.33 us |      0.096 us |      0.085 us |
| PlaceLabels_ObjectPlacementManager | 1000        | 4                       |   2,256.96 us |     27.620 us |     25.836 us |
| PlaceLabels_QuadTree               | 1000        | 4                       |   2,333.65 us |      9.377 us |      8.772 us |
| PlaceLabels_ObjectPlacementManager | 1000        | 16                      |   2,288.11 us |     16.616 us |     14.730 us |
| PlaceLabels_QuadTree               | 1000        | 16                      |   1,578.17 us |      9.186 us |      7.671 us |
| PlaceLabels_ObjectPlacementManager | 1000        | 64                      |   2,291.96 us |     15.654 us |     14.643 us |
| PlaceLabels_QuadTree               | 1000        | 64                      |   1,243.24 us |      5.205 us |      4.869 us |
| PlaceLabels_ObjectPlacementManager | 1000        | 256                     |   2,290.45 us |     13.283 us |     11.775 us |
| PlaceLabels_QuadTree               | 1000        | 256                     |   1,159.24 us |      8.859 us |      7.854 us |
| PlaceLabels_ObjectPlacementManager | 1000        | 1024                    |   2,304.13 us |     10.224 us |      9.063 us |
| PlaceLabels_QuadTree               | 1000        | 1024                    |   1,413.42 us |      8.602 us |      7.183 us |
| PlaceLabels_ObjectPlacementManager | 10000       | 4                       |  51,472.25 us |    587.612 us |    549.653 us |
| PlaceLabels_QuadTree               | 10000       | 4                       |  44,169.88 us |    464.728 us |    434.707 us |
| PlaceLabels_ObjectPlacementManager | 10000       | 16                      |  51,283.13 us |    254.787 us |    238.328 us |
| PlaceLabels_QuadTree               | 10000       | 16                      |  32,972.48 us |    361.403 us |    338.056 us |
| PlaceLabels_ObjectPlacementManager | 10000       | 64                      |  51,396.54 us |    338.441 us |    282.614 us |
| PlaceLabels_QuadTree               | 10000       | 64                      |  27,370.44 us |    156.634 us |    146.516 us |
| PlaceLabels_ObjectPlacementManager | 10000       | 256                     |  51,291.19 us |    353.254 us |    330.434 us |
| PlaceLabels_QuadTree               | 10000       | 256                     |  23,623.58 us |     63.318 us |     52.874 us |
| PlaceLabels_ObjectPlacementManager | 10000       | 1024                    |  50,254.25 us |    251.784 us |    223.200 us |
| PlaceLabels_QuadTree               | 10000       | 1024                    |  24,362.95 us |    136.423 us |    127.610 us |
| PlaceLabels_ObjectPlacementManager | 100000      | 4                       | 590,427.41 us |  5,374.948 us |  5,027.730 us |
| PlaceLabels_QuadTree               | 100000      | 4                       | 923,181.50 us |  5,647.586 us |  5,282.756 us |
| PlaceLabels_ObjectPlacementManager | 100000      | 16                      | 590,403.87 us |  3,163.807 us |  2,959.427 us |
| PlaceLabels_QuadTree               | 100000      | 16                      | 494,396.29 us |  2,485.814 us |  2,325.232 us |
| PlaceLabels_ObjectPlacementManager | 100000      | 64                      | 583,016.00 us | 11,567.117 us | 12,856.820 us |
| PlaceLabels_QuadTree               | 100000      | 64                      | 389,549.45 us |  7,653.953 us |  7,159.512 us |
| PlaceLabels_ObjectPlacementManager | 100000      | 256                     | 584,714.65 us | 11,548.222 us | 12,835.818 us |
| PlaceLabels_QuadTree               | 100000      | 256                     | 337,510.60 us |  6,662.067 us |  7,672.046 us |
| PlaceLabels_ObjectPlacementManager | 100000      | 1024                    | 580,811.79 us | 11,390.831 us | 11,697.545 us |
| PlaceLabels_QuadTree               | 100000      | 1024                    | 335,171.51 us |    799.996 us |    709.176 us |
*/