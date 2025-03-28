using BenchmarkDotNet.Running;

// This is strictly to run launch the benchmark runner
// in a convenient way as a console app.

public class MapLibBenchmarks
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(
            typeof(MapLibBenchmarks).Assembly)
            .Run(args);
    }
}


