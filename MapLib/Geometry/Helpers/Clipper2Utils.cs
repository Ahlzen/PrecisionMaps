﻿using Clipper2Lib;

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
    public static Polygon ToPolygon(this PathD path, TagList? tags)
        => new Polygon(ToCoords(path, true), tags);
    public static Line ToLine(this PathD path, TagList? tags)
        => new Line(ToCoords(path, false), tags);

    public static Coord[][] ToCoords(this PathsD paths, bool arePolygons)
    {
        List<Coord[]> coords = new List<Coord[]>(paths.Count);
        foreach (PathD path in paths)
            coords.Add(ToCoords(path, arePolygons));
        return coords.ToArray();
    }
    public static MultiPolygon ToMultiPolygon(this PathsD paths, TagList? tags)
        => new MultiPolygon(ToCoords(paths, true), tags);
    public static MultiLine ToMultiLine(this PathsD paths, TagList? tags)
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

    /// <summary>
    /// Returns a reasonable epsilon value (smallest distance from
    /// line through adjacent points) to use as an argument in Clipper's
    /// SimplifyPath.
    /// </summary>
    /// <param name="bounds">Feature's bounds</param>
    /// <remarks>
    /// We aim for something that's meaningful, but shouldn't visibly
    /// affect the result. Here we use 1/1,000,000 of the feature's scale:
    /// See http://www.angusj.com/clipper2/Docs/Units/Clipper/Functions/SimplifyPaths.htm
    /// </remarks>
    public static double GetSimplifyEpsilon(Bounds bounds)
        => Math.Max(bounds.Width, bounds.Height) * 0.000001;
}