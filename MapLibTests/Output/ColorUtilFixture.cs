using System.Drawing;
using MapLib.Output;

namespace MapLib.Tests.Output;

[TestFixture]
public class ColorUtilFixture : BaseFixture
{
    [Test]
    public void TestFromHex()
    {
        // Three-value syntax: #RGB
        Color c1 = ColorUtil.FromHex("#734");
        Assert.That(c1.R == 0x77);
        Assert.That(c1.G == 0x33);
        Assert.That(c1.B == 0x44);
        Assert.That(c1.A == 0xff);

        // Four-value syntax: #RGBA
        Color c2 = ColorUtil.FromHex("#ABCD");
        Assert.That(c2.R == 0xaa);
        Assert.That(c2.G == 0xbb);
        Assert.That(c2.B == 0xcc);
        Assert.That(c2.A == 0xdd);

        // Six-value syntax: #RRGGBB
        Color c3 = ColorUtil.FromHex("#c82365");
        Assert.That(c3.R == 0xc8);
        Assert.That(c3.G == 0x23);
        Assert.That(c3.B == 0x65);
        Assert.That(c3.A == 0xff);

        // Eight-value syntax: #RRGGBBAA
        Color c4 = ColorUtil.FromHex("#c8236544");
        Assert.That(c4.R == 0xc8);
        Assert.That(c4.G == 0x23);
        Assert.That(c4.B == 0x65);
        Assert.That(c4.A == 0x44);

        // Invalid values
        Assert.Throws<FormatException>(() => ColorUtil.FromHex("123")); // requires #
        Assert.Throws<FormatException>(() => ColorUtil.FromHex("#ffee8")); // no 5-valued format
        Assert.Throws<FormatException>(() => ColorUtil.FromHex(""));
    }
}
