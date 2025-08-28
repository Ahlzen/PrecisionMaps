using MapLib.Geometry;
using OSGeo.GDAL;
using OSGeo.OSR;
using System.Data;

namespace MapLib.GdalSupport;

public class Srs : IDisposable
{
    internal SpatialReference SpatialReference { get; private set; }

    #region Constructors

    /// <param name="wktOrEpsg">
    /// A full PROJ-style WKT or WKT2 format definition,
    /// or an EPSG shortand (e.g. "EPSG:4326")
    /// </param>
    public Srs(string wktOrEpsg)
    {
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

    private Srs(SpatialReference sr)
    {
        SpatialReference = sr;
    }

    public static Srs FromDataset(Dataset dataset)
    {
        string wkt = dataset.GetProjectionRef();
        SpatialReference sr = new(null);
        sr.ImportFromWkt(ref wkt);
        sr.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
        return new Srs(sr);
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

    public string? Name => SpatialReference.GetName();

    public string? GetWkt(bool pretty = false)
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

    #region Known common projections

    public static Srs Wgs84 { get; private set; } = new("EPSG:4326");
    public static Srs WebMercator { get; private set; } = new("EPSG:3857");
    public static Srs Nad83 { get; private set; } = new("EPSG:4269");

    // NOTE: The following SRS are not recognized by their
    // respective EPSG numbers in this version of PROJ.
    // Workaround: Spell out the full WKT or WKT2 string.

    /// <remarks>
    /// EPSG 54029 or 53029
    /// </remarks>
    public static Srs VanDerGrinten { get; private set; } = new(
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

    /// <remarks>
    /// EPSG 54030
    /// </remarks>
    public static Srs WktRobinson { get; private set; } = new(
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

    #endregion
}