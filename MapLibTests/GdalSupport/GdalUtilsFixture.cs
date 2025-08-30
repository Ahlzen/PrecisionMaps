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
    public static IEnumerable<Srs> ExampleSrs
    {
        get
        {
            yield return Srs.Wgs84;
            yield return Srs.WebMercator;
            yield return Srs.Nad83;
            yield return Srs.VanDerGrinten;
            yield return Srs.Robinson;
        }
    }

    [Test]
    [TestCaseSource(nameof(ExampleSrs))]
    public void TestInitializeSrs(Srs srs)
    {
        Assert.That(srs, Is.Not.Null);
        Assert.That(srs.BoundsLatLon, Is.Not.Null);

        Trace.WriteLine(srs.Name);
        Trace.Indent();
        Trace.WriteLine(srs.BoundsLatLon);
        Trace.Unindent();
    }
}
