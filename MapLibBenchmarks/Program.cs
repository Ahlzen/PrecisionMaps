using System;
using BenchmarkDotNet.Running;

namespace MapLib.Benchmarks;

internal class Program
{
    static void Main(string[] args)
    {
        //BenchmarkDotNet.Reports.Summary[] summary =
        //    BenchmarkRunner.Run(typeof(Program).Assembly);
        //Console.WriteLine(summary);
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
