using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Tests.Render;

[TestFixture]
public class HyphenatorFixture : BaseFixture
{
    [Test]
    [Explicit]
    public void TestHyphenate()
    {
        var hyphenator = new MapLib.Render.Rewriting.LabelHyphenator();
        var input = "Let's try some hyphenation of random text.";
        var result = hyphenator.Hyphenate(input);
        Console.WriteLine(result);
    }
}
