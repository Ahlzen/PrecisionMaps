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
    public const string WktNad83 = "EPSG:4269";

    /// <remarks>
    /// Should be EPSG 54029 or 53029, though they don't seem recognized
    /// with the version of PROJ we're using.
    /// Workaround: Spell it out!
    /// </remarks>
    public const string WktVanDerGrinten = @"
PROJCS[""World_Van_der_Grinten_I"",
    GEOGCS[""WGS 84"",
        DATUM[""WGS_1984"",
            SPHEROID[""WGS 84"",6378137,298.257223563,
                AUTHORITY[""EPSG"",""7030""]],
            AUTHORITY[""EPSG"",""6326""]],
        PRIMEM[""Greenwich"",0],
        UNIT[""Degree"",0.0174532925199433]],
    PROJECTION[""VanDerGrinten""],
    PARAMETER[""central_meridian"",0],
    PARAMETER[""false_easting"",0],
    PARAMETER[""false_northing"",0],
    UNIT[""metre"",1,
        AUTHORITY[""EPSG"",""9001""]],
    AXIS[""Easting"",EAST],
    AXIS[""Northing"",NORTH],
    AUTHORITY[""ESRI"",""54029""]]
";

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
        // NOTE: AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER
        // ensures that coordinate systems stick to [x,y,z] order
        // rather than e.g. lat/lon.

        SourceSrs = sourceWkt;
        SourceSpatialRef = CreateSpatialReference(sourceWkt);
        SourceSpatialRef.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
        DestSrs = destWkt;
        DestSpatialRef = CreateSpatialReference(destWkt);
        DestSpatialRef.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
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
