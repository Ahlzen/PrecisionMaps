using System.Diagnostics;

namespace MapLib.Geometry.Helpers;

/// <summary>
/// Implements line simplification using the Douglas-Peucker algorithm.
/// </summary>
/// <remarks>
/// Based on code by Adam Hancock
/// https://gist.github.com/ahancock1/0d99b43c4c01ef9b4fe4a5e7ad1e9029
/// 
/// Probably not the most efficient implementation, but this
/// will do for now.
/// </remarks>
public class DouglasPeucker
{
    /// <summary>
    /// Interpolates the sepcified points by reducing until the sepcified
    /// tolerance is met or the specified max number of points is met.
    /// </summary>
    /// <remarks>
    /// Must specify at least one of maxPointCount or tolerance, or this
    /// operation will have no effect.
    /// </remarks>
    /// <param name="points">The points to reduce.</param>
    /// <param name="maxPointCount">The max number of points to return.</param>
    /// <param name="tolerance">The min distance tolerance of points to return.</param>
    /// <returns>The interpolated reduced points.</returns>
    public static Coord[] Simplify(Coord[] points,
        int maxPointCount = int.MaxValue,
        double tolerance = 0d)
    {
        if (maxPointCount == 0 && tolerance == 0)
            return points; // nothing to do!
        if (maxPointCount < MinCoords)
            //return points; // line too short
            maxPointCount = MinCoords;
        int originalPointCount = points.Length;
        List<Coord> pointsList = points.ToList();
        var segments = GetSegments(pointsList).ToList();
        Reduce(ref segments, pointsList, maxPointCount, tolerance);
        Coord[] result = segments
            .OrderBy(p => p.StartIndex)
            .SelectMany((s, i) => GetCoords(s, segments.Count, i, pointsList))
            .ToArray();
        int simplifiedPointCount = result.Length;
        Debug.WriteLine($"Simplified line: {originalPointCount} -> {simplifiedPointCount} points");
        return result;
    }


    private const int MinCoords = 3;

    /// <summary>
    /// Class representing a Douglas Peucker
    /// segment. Contains the start and end index of the line,
    /// the biggest distance of a point from the line and the
    /// points index.
    /// </summary>
    private class Segment
    {
        /// <summary>
        /// The start index of the line.
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        ///     The end index of the line.
        /// </summary>
        public int EndIndex { get; set; }

        /// <summary>
        /// Index of point with the biggest perpendicular distance.
        /// </summary>
        public int MaxDistanceIndex { get; set; }

        /// <summary>
        /// The max perpendicular distance of a point along the line.
        /// </summary>
        public double MaxDistance { get; set; }
    }

    /// <summary>
    ///     Gets the perpendicular distance of a point to the line between start
    ///     and end.
    /// </summary>
    /// <param name="start">The start point of the line.</param>
    /// <param name="end">The end point of the line.</param>
    /// <param name="point">
    ///     The point to calculate the perpendicular distance of.
    /// </param>
    /// <returns>The perpendicular distance.</returns>
    private static double GetDistance(Coord start, Coord end, Coord point)
    {
        var x = end.X - start.X;
        var y = end.Y - start.Y;

        var m = x * x + y * y;

        var u = ((point.X - start.X) * x + (point.Y - start.Y) * y) / m;

        if (u < 0)
        {
            x = start.X;
            y = start.Y;
        }
        else if (u > 1)
        {
            x = end.X;
            y = end.Y;
        }
        else
        {
            x = start.X + u * x;
            y = start.Y + u * y;
        }

        x = point.X - x;
        y = point.Y - y;

        return Math.Sqrt(x * x + y * y);
    }

    /// <summary>
    ///     Creates a new <see cref="Segment"/> with the start and end indices.
    ///     Calculates the max perpendicular distance for each specified point
    ///     against the line between start and end.
    /// </summary>
    /// <param name="start">The start index of the line.</param>
    /// <param name="end">The end index of the line.</param>
    /// <param name="points">The points.</param>
    /// <returns>The Segment</returns>
    /// <remarks>
    ///     If the segment doesnt contain enough values to be split again the
    ///     segment distance property is left as 0. This ensures that the segment
    ///     wont be selected again from the <see cref="Reduce(ref List{Segment},
    ///     List{Coord}, int, double)"/> part of the algorithm.
    /// </remarks>
    private static Segment CreateSegment(int start, int end, List<Coord> points)
    {
        var count = end - start;

        if (count >= MinCoords - 1)
        {
            var first = points[start];
            var last = points[end];

            var max = points.GetRange(start + 1, count - 1)
                .Select((point, index) => new
                {
                    Index = start + 1 + index,
                    Distance = GetDistance(first, last, point)
                }).OrderByDescending(p => p.Distance).First();

            return new Segment
            {
                StartIndex = start,
                EndIndex = end,
                MaxDistanceIndex = max.Index,
                MaxDistance = max.Distance
            };
        }

        return new Segment
        {
            StartIndex = start,
            EndIndex = end,
            MaxDistanceIndex = -1
        };
    }

    /// <summary>
    ///     Splits the specified segment about the perpendicular index and return
    ///     the segment before and after with calculated values.
    /// </summary>
    /// <param name="segment">The segment to split.</param>
    /// <param name="points">The points.</param>
    /// <returns>The two segments.</returns>
    private static IEnumerable<Segment> SplitSegment(Segment segment,
        List<Coord> points)
    {
        return new[]
        {
            CreateSegment(segment.StartIndex, segment.MaxDistanceIndex, points),
            CreateSegment(segment.MaxDistanceIndex, segment.EndIndex, points)
        };
    }

    /// <summary>
    ///     Check to see if the point has valid values and returns false if not.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns>True if the points values are valid.</returns>
    private static bool IsValid(Coord point)
    {
        return !double.IsNaN(point.X) && !double.IsNaN(point.Y);
    }



    /// <summary>
    ///     Gets the reduced points from the <see cref="Segment"/>. Invalid values
    ///     are included in the result as well as last point of the last segment.
    /// </summary>
    /// <param name="segment">The segment to get the indices from.</param>
    /// <param name="count">The total number of segments in the algorithm.</param>
    /// <param name="index">The index of the current segment.</param>
    /// <param name="points">The points.</param>
    /// <returns>The valid points from the segment.</returns>
    private static IEnumerable<Coord> GetCoords(Segment segment, int count,
        int index, List<Coord> points)
    {
        yield return points[segment.StartIndex];

        var next = segment.EndIndex + 1;

        var isGap = next < points.Count && !IsValid(points[next]);

        if (index == count - 1 || isGap)
        {
            yield return points[segment.EndIndex];

            if (isGap)
            {
                yield return points[next];
            }
        }
    }

    /// <summary>
    ///     Gets the initial <see cref="Segment"/> for the algorithm. If points
    ///     contains invalid values then multiple segments are returned for each
    ///     side of the invalid value.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <returns>The segments.</returns>
    private static IEnumerable<Segment> GetSegments(List<Coord> points)
    {
        var previous = 0;

        foreach (var p in points.Select((p, i) => new
        {
            Coord = p,
            Index = i
        })
        .Where(p => !IsValid(p.Coord)))
        {
            yield return CreateSegment(previous, p.Index - 1, points);

            previous = p.Index + 1;
        }

        yield return CreateSegment(previous, points.Count - 1, points);
    }

    /// <summary>
    ///     Reduces the segments until the specified max or tolerance has been met
    ///     or the points can no longer be reduced.
    /// </summary>
    /// <param name="segments">The segements to reduce.</param>
    /// <param name="points">The points.</param>
    /// <param name="max">The max number of points to return.</param>
    /// <param name="tolerance">The min distance tolerance for the points.</param>
    private static void Reduce(ref List<Segment> segments, List<Coord> points,
        int max,
        double tolerance)
    {
        var gaps = points.Count(p => !IsValid(p));

        // Check to see if max numbers has been reached.
        while (segments.Count + gaps < max - 1)
        {
            // Get the largest perpendicular distance segment.
            var current = segments.OrderByDescending(s => s.MaxDistance).First();

            // Check if tolerance has been met yet or can no longer reduce.
            if (current.MaxDistance <= tolerance)
            {
                break;
            }

            segments.Remove(current);

            var split = SplitSegment(current, points);

            segments.AddRange(split);
        }
    }
}
