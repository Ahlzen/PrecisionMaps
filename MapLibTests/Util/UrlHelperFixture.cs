using MapLib.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Tests.Util;

[TestFixture]
internal class UrlHelperFixture
{
    [Test]
    public void TestGetFilenameFromUrl()
    {
        string url1 = "https://prd-tnm.s3.amazonaws.com/index.html?prefix=StagedProducts/";
        Assert.That(UrlHelper.GetFilenameFromUrl(url1),
            Is.EqualTo("index.html"));
    }
}
