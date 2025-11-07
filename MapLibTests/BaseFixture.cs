using MapLib.DataSources.Raster;
using MapLib.GdalSupport;
using MapLib.Util;
using System.Drawing;
using System.IO;

namespace MapLib.Tests;

[TestFixture]
public abstract class BaseFixture
{
    protected static readonly Polygon TestPolygon1 =
        new Polygon([(1, 1), (8, -2), (7, 5), (6, 2), (5, 3), (1, 1)], null);

    protected static readonly Line TestLine1 =
        new Line([(1, 1), (8, -2), (7, 5), (6, 2), (5, 3)], null);

    protected static readonly Line TestLine2 =
        new Line([(1, 1), (5, 1), (7, 5)], null);

    // squiggly line (e.g. for testing offsetting)
    protected static readonly Line TestLine3 =
        new Line([(1, 1), (5, 3), (6, 2), (7, 5), (8, -2)], null);

    public static string TestDataPath => FileSystemHelpers.TestDataPath;


    // Approximate bounds. For testing only.

    protected static readonly Bounds MassachusettsBounds =
        new(-73.30, -69.56, 41.14, 42.53);

    protected static readonly Bounds UnitedKingdomBounds =
        new(-9.23, 2.59, 49.47, 61.19);


    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        GdalUtils.EnsureInitialized();

        // Make sure we have all data required for the tests
        TestDataManager.EnsureTestDataReady(Console.Out);
    }

    protected void SaveTempBitmap(Bitmap bitmap, string? prefix = null, string extension = ".png")
    {
        string filename = FileSystemHelpers.GetTempOutputFileName(
            extension, prefix);
        bitmap.Save(filename);
    }

    protected async Task<SingleBandRasterData> GetTestDemData()
    {
        // Get 3DEP DEM data
        Usgs3depDataSource source = new(scaleFactor: 0.25);
        RasterData data = await source.GetData(MassachusettsBounds);
        SingleBandRasterData? demData = data as SingleBandRasterData;
        Assert.That(demData, Is.Not.Null);
        return demData!;
    }

    internal static ImageRasterData GetTestImage()
    {
        string bitmapPath = Path.Join(TestDataPath, "Misc", "me.jpg");
        using Bitmap bitmap = (Bitmap)Bitmap.FromFile(bitmapPath);
        return new ImageRasterData(Srs.Wgs84, Bounds.GlobalWgs84, bitmap);
    }

    internal static SingleBandRasterData GetSingleBandTestImage()
        => GetTestImage().ToSingleBandRasterData();
}
