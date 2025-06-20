using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Geometry.Helpers;

/// <summary>
/// Helpers for calculating the pole of inaccessibility in a polygon.
/// </summary>
/// <remarks>
/// The "Pole of Inaccesibility" is the point in a polygon that is farthest
/// from the boundary of the polygon. This may be a suitable point e.g.
/// for a text label or symbol.
/// 
/// Also known as PolyLabel... presumably since it's a suitable point
/// for placing a label or symbol within a polygon.
/// 
/// Adapted from
/// https://github.com/mapbox/polylabel/issues/26
/// by Shawn Castrianni, based on the PolyLabel Mapbox implementation.
/// </remarks>
public class PolyLabel
{
    private const float EPSILON = 1E-8f;

    /// <param name="polygon"></param>
    /// <param name="precision">
    /// Since this is an iterative algorithm, we can calculate the result
    /// to an arbitrary precision. If null, the default precision used
    /// is based on the size of the polygon.
    /// </param>
    /// <returns>
    /// The pole of inaccessibility for the polygon (see class remarks).
    /// </returns>
    public static Coord Calculate(Polygon polygon, double? precision = null)
        => Calculate(polygon.AsMultiPolygon().Coords, polygon.GetBounds(), precision);

    public static Coord Calculate(MultiPolygon multiPolygon, double? precision = null)
        => Calculate(multiPolygon.Coords, multiPolygon.GetBounds(), precision);

    public static Coord Calculate(Coord[][] multiPolygonCoords, double? precision = null)
        => Calculate(multiPolygonCoords, Bounds.FromCoords(multiPolygonCoords), precision);


    private static Coord Calculate(
        Coord[][] multiPolygonCoords,
        Bounds bounds,
        double? precision)
    {
        double polygonSize = Math.Max(bounds.Width, bounds.Height);
        double cellSize = Math.Min(bounds.Width, bounds.Height);
        double h = cellSize / 2;

        // Default precision. Since this is primarily used to find a reasonable
        // point for a label, high precision is usually not required.
        precision ??= polygonSize * 0.02;

        PriorityQueue<Cell, double> cellQueue = new();
        
        if (DoubleEquals(cellSize, 0))
            return new Coord(bounds.XMin, bounds.YMin);

        // Create initial cells
        for (double x = bounds.XMin; x < bounds.XMax; x += cellSize)
        {
            for (double y = bounds.YMin; y < bounds.YMax; y += cellSize)
            {
                Cell cell = new Cell(new Coord(x + h, y + h), h, multiPolygonCoords);
                cellQueue.Enqueue(cell, cell.Max);
            }
        }

        // Take centroid as the first best guess
        Cell bestCell = GetCentroidCell(multiPolygonCoords);

        // Special case for rectangular polygons
        Cell bboxCell = new Cell(bounds.Center, 0, multiPolygonCoords);
        if (bboxCell.D > bestCell.D)
            bestCell = bboxCell;

        int numProbes = cellQueue.Count;

        while (cellQueue.Count > 0)
        {
            // Pick the most promising cell from the queue
            Cell cell = cellQueue.Dequeue();

            // Update the best cell if we found a better one
            if (cell.D > bestCell.D)
                bestCell = cell;

            // Do not drill down further if there's no chance of a better solution
            if (cell.Max - bestCell.D <= precision)
                continue;

            // Split the cell into four cells
            h = cell.H / 2;
            Cell cell1 = new(new Coord(cell.X - h, cell.Y - h), h, multiPolygonCoords);
            cellQueue.Enqueue(cell1, cell1.Max);
            Cell cell2 = new(new Coord(cell.X + h, cell.Y - h), h, multiPolygonCoords);
            cellQueue.Enqueue(cell2, cell2.Max);
            Cell cell3 = new(new Coord(cell.X - h, cell.Y + h), h, multiPolygonCoords);
            cellQueue.Enqueue(cell3, cell3.Max);
            Cell cell4 = new(new Coord(cell.X + h, cell.Y + h), h, multiPolygonCoords);
            cellQueue.Enqueue(cell4, cell4.Max);
            numProbes += 4;
        }
        return new Coord(bestCell.X, bestCell.Y);
    }

    /// <returns>
    /// Signed distance from point to polygon outline (negative if
    /// point is outside).
    /// </returns>
    private static double CoordToPolygonDistance(Coord c,
        Coord[][] multiPolygon)
    {
        bool inside = false;
        double minDistSq = double.PositiveInfinity;

        for (int p = 0; p < multiPolygon.Length; p++)
        {
            Coord[] ring = multiPolygon[p];
            for (int i = 0, len = ring.Length, j = len - 1; i < len; j = i++)
            {
                Coord a = ring[i];
                Coord b = ring[j];
                if ((a.Y > c.Y != b.Y > c.Y) && (c.X < (b.X - a.X) * (c.Y - a.Y) / (b.Y - a.Y) + a.X))
                    inside = !inside;
                minDistSq = Math.Min(minDistSq, GetSegDistSq(c, a, b));
            }
        }
        return ((inside ? 1 : -1) * (float)Math.Sqrt(minDistSq));
    }

    /// <returns>
    /// Squared distance from a point c to a segment a-b.
    /// </returns>
    private static double GetSegDistSq(Coord c,
        Coord a, Coord b)
    {
        double x = a.X;
        double y = a.Y;
        double dx = b.X - x;
        double dy = b.Y - y;

        if (!DoubleEquals(dx, 0) || !DoubleEquals(dy, 0))
        {
            double t = ((c.X - x) * dx + (c.Y - y) * dy) / (dx * dx + dy * dy);
            if (t > 1)
            {
                x = b.X;
                y = b.Y;
            }
            else if (t > 0)
            {
                x += dx * t;
                y += dy * t;
            }
        }
        dx = c.X - x;
        dy = c.Y - y;
        return (dx * dx + dy * dy);
    }

    /// <summary>
    /// Get polygon centroid
    /// </summary>
    private static Cell GetCentroidCell(Coord[][] multipolygon)
    {
        double area = 0;
        double x = 0;
        double y = 0;
        Coord[] ring = multipolygon[0];

        for (int i = 0, len = ring.Length, j = len - 1; i < len; j = i++)
        {
            Coord a = ring[i];
            Coord b = ring[j];
            double f = a.X * b.Y - b.X * a.Y;
            x += (a.X + b.X) * f;
            y += (a.Y + b.Y) * f;
            area += f * 3;
        }
        if (DoubleEquals(area, 0))
            return new Cell(ring[0], 0, multipolygon);
        return new Cell(new Coord(x/area, y/area), 0, multipolygon);
    }

    private static bool DoubleEquals(double a, double b)
        => (Math.Abs(a - b) < EPSILON);

    private class Cell
    {
        public Coord C { get; }
        public double X => C.X;
        public double Y => C.Y;

        public double H { get; }
        public double D { get; }
        public double Max { get; }

        public Cell(Coord c, double h, Coord[][] multiPolygon)
        {
            C = c;
            H = h;
            D = CoordToPolygonDistance(c, multiPolygon);
            Max = D + H * (double)Math.Sqrt(2);
        }
    }
}
