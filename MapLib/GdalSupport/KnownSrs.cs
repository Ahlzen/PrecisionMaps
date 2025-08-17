namespace MapLib.GdalSupport;

public class KnownSrs
{
    // Short-hand WKT strings for a few common SRS
    public const string EpsgWgs84 = "EPSG:4326";
    public const string EpsgWebMercator = "EPSG:3857";
    public const string EpsgNad83 = "EPSG:4269";
    public const string EpsgRobinson = "EPSG:54030";

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
}
