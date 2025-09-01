using MapLib.Geometry;
using OSGeo.GDAL;
using OSGeo.OSR;
using System.Data;

namespace MapLib.GdalSupport;

/// <summary>
/// Wraps a GDAL/OGR SpatialReference object.
/// </summary>
/// <remarks>
/// NOTE: To ensure that coordinate transformations work with our coordinate
/// format, we need to ensure that AxisMappingStrategy is always set to
/// TRADITIONAL_GIS_ORDER.
/// </remarks>
public class Srs : IDisposable, IEquatable<Srs>
{
    internal SpatialReference SpatialReference { get; private set; }

    #region Constructors

    /// <param name="wktOrEpsg">
    /// A full PROJ-style WKT/WKT2 definition, or and EPSG
    /// shorthand (e.g. "EPSG:4326")
    /// </param>
    public Srs(string wktOrEpsg)
    {
        GdalUtils.EnsureInitialized();

        // HACK: Apparently SpatialReference's constructor won't
        // accept the shorthand SRS definitions directly, so
        // we parse it out... :(
        if (wktOrEpsg.StartsWith("EPSG:"))
        {
            SpatialReference = new(null);
            int epsgNumber = int.Parse(wktOrEpsg.Substring(5));
            SpatialReference.ImportFromEPSG(epsgNumber);
            SpatialReference.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
        }
        else
        {
            SpatialReference = new SpatialReference(wktOrEpsg);
            SpatialReference.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
        }
    }

    static Srs()
    {
        // NOTE: Unfortunately, we have to initialize the
        // static known SRS members here, rather than using inline
        // static initialiers, since using the latter we cannot ensure
        // that GDAL is initialized prior.
        GdalUtils.EnsureInitialized();

        Wgs84 = new("EPSG:4326");
        WebMercator = new("EPSG:3857");
        Nad83 = new("EPSG:4269");

        // NOTE: The following SRS are not recognized by their
        // respective EPSG numbers in this version of PROJ.
        // Workaround: Spell out the full WKT or WKT2 string.

        /// EPSG 54029 or 53029
        VanDerGrinten = new(
    @"PROJCRS[""World_Van_der_Grinten_I"",
        BASEGEOGCRS[""WGS 84"",
            DATUM[""World Geodetic System 1984"",
                ELLIPSOID[""WGS 84"",6378137,298.257223563,
                    LENGTHUNIT[""metre"",1]]],
            PRIMEM[""Greenwich"",0,
                ANGLEUNIT[""Degree"",0.0174532925199433]]],
        CONVERSION[""World_Van_der_Grinten_I"",
            METHOD[""Van Der Grinten""],
            PARAMETER[""Longitude of natural origin"",0,
                ANGLEUNIT[""Degree"",0.0174532925199433],
                ID[""EPSG"",8802]],
            PARAMETER[""False easting"",0,
                LENGTHUNIT[""metre"",1],
                ID[""EPSG"",8806]],
            PARAMETER[""False northing"",0,
                LENGTHUNIT[""metre"",1],
                ID[""EPSG"",8807]]],
        CS[Cartesian,2],
            AXIS[""(E)"",east,
                ORDER[1],
                LENGTHUNIT[""metre"",1]],
            AXIS[""(N)"",north,
                ORDER[2],
                LENGTHUNIT[""metre"",1]],
        USAGE[
            SCOPE[""Not known.""],
            AREA[""World.""],
            BBOX[-90,-180,90,180]],
        ID[""ESRI"",54029]]");

        /// EPSG 54030
        Robinson = new(
    @"PROJCRS[""World_Robinson"",
        BASEGEOGCRS[""WGS 84"",
            DATUM[""World Geodetic System 1984"",
                ELLIPSOID[""WGS 84"",6378137,298.257223563,
                    LENGTHUNIT[""metre"",1]]],
            PRIMEM[""Greenwich"",0,
                ANGLEUNIT[""Degree"",0.0174532925199433]]],
        CONVERSION[""World_Robinson"",
            METHOD[""Robinson""],
            PARAMETER[""Longitude of natural origin"",0,
                ANGLEUNIT[""Degree"",0.0174532925199433],
                ID[""EPSG"",8802]],
            PARAMETER[""False easting"",0,
                LENGTHUNIT[""metre"",1],
                ID[""EPSG"",8806]],
            PARAMETER[""False northing"",0,
                LENGTHUNIT[""metre"",1],
                ID[""EPSG"",8807]]],
        CS[Cartesian,2],
            AXIS[""(E)"",east,
                ORDER[1],
                LENGTHUNIT[""metre"",1]],
            AXIS[""(N)"",north,
                ORDER[2],
                LENGTHUNIT[""metre"",1]],
        USAGE[
            SCOPE[""Not known.""],
            AREA[""World.""],
            BBOX[-90,-180,90,180]],
        ID[""ESRI"",54030]]");
    }

    private Srs(SpatialReference sr)
    {
        GdalUtils.EnsureInitialized();

        SpatialReference = sr;
    }

    public static Srs FromDataset(Dataset dataset)
    {
        if (dataset.RasterCount > 0)
        {
            // Assume this is a raster data set
            string wkt = dataset.GetProjectionRef();
            if (string.IsNullOrEmpty(wkt))
                throw new ApplicationException("Failed to get WKT for projection.");
            SpatialReference sr = new(null);
            sr.ImportFromWkt(ref wkt);
            sr.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
            return new Srs(sr);
        }
        else if (dataset.GetLayerCount() > 0)
        {
            // Assume this is a vector data set
            SpatialReference sr = dataset.GetLayer(0).GetSpatialRef();
            sr.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
            return new Srs(sr);
        }
        else
            throw new ApplicationException(
                "Can't get SRS: No rasters or vector layers in dataset.");
    }

    public static Srs FromFile(string filename)
    {
        using Dataset dataSet = GdalUtils.OpenDataset(filename);
        return FromDataset(dataSet);
    }

    public void Dispose()
    {
        SpatialReference.Dispose();
    }

    #endregion

    #region Properties

    public string Name => SpatialReference.GetName();

    public string GetWkt(bool pretty = false)
    {
        if (pretty) {
            SpatialReference.ExportToPrettyWkt(out string wkt, 0);
            return wkt;
        }
        else {
            SpatialReference.ExportToWkt(out string wkt, null);
            return wkt;
        }
    }

    public int? Epsg
    {
        get
        {
            string? authority = SpatialReference.GetAuthorityName(null);
            if (authority != "EPSG")
                return null;
            string? epsgString = SpatialReference.GetAuthorityCode(null);
            if (int.TryParse(epsgString, out int epsgNumber)) {
                return epsgNumber;
            }
            return null;
        }
    }

    public Bounds? BoundsLatLon
    {
        get
        {
            AreaOfUse? area = SpatialReference.GetAreaOfUse();
            if (area == null)
                return null;
            return new Bounds(
                area.west_lon_degree, area.east_lon_degree,
                area.south_lat_degree, area.north_lat_degree);
        }
    }

    #endregion

    public override int GetHashCode()
    {
        // Implement if needed (I doubt it).
        throw new NotImplementedException();
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != typeof(Srs))
            return false;
        return Equals((Srs)obj);
    }

    public bool Equals(Srs? other)
    {
        // Compare by WKT string. Probably not very efficient
        // if we end up having to do this a lot.

        if (other == null)
            return false;
        string? thisWkt = GetWkt();
        string? otherWkt = other.GetWkt();
        return 
            thisWkt != null &&
            thisWkt == otherWkt;
    }

    public static bool operator == (Srs? a, Srs? b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;
        return a.Equals(b);
    }

    public static bool operator != (Srs? a, Srs? b)
        => ! (a == b);

    #region Known common projections

    public static Srs Wgs84 { get; }
    public static Srs WebMercator { get; }
    public static Srs Nad83 { get; }
    public static Srs VanDerGrinten { get; }
    public static Srs Robinson { get; }

    #endregion
}