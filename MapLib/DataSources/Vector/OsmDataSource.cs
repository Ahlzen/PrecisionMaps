using MapLib.FileFormats.Vector;
using MapLib.Geometry;
using MapLib.Util;

namespace MapLib.DataSources.Vector;

public class OsmDataSource : BaseCachingDataSource, IVectorDataSource
{
    public string Name => "OpenStreetMap";

    public static Bounds _bounds = new Bounds(-180, 180, -90, 90);
    public Bounds? BoundsWgs84 => _bounds;

    public string Srs => "EPSG:4326"; // WGS84 lon/lat

    // Cache OSM data for up to a week by default. Set the CacheDuration
    // property in the data source to override.
    public override TimeSpan DefaultCacheDuration => TimeSpan.FromDays(7);

    public OsmDataSource()
    {
    }

    public VectorData GetData(Bounds bounds)
    {
        string baseFilename = $"osm_{bounds.XMin}_{bounds.YMin}_{bounds.XMax}_{bounds.YMax}";
        string filename = GetExistingCachedFile(baseFilename, ".osm") ?? DownloadData(bounds);
        OsmDataReader reader = new();
        VectorData data = reader.ReadFile(filename);
        return data;
    }

    private string DownloadData(Bounds bounds)
    {
        // Overpass URL format:
        // https://overpass-api.de/api/map?bbox=-77.44984,25.04861,-77.39489,25.08342

        string overpassUrl = "https://overpass-api.de/api/map?" +
            $"bbox={bounds.XMin},{bounds.YMin},{bounds.XMax},{bounds.YMax}";

        // Download data
        string destFilename = FileSystemHelpers.GetTempFileName(".osm");
        UrlHelper.DownloadUrl(overpassUrl, destFilename);
        return destFilename;
    }
}
