using BenchmarkDotNet.Running;
using MapLib.GdalSupport;
using System;

namespace MapLib.Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkDotNet.Reports.Summary[] summary =
            BenchmarkRunner.Run(typeof(Program).Assembly);
        Console.WriteLine(summary);
    }
}
