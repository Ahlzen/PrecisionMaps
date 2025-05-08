namespace MapLib.ColorSpace;

public struct Rgba
{
    public float R; // red [0, 1]
    public float G; // green [0, 1]
    public float B; // blue [0, 1]
    public float A; // opacity [0, 1]

    public Rgba()
    {
        R = G = B = 0; A = 1f;
    }

    public Rgba(float r, float g, float b, float a = 1.0f)
    {
        R = r; G = g; B = b; A = a;
    }


    public static Rgba operator *(Rgba x, Rgba y) =>
        new Rgba(x.R * y.R, x.G * y.G, x.B * y.B, x.A * y.A);

    public static Rgba Lerp(Rgba from, Rgba to, float distance) =>
        new Rgba(
            from.R + distance * (to.R - from.R),
            from.G + distance * (to.G - from.G),
            from.B + distance * (to.B - from.B),
            from.A + distance * (to.A - from.A));
}