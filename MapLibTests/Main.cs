using BenchmarkDotNet.Running;
using Microsoft.VisualStudio.TestPlatform.TestHost;

// This is strictly to run launch the benchmark runner
// in a convenient way as a console app.

public class MapLibBenchmarks
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}


