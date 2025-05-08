namespace MapLib.ColorSpace;

/// <summary>
/// Color conversion and correction extension methods.
/// </summary>
/// <remarks>
/// Adapted from
/// https://en.wikipedia.org/wiki/HSL_color_space
/// https://en.wikipedia.org/wiki/HSV_color_space
/// and
/// https://gist.github.com/mjackson/5311256
/// </remarks>
public static class ColorSpaceExtensions
{
    /// <summary>Converts RGB to HSL</summary>
    /// <param name="rgb">(r,g,b) tuple, range [0,1]</param>
    /// <returns>(h,s,l) tuple, range [0,1]</returns>
    public static (float h, float s, float l) RgbToHsl(this (float r, float g, float b) rgb)
    {
        float max = Max(rgb.r, rgb.g, rgb.b);
        float min = Min(rgb.r, rgb.g, rgb.b);
        float l = (max + min) / 2f;
        float h, s;

        if (max == min)
        {
            h = s = 0; // achromatic
        }
        else
        {
            float diff = max - min;
            s = l > 0.5f ? diff / (2f - max - min) : diff / (max + min);
            if (max == rgb.r)
                h = (rgb.g - rgb.b) / diff + (rgb.g < rgb.b ? 6f : 0);
            else if (max == rgb.g)
                h = (rgb.b - rgb.r) / diff + 2f;
            else
                h = (rgb.r - rgb.g) / diff + 4f;
            h /= 6f;
        }
        return (h, s, l);
    }

    /// <summary>Converts HSL to RGB</summary>
    /// <param name="hsl">(h,s,l) tuple, range [0,1]</param>
    /// <returns>(r,g,b) tuple, range [0,1]</returns>
    public static (float r, float g, float b) HslToRgb(this (float h, float s, float l) hsl)
    {
        float r, g, b;

        if (hsl.s == 0)
        {
            r = g = b = hsl.l; // achromatic
        }
        else
        {
            float Hue2Rgb(float p, float q, float t)
            {
                if (t < 0f) t += 1f;
                if (t > 1f) t -= 1f;
                if (t < 1f / 6f) return p + (q - p) * 6f * t;
                if (t < 1f / 2f) return q;
                if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
                return p;
            }
            var q = hsl.l < 0.5f ? hsl.l * (1f + hsl.s) : hsl.l + hsl.s - hsl.l * hsl.s;
            var p = 2f * hsl.l - q;
            r = Hue2Rgb(p, q, hsl.h + 1f / 3f);
            g = Hue2Rgb(p, q, hsl.h);
            b = Hue2Rgb(p, q, hsl.h - 1f / 3f);
        }
        return (r, g, b);
    }

    /// <summary>Converts RGB to HSV</summary>
    /// <param name="rgb">(r,g,b) tuple, range [0,1]</param>
    /// <returns>(h,s,v) tuple, range [0,1]</returns>
    public static (float h, float s, float v) RgbToHsv(this (float r, float g, float b) rgb)
    {
        float max = Max(rgb.r, rgb.g, rgb.b);
        float min = Min(rgb.r, rgb.g, rgb.b);
        float h, s, v = max;

        float diff = max - min;
        s = max == 0 ? 0 : diff / max;

        if (max == min)
        {
            h = 0; // achromatic
        }
        else
        {
            if (max == rgb.r) h = (rgb.g - rgb.b) / diff + (rgb.g < rgb.b ? 6f : 0);
            else if (max == rgb.g) h = (rgb.b - rgb.r) / diff + 2f;
            else h = (rgb.r - rgb.g) / diff + 4f;
            h /= 6f;
        }
        return (h, s, v);
    }

    /// <summary>Converts HSV to RGB</summary>
    /// <param name="hsv">(h,s,v) tuple, range [0,1]</param>
    /// <returns>(r,g,b) tuple, range [0,1]</returns>
    public static (float r, float g, float b) HsvToRgb(this (float h, float s, float v) hsv)
    {
        float r = 0, g = 0, b = 0;

        int i = (int)Math.Floor(hsv.h * 6);
        float f = hsv.h * 6 - i;
        float p = hsv.v * (1 - hsv.s);
        float q = hsv.v * (1 - f * hsv.s);
        float t = hsv.v * (1 - (1 - f) * hsv.s);

        switch (i % 6)
        {
            case 0: r = hsv.v; g = t; b = p; break;
            case 1: r = q; g = hsv.v; b = p; break;
            case 2: r = p; g = hsv.v; b = t; break;
            case 3: r = p; g = q; b = hsv.v; break;
            case 4: r = t; g = p; b = hsv.v; break;
            case 5: r = hsv.v; g = p; b = q; break;
        }
        return (r, g, b);
    }

    #region Helpers

    public static float Max(float r, float g, float b)
        => Math.Max(r, Math.Max(g, b));

    public static float Min(float r, float g, float b)
        => Math.Min(r, Math.Min(g, b));

    #endregion
}
