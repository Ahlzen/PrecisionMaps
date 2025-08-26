namespace MapLib.GdalSupport;

public class KnownSrs
{
    // Short-hand WKT strings for a few common SRS
    public const string EpsgWgs84 = "EPSG:4326";
    public const string EpsgWebMercator = "EPSG:3857";
    public const string EpsgNad83 = "EPSG:4269";


    // NOTE: The following SRS are not recognized by their
    // respective EPSG numbers in this version of PROJ.
    // Workaround: Spell out the full WKT or WKT2 string.

    /// <remarks>
    /// EPSG 54029 or 53029
    /// </remarks>
    public const string WktVanDerGrinten =
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
    ID[""ESRI"",54029]]";

    /// <remarks>
    /// EPSG 54030
    /// </remarks>
    public const string WktRobinson =
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
    ID[""ESRI"",54030]]";
}
