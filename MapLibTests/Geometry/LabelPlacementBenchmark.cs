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

    [Params(4, 16, 32, 64, 256)]
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
        ObjectOverlapManager placementManager = new();
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
        QuadtreeNode quadtree = new(OverallBounds, QuadTreeMaxItemsPerNode);
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
| Method                             | ObjectCount | QuadTreeMaxItemsPerNode | Mean           | Error         | StdDev        |
|----------------------------------- |------------ |------------------------ |---------------:|--------------:|--------------:|
| PlaceLabels_ObjectPlacementManager | 100         | 4                       |      32.142 us |     0.3120 us |     0.2918 us |
| PlaceLabels_QuadTree               | 100         | 4                       |      13.700 us |     0.1192 us |     0.1115 us |
| PlaceLabels_ObjectPlacementManager | 100         | 16                      |      31.704 us |     0.3745 us |     0.3320 us |
| PlaceLabels_QuadTree               | 100         | 16                      |      12.209 us |     0.2374 us |     0.3169 us |
| PlaceLabels_ObjectPlacementManager | 100         | 32                      |      31.249 us |     0.6117 us |     0.6282 us |
| PlaceLabels_QuadTree               | 100         | 32                      |       9.045 us |     0.1523 us |     0.1350 us |
| PlaceLabels_ObjectPlacementManager | 100         | 64                      |      31.064 us |     0.4140 us |     0.3872 us |
| PlaceLabels_QuadTree               | 100         | 64                      |      13.524 us |     0.1317 us |     0.1168 us |
| PlaceLabels_ObjectPlacementManager | 100         | 256                     |      30.998 us |     0.1157 us |     0.0966 us |
| PlaceLabels_QuadTree               | 100         | 256                     |       8.772 us |     0.0775 us |     0.0687 us |
| PlaceLabels_ObjectPlacementManager | 1000        | 4                       |   2,379.216 us |     6.2599 us |     4.8873 us |
| PlaceLabels_QuadTree               | 1000        | 4                       |     368.386 us |     4.5383 us |     4.0231 us |
| PlaceLabels_ObjectPlacementManager | 1000        | 16                      |   2,345.893 us |    16.7155 us |    14.8179 us |
| PlaceLabels_QuadTree               | 1000        | 16                      |     328.258 us |     4.3945 us |     4.1106 us |
| PlaceLabels_ObjectPlacementManager | 1000        | 32                      |   2,330.282 us |    30.9301 us |    28.9320 us |
| PlaceLabels_QuadTree               | 1000        | 32                      |     349.273 us |     1.3456 us |     1.2587 us |
| PlaceLabels_ObjectPlacementManager | 1000        | 64                      |   2,392.134 us |    26.8863 us |    25.1495 us |
| PlaceLabels_QuadTree               | 1000        | 64                      |     351.337 us |     2.3255 us |     2.1753 us |
| PlaceLabels_ObjectPlacementManager | 1000        | 256                     |   2,369.220 us |    10.1127 us |     7.8953 us |
| PlaceLabels_QuadTree               | 1000        | 256                     |     597.709 us |     2.5969 us |     2.0275 us |
| PlaceLabels_ObjectPlacementManager | 10000       | 4                       |  52,387.317 us |   608.7829 us |   569.4559 us |
| PlaceLabels_QuadTree               | 10000       | 4                       |   4,469.100 us |    26.0480 us |    21.7512 us |
| PlaceLabels_ObjectPlacementManager | 10000       | 16                      |  51,979.057 us |   682.5020 us |   638.4128 us |
| PlaceLabels_QuadTree               | 10000       | 16                      |   4,420.094 us |    25.7110 us |    21.4699 us |
| PlaceLabels_ObjectPlacementManager | 10000       | 32                      |  51,356.964 us |   251.1667 us |   222.6527 us |
| PlaceLabels_QuadTree               | 10000       | 32                      |   4,623.323 us |    38.8024 us |    34.3973 us |
| PlaceLabels_ObjectPlacementManager | 10000       | 64                      |  51,833.886 us |   437.5756 us |   387.8994 us |
| PlaceLabels_QuadTree               | 10000       | 64                      |   4,677.096 us |    54.7515 us |    51.2146 us |
| PlaceLabels_ObjectPlacementManager | 10000       | 256                     |  52,255.805 us |   401.2329 us |   335.0477 us |
| PlaceLabels_QuadTree               | 10000       | 256                     |   6,156.808 us |    52.1323 us |    43.5329 us |
| PlaceLabels_ObjectPlacementManager | 100000      | 4                       | 595,473.340 us | 7,164.8975 us | 6,702.0496 us |
| PlaceLabels_QuadTree               | 100000      | 4                       |  59,629.655 us |   705.6188 us |   625.5126 us |
| PlaceLabels_ObjectPlacementManager | 100000      | 16                      | 598,935.446 us | 6,694.7818 us | 5,590.4460 us |
| PlaceLabels_QuadTree               | 100000      | 16                      |  59,673.222 us |   257.3601 us |   228.1430 us |
| PlaceLabels_ObjectPlacementManager | 100000      | 32                      | 591,503.538 us | 1,582.0849 us | 1,321.1126 us |
| PlaceLabels_QuadTree               | 100000      | 32                      |  60,956.210 us |   381.0463 us |   337.7877 us |
| PlaceLabels_ObjectPlacementManager | 100000      | 64                      | 585,709.940 us | 9,089.6825 us | 8,502.4947 us |
| PlaceLabels_QuadTree               | 100000      | 64                      |  58,753.802 us |   625.9187 us |   554.8606 us |
| PlaceLabels_ObjectPlacementManager | 100000      | 256                     | 588,842.058 us | 2,203.8328 us | 1,720.6077 us |
| PlaceLabels_QuadTree               | 100000      | 256                     |  68,792.807 us |   441.0975 us |   344.3799 us |
*/