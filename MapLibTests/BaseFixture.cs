using MapLib.GdalSupport;

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

    //internal static string GetTempFileName(string extension)
    //{
    //    return TrimEnd(Path.GetTempFileName(), ".tmp") + extension;
    //}

    //internal static string TrimEnd(string source, string value)
    //{
    //    if (!source.EndsWith(value))
    //        return source;
    //    return source.Remove(source.LastIndexOf(value));
    //}

    //internal static void ShowFile(string filename)
    //{
    //    Process.Start(new ProcessStartInfo
    //    {
    //        FileName = filename,
    //        UseShellExecute = true
    //    });
    //    Debug.WriteLine(filename);
    //}
}
