using MapLib;
using MapLib.GdalSupport;
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
}
