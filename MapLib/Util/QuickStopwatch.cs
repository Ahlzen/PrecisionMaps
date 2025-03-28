using System.Diagnostics;

namespace MapLib.Util;

/// <summary>
/// Simplified stopwatch that is automatically started
/// on construction and stopped with a debug message
/// when disposed.
/// </summary>
public class QuickStopwatch : IDisposable
{
    private readonly Stopwatch _sw;
    private readonly string? _name;

    public QuickStopwatch(string? name = null)
    {
        _name = name;
        _sw = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _sw.Stop();
        Console.WriteLine($"{_name ?? "Task"} finished in {_sw.Elapsed}.");
    }
}
