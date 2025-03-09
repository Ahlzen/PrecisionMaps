using System.Drawing;
using System.Text.RegularExpressions;

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

    /// <summary>
    /// Parses a hex color into a Color structure.
    /// </summary>
    /// <remarks>
    /// Implements the CSS spec, i.e. 3/4/6/8-digit formats:
    ///  #RGB
    ///  #RGBA
    ///  #RRGGBB
    ///  #RRGGBBAA
    /// </remarks>
    /// <exception cref="FormatException">
    /// Thrown if specified string is not a valid hex code.
    /// </exception>
    public static Color FromHex(string hexString)
    {
        var match = HexColorRegex.Match(hexString.Trim());
        if (match.Success)
        {
            const System.Globalization.NumberStyles hex = System.Globalization.NumberStyles.HexNumber;
            string hexDigits = match.Groups[1].Value;
            switch (hexDigits.Length)
            {
                case 3:
                    return Color.FromArgb(
                        0x11 * int.Parse(hexDigits.Substring(0, 1), hex),
                        0x11 * int.Parse(hexDigits.Substring(1, 1), hex),
                        0x11 * int.Parse(hexDigits.Substring(2, 1), hex));
                case 4:
                    return Color.FromArgb(
                        0x11 * int.Parse(hexDigits.Substring(3, 1), hex),
                        0x11 * int.Parse(hexDigits.Substring(0, 1), hex),
                        0x11 * int.Parse(hexDigits.Substring(1, 1), hex),
                        0x11 * int.Parse(hexDigits.Substring(2, 1), hex));
                case 6:
                    return Color.FromArgb(
                        int.Parse(hexDigits.Substring(0, 2), hex),
                        int.Parse(hexDigits.Substring(2, 2), hex),
                        int.Parse(hexDigits.Substring(4, 2), hex));
                case 8:
                    return Color.FromArgb(
                        int.Parse(hexDigits.Substring(6, 2), hex),
                        int.Parse(hexDigits.Substring(0, 2), hex),
                        int.Parse(hexDigits.Substring(2, 2), hex),
                        int.Parse(hexDigits.Substring(4, 2), hex));
            }
        }
        throw new FormatException($"Not a valid hex color: \"{hexString}\"");
    }

    private static Regex HexColorRegex = new(
        "#([0-9a-fA-F]{3,8})", RegexOptions.Compiled);
}
