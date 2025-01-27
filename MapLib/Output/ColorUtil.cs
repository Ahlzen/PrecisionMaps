using System.Drawing;

namespace MapLib.Output;

public static class ColorUtil
{
    public static string ToHexCode(this Color c)
    {
        if (c.A == 255)
        {
            // no opacity, use #rrggbb format
            return "#" + c.R.ToString("x2") + c.G.ToString("x2") + c.B.ToString("x2");
        }
        else
        {
            // with opacity, use #rrggbbaa format
            return "#" + c.R.ToString("x2") + c.G.ToString("x2") + c.B.ToString("x2") + c.A.ToString("x2");
        }
    }
}
