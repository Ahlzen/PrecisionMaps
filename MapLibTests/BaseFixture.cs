using MapLib;
using MapLib.GdalSupport;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MapLibTests;

[TestFixture]
public abstract class BaseFixture
{
    public string TestDataPath =>
        "../../../data";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        GdalUtils.Initialize();
    }

    protected string FormatVectorDataSummary(VectorData data)
    {
        StringBuilder sb = new();
        sb.AppendLine("VectorData");
        sb.AppendLine("Bounds: " + data.Bounds.ToString());
        sb.AppendLine($"Points: {data.Points.Length}");
        sb.AppendLine($"MultiPoints: {data.MultiPoints.Length}");
        sb.AppendLine($"Lines: {data.Lines.Length}");
        sb.AppendLine($"MultiLines: {data.MultiLines.Length}");
        sb.AppendLine($"Polygons: {data.Polygons.Length}");
        sb.AppendLine($"MultiPolygons: {data.MultiPolygons.Length}");
        return sb.ToString();
    }

    internal static string GetTempFileName(string extension)
    {
        return TrimEnd(Path.GetTempFileName(), ".tmp") + extension;
    }

    internal static string TrimEnd(string source, string value)
    {
        if (!source.EndsWith(value))
            return source;
        return source.Remove(source.LastIndexOf(value));
    }

    internal static void ShowFile(string filename)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = filename,
            UseShellExecute = true
        });
        Debug.WriteLine(filename);
    }
}
