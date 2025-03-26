using MapLib.GdalSupport;

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


    public static string TestDataPath =>
        "../../../../TestData";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        GdalUtils.Initialize();

        // Make sure we have all data required for the tests
        TestDataManager.EnsureTestDataReady(Console.Out);
    }
}
