namespace MapLib.Geometry.Helpers;

/// <summary>
/// Implements line simplification using the Visvalingam-Whyatt algorithm.
/// </summary>
/// <remarks>
/// https://en.wikipedia.org/wiki/Visvalingam%E2%80%93Whyatt_algorithm
/// </remarks>
public static class VisvalingamWhyatt
{
    public static Coord[] Simplify(Coord[] points,
        int maxPointCount = int.MaxValue,
        double toleranceMaxArea = double.MaxValue)
    {
        if (points.Length <= 3) return points;
        if (maxPointCount < 3) maxPointCount = 3;

        // Build list of items and priority queue
        
        Item head = new() { Coord = points[0] };
        PriorityQueue<Item, double> queue = new();
        queue.Enqueue(head, double.MaxValue);
        Item prev = head;
        for (int i = 1; i < points.Length - 1; i++)
        {
            Item curr = new Item() {
                Coord = points[i],
                Area = GetArea(points[i - 1], points[i], points[i + 1]),
                Prev = prev,
            };
            prev.Next = curr;
            queue.Enqueue(curr, curr.Area ?? double.MaxValue);
            prev = curr;
        }
        Item end = new Item() {
            Coord = points[^1],
            Area = null,
            Prev = prev
        };
        prev.Next = end;
        queue.Enqueue(end, double.MaxValue);
        int itemCount = points.Length;

        // Iterate until we're done
        while (true)
        {
            if (itemCount <= maxPointCount)
                break;

            if (!queue.TryDequeue(out Item? curr, out double enqueuedArea))
                throw new ApplicationException("No items in queue."); // this shouldn't happen
            if (enqueuedArea != curr.Area)
                continue; // Item's area has been updated since enqueued. Discard.
            if (curr.Area >= toleranceMaxArea)
                break; // done

            // Remove current item, update neighbors and enqueue new areas
            Item? p = curr.Prev;
            Item? n = curr.Next;
            if (p != null) p.Next = n;
            if (n != null) n.Prev = p;
            UpdateItemArea(p);
            UpdateItemArea(n);
            if (p != null) queue.Enqueue(p, p.Area ?? double.MaxValue);
            if (n != null) queue.Enqueue(n, n.Area ?? double.MaxValue);
            itemCount--;
        }

        // Build new array of coords from list
        Coord[] result = new Coord[itemCount];
        int j = 0;
        Item? c = head;
        while (c != null)
        {
            result[j++] = c.Coord;
            c = c.Next;
        }
        return result;
    }

    private class Item
    {
        public Coord Coord { get; set; }
        public double? Area { get; set; } = null;
        public Item? Prev { get; set; } = null;
        public Item? Next { get; set; } = null;
    }

    private static void UpdateItemArea(Item? item)
    {
        if (item == null) return;
        if (item.Prev == null || item.Next == null) return;
        item.Area = GetArea(item.Prev.Coord, item.Coord, item.Next.Coord);
    }

    private static double GetArea(Coord prev, Coord curr, Coord next)
        => 0.5 * Math.Abs(
            prev.X * curr.Y + curr.X * next.Y + next.X * prev.Y -
            prev.X * next.Y - curr.X * prev.Y - next.X * curr.Y);
}


public static class VisvalingamWhyatt_Naive
{
    // Naive implementation for testing only - this is inefficient and does not scale!

    public static Coord[] Simplify(Coord[] points,
        int maxPointCount = int.MaxValue,
        double toleranceMaxArea = double.MaxValue)
    {
        List<Coord> coordList = new(points);
        while (Optimize_SingleStep(coordList, maxPointCount, toleranceMaxArea)) {}
        return coordList.ToArray();
    }

    /// <summary>
    /// Attempt to remove a single point.
    /// </summary>
    /// <returns>
    /// True if a point was removed, false otherwise.
    /// </returns>
    private static bool Optimize_SingleStep(
        List<Coord> coords, int maxPointCount, double toleranceMaxArea)
    {
        if (coords.Count < 3) return false;
        if (coords.Count <= maxPointCount) return false;

        int minAreaIndex = 1;
        double minArea = double.MaxValue;
        for (int i = 1; i < coords.Count-1; i++)
        {
            double area = GetArea(coords[i - 1], coords[i], coords[i + 1]);
            if (area < minArea)
            {
                minArea = area;
                minAreaIndex = i;
            }
        }

        if (minArea > toleranceMaxArea)
            return false;

        coords.RemoveAt(minAreaIndex);
        return true;
    }

    private static double GetArea(Coord prev, Coord curr, Coord next)
        => 0.5 * Math.Abs(
            prev.X * curr.Y + curr.X * next.Y + next.X * prev.Y -
            prev.X * next.Y - curr.X * prev.Y - next.X * curr.Y);

}
