using MapLib.Geometry;
using OSGeo.OSR;

namespace MapLib.GdalSupport;

/// <summary>
/// Transforms coordinates between two coordinate systems.
/// </summary>
public class Transformer : IDisposable
{
    // Short-hand WKT strings for a few common SRS
    public const string WktWgs84 = "EPSG:4326";
    public const string WktWebMercator = "EPSG:3857";

    public string SourceSrs { get; }
    public string DestSrs { get; }

    protected SpatialReference SourceSpatialRef { get; }
    protected SpatialReference DestSpatialRef { get; }

    private CoordinateTransformation _transform;

    public Transformer(int sourceEpsg, int destEpsg)
        : this("EPSG:" + sourceEpsg, "EPSG:" + destEpsg)
    {
    }

    public Transformer(string sourceWkt, string destWkt)
    {
        SourceSrs = sourceWkt;
        SourceSpatialRef = CreateSpatialReference(sourceWkt);
        DestSrs = destWkt;
        DestSpatialRef = CreateSpatialReference(destWkt);
        _transform = new CoordinateTransformation(SourceSpatialRef, DestSpatialRef,
            new CoordinateTransformationOptions());
    }

    private SpatialReference CreateSpatialReference(string wkt)
    {
        // HACK: Apparently SpatialReference's constructor won't
        // accept the shorthand SRS definitions directly, so
        // we parse it out... :(
        if (wkt.StartsWith("EPSG:"))
        {
            SpatialReference sr = new(null);
            int epsgNumber = int.Parse(wkt.Substring(5));
            sr.ImportFromEPSG(epsgNumber);
            return sr;
        }
        else
        {
            return new SpatialReference(wkt);
        }
    }

    public void Dispose()
    {
        SourceSpatialRef.Dispose();
        DestSpatialRef.Dispose();
    }
    
    public Coord Transform(Coord coord)
    {
        double[] result = new double[3];
        _transform.TransformPoint(result, coord.X, coord.Y, 0);
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
