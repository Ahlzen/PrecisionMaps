using Clipper2Lib;

namespace MapLib.Geometry.Helpers;

internal static class Clipper2Utils
{
    // Geometry to Clipper

    public static PathD ToPathD(this Coord[] coords)
    {
        PathD pathD = new PathD(coords.Length);
        foreach (Coord coord in coords)
        {
            pathD.Add(new PointD(coord.X, coord.Y));
        }
        return pathD;
    }
    public static PathD ToPathD(this Polygon polygon)
        => ToPathD(polygon.Coords);
    public static PathD ToPathD(this Line line)
        => ToPathD(line.Coords);

    public static PathsD ToPathsD(this Line[] lines)
    {
        PathsD pathsD = new PathsD(lines.Length);
        foreach (Line line in lines)
        {
            PathD pathD = ToPathD(line);
            pathsD.Add(pathD);
        }
        return pathsD;
    }
    public static PathsD ToPathsD(this MultiPolygon multiPolygon)
        => ToPathsD(multiPolygon.Polygons);
    public static PathsD ToPathsD(this MultiLine multiLine)
        => ToPathsD(multiLine.Lines);


    // Clipper to geometry

    public static Coord[] ToCoords(this PathD path)
    {
        var coords = new Coord[path.Count];
        for (int i = 0; i < coords.Length; i++)
            coords[i] = new Coord(path[i].x, path[i].y);
        return coords;
    }
    public static Polygon ToPolygon(this PathD path)
        => new Polygon(ToCoords(path));
    public static Line ToLine(this PathD path)
        => new Line(ToCoords(path));

    public static Polygon[] ToPolygons(this PathsD paths)
    {
        var polygons = new Polygon[paths.Count];
        for (int p = 0; p < polygons.Length; p++)
            polygons[p] = paths[p].ToPolygon();
        return polygons;
    }
    public static MultiPolygon ToMultiPolygon(this PathsD paths)
        => new MultiPolygon(ToPolygons(paths));
    public static MultiLine ToMultiLine(this PathsD paths)
        => new MultiLine(ToPolygons(paths));


    /// <summary>
    /// Calculates an appropriate scale to use with clipper's
    /// long integer coordinates.
    /// </summary>
    /// <remarks>
    /// NOTE: This may no longer be needed since Clipper2 does this
    /// internally and take double-precision geometry (PathD) directly.
    /// </remarks>
    /// <param name="extraBuffer">
    /// Extra space needed for certain operations like polygon
    /// offsets.
    /// </param>
    /// <param name="shapes">
    /// All geometry affecting the operation(s).
    /// </param>
    public static double GetScale(
        double extraBuffer = 0,
        params Shape[] shapes)
    {
        Bounds bounds = Bounds.FromBounds(
            shapes.Select(s => s.GetBounds()));

        double extent = Math.Max(
            Math.Max(Math.Abs(bounds.XMin), Math.Abs(bounds.YMin)),
            Math.Max(Math.Abs(bounds.YMin), Math.Abs(bounds.YMax)));

        // Add a generous amount of space since ops like poly offsettin
        // can extend corners far beyond the offset distance.
        extent += extraBuffer * 10;

        // Add some extra margin
        extent *= 2.0;

        double scale = long.MaxValue / extent;
        return scale;
    }
}