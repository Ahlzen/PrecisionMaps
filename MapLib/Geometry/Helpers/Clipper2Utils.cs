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

    //public static PathsD ToPathsD(this Line[] lines)
    //{
    //    PathsD pathsD = new PathsD(lines.Length);
    //    foreach (Line line in lines)
    //    {
    //        PathD pathD = ToPathD(line);
    //        pathsD.Add(pathD);
    //    }
    //    return pathsD;
    //}
    public static PathsD ToPathsD(this Coord[][] lines)
    {
        PathsD pathsD = new PathsD(lines.Length);
        foreach (Coord[] line in lines)
        {
            PathD pathD = ToPathD(line);
            pathsD.Add(pathD);
        }
        return pathsD;
    }
    public static PathsD ToPathsD(this MultiPolygon multiPolygon)
        => ToPathsD(multiPolygon.Coords);
    public static PathsD ToPathsD(this MultiLine multiLine)
        => ToPathsD(multiLine.Coords);


    // Clipper to geometry

    /// <param name="isPolygon">
    /// If true, this method ensure that the first and last points
    /// are equal (Clipper doesn't guarantee this)
    /// </param>
    public static Coord[] ToCoords(this PathD path, bool isPolygon)
    {
        // If this is a polygon, we require start and end point to be equal
        bool needsEndPoint = isPolygon && path[0] != path[^1];
        int coordCount = path.Count + (needsEndPoint ? 1 : 0);
        var coords = new Coord[coordCount];
        for (int i = 0; i < path.Count; i++)
            coords[i] = new Coord(path[i].x, path[i].y);
        if (needsEndPoint)
            coords[^1] = coords[0];
        return coords;
    }
    public static Polygon ToPolygon(this PathD path, TagDictionary? tags)
        => new Polygon(ToCoords(path, true), tags);
    public static Line ToLine(this PathD path, TagDictionary? tags)
        => new Line(ToCoords(path, false), tags);


    //public static Polygon[] ToPolygons(this PathsD paths, TagDictionary? tags)
    //{
    //    var polygons = new Polygon[paths.Count];
    //    for (int p = 0; p < polygons.Length; p++)
    //        polygons[p] = paths[p].ToPolygon(tags);
    //    return polygons;
    //}
    //public static Line[] ToLines(this PathsD paths, TagDictionary? tags)
    //{
    //    var lines = new Line[paths.Count];
    //    for (int p = 0; p < paths.Count; p++)
    //        lines[p] = paths[p].ToLine(tags);
    //    return lines;
    //}
    public static Coord[][] ToCoords(this PathsD paths, bool arePolygons)
    {
        List<Coord[]> coords = new List<Coord[]>(paths.Count);
        //Coord[][] coords = new Coord[][paths.Count];
        //for (int p = 0; p < paths.Count; p++)
        //polygons[p] = paths[p].ToPolygon(tags);
        foreach (PathD path in paths)
            coords.Add(ToCoords(path, arePolygons));
        return coords.ToArray();
    }
    public static MultiPolygon ToMultiPolygon(this PathsD paths, TagDictionary? tags)
        //=> new MultiPolygon(ToPolygons(paths, tags));
        => new MultiPolygon(ToCoords(paths, true), tags);
    public static MultiLine ToMultiLine(this PathsD paths, TagDictionary? tags)
        //=> new MultiLine(ToLines(paths, tags));
        => new MultiLine(ToCoords(paths, true), tags);


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