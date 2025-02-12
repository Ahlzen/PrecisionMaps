using MapLib.GdalSupport;

namespace MapLibTests;

[TestFixture]
public abstract class BaseFixture
{
    protected static readonly Polygon TestPolygon1 =
        new Polygon([(1, 1), (8, -2), (7, 5), (6, 2), (5, 3), (1, 1)], null);

    protected static readonly Line TestLine1 =
        new Line([(1, 1), (8, -2), (7, 5), (6, 2), (5, 3)], null);

    private static readonly Line TestLine2 =
        new Line([(1, 1), (5, 1), (7, 5)], null);


    public string TestDataPath =>
        "../../../../TestData";

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        GdalUtils.Initialize();
    }
}
