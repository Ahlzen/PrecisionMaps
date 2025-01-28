namespace MapLib.Geometry;

public static class CoordExtensions
{
    #region Transformations

    public static Coord Transform(this Coord source, double scale, double offsetX, double offsetY)
        => new(
            source.X * scale + offsetX,
            source.Y * scale + offsetY);

    public static Coord[] Transform(this Coord[] source, double scale, double offsetX, double offsetY)
    {
        var dest = new Coord[source.Length];
        for (int i = 0; i < source.Length; i++)
            dest[i] = Transform(source[i], scale, offsetX, offsetY);
        return dest;
    }

    #endregion
}