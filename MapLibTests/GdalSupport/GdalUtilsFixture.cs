using BenchmarkDotNet.Attributes;
using MapLib.GdalSupport;
using OSGeo.OSR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Tests.GdalSupport;

[TestFixture]
public class GdalUtilsFixture : BaseFixture
{
    public static IEnumerable<string> ExampleWkts
    {
        get
        {
            yield return KnownSrs.EpsgWgs84;
            yield return KnownSrs.EpsgWebMercator;
            yield return KnownSrs.EpsgNad83;
            yield return KnownSrs.WktVanDerGrinten;
            yield return KnownSrs.WktRobinson;
        }
    }


    [Test]
    [TestCaseSource(nameof(ExampleWkts))]
    public void TestInitializeSrs(string wkt)
    {
        SpatialReference spatialReference = GdalUtils.CreateSpatialReference(wkt);
        Assert.That(spatialReference, Is.Not.Null);
        Assert.That(spatialReference.GetAreaOfUse(), Is.Not.Null);

        Trace.WriteLine(spatialReference.GetName());
        Trace.Indent();
        Trace.WriteLine(GdalUtils.FormatAreaOfUse(spatialReference.GetAreaOfUse()));
        Trace.Unindent();
    }
}
