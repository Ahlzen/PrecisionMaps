using MapLib.Geometry;
using OSGeo.OSR;
using System.Diagnostics;

namespace MapLib.GdalSupport;

/// <summary>
/// Transforms coordinates between two coordinate systems.
/// </summary>
public class Transformer : IDisposable
{
    public Srs SourceSrs { get; private set; }
    public Srs DestSrs { get; private set; }

    private CoordinateTransformation _transform;

    public Transformer(Srs sourceSrs, Srs destSrs)
    {
        SourceSrs = sourceSrs;
        DestSrs = destSrs;
        _transform = new CoordinateTransformation(
            SourceSrs.SpatialReference, DestSrs.SpatialReference,
            new CoordinateTransformationOptions());
    }

    public void Dispose()
    {
        _transform.Dispose();
    }

    public Coord Transform(Coord coord)
    {
        double[] result = [coord.X, coord.Y, 0];
        _transform.TransformPoint(result);
        return new Coord(result[0], result[1]);
    }

    public Coord[] Transform(Coord[] coords)
    {
        // Deconstruct coords into individual arrays
        int count = coords.Length;
        double[] xCoords = new double[count];
        double[] yCoords = new double[count];
        double[] zCoords = new double[count];
        for (int i = 0; i < count; i++) {
            xCoords[i] = coords[i].X;
            yCoords[i] = coords[i].Y;
        }

        // Transform
        _transform.TransformPoints(count, xCoords, yCoords, zCoords);

        // Reassemble coords
        Coord[] result = new Coord[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = new Coord(xCoords[i], yCoords[i]);
        }
        return result;
    }

    public Coord[][] Transform(Coord[][] coords) =>
        coords.Select(Transform).ToArray();

    public Bounds Transform(Bounds b)
        => new(
            Transform(b.BottomLeft),
            Transform(b.TopRight));
} 
