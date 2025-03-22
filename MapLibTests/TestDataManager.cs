using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapLib.Tests;

internal class TestDataManager
{
    public static readonly Dictionary<string, string> UrlsByFilename = new()
    {
        // OSM test data (OSM XML)
        {"osm-xml/Weymouth Detail.osm", "https://www.openstreetmap.org/api/0.6/map?bbox=-70.9473%2C42.18745%2C-70.91211%2C42.216" },
        {"osm-xml/Aaron River Reservoir.osm", "https://overpass-api.de/api/interpreter?data=%2F*%0AThis%20has%20been%20generated%20by%20the%20overpass-turbo%20wizard.%0AThe%20original%20search%20was%3A%0A%E2%80%9Cname%3D%22Aaron%20River%20Reservoir%22%E2%80%9D%0A*%2F%0A%5Bout%3Axml%5D%5Btimeout%3A25%5D%3B%0A%2F%2F%20gather%20results%0Anwr%5B%22name%22%3D%22Aaron%20River%20Reservoir%22%5D%2842.164930299745755%2C-70.86301803588869%2C42.22889868544603%2C-70.75650215148927%29%3B%0A%2F%2F%20print%20results%0Aout%20geom%3B%0A" },

        // OpenLayers example data (GeoJSON)
        {"GeoJSON/openlayers-line-samples.geojson", "https://github.com/openlayers/openlayers/raw/refs/heads/main/examples/data/geojson/line-samples.geojson" },
        {"GeoJSON/openlayers-point-samples.geojson", "https://github.com/openlayers/openlayers/raw/refs/heads/main/examples/data/geojson/point-samples.geojson" },
        {"GeoJSON/openlayers-polygon-samples.geojson", "https://github.com/openlayers/openlayers/raw/refs/heads/main/examples/data/geojson/polygon-samples.geojson" },
        {"GeoJSON/openlayers-vienna-streets.geojson", "https://github.com/openlayers/openlayers/raw/refs/heads/main/examples/data/geojson/vienna-streets.geojson" },
        {"GeoJSON/openlayers-world-cities.geojson", "https://github.com/openlayers/openlayers/raw/refs/heads/main/examples/data/geojson/world-cities.geojson" },

        // Natural Earth
        // NOTE: Downloading directly from naturalearthdata.com returns a HTTP 500
        // (known limitation; intentional?) so we get the files from the NACIS CDN instead.
        {"Natural Earth/ne_10m_populated_places_simple.zip", "https://naciscdn.org/naturalearth/10m/cultural/ne_10m_populated_places_simple.zip" },
        {"Natural Earth/ne_110m_land.zip", "https://naciscdn.org/naturalearth/110m/physical/ne_110m_land.zip" },

        // MassGIS Elevation from LiDAR
        {"MassGIS LiDAR/be_19TCG339674.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/be_rasters/be_19TCG339674.zip" },
        {"MassGIS LiDAR/be_19TCG340674.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/be_rasters/be_19TCG340674.zip" },
        {"MassGIS LiDAR/be_19TCG339672.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/be_rasters/be_19TCG339672.zip" },
        {"MassGIS LiDAR/be_19TCG340672.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/be_rasters/be_19TCG340672.zip" },

        // MassGIS Contour Lines
        {"MassGIS contours/contour_1ft_19TCG339674.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/contours/contour_1ft_19TCG339674.zip" },
        {"MassGIS contours/contour_1ft_19TCG340674.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/contours/contour_1ft_19TCG340674.zip" },
        {"MassGIS contours/contour_1ft_19TCG339672.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/contours/contour_1ft_19TCG339672.zip" },
        {"MassGIS contours/contour_1ft_19TCG340672.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/contours/contour_1ft_19TCG340672.zip" },
    };

    [Test]
    [Explicit]
    public async Task DownloadData() =>
        await EnsureTestDataReady(Console.Out);

    /// <summary>
    /// Checks that all of the required test data files exist locally, and
    /// downloads any missing file(s).
    /// </summary>
    /// <param name="logger">
    /// Optional TextWriter for status/error output, such as Console.Out.
    /// No output if all files exist.
    /// </param>
    public async static Task EnsureTestDataReady(TextWriter? logger)
    {
        foreach (string filename in UrlsByFilename.Keys)
        {
            // Make sure data is downloaded
            
            string destPath = Path.Join(BaseFixture.TestDataPath, filename);
            string? destDir = Path.GetDirectoryName(destPath);
            if (destDir == null)
                throw new ApplicationException("Expected path.");

            if (File.Exists(destPath))
            {
                // Already exists. Skip.
                continue;
            }
            else
            {
                // Ensure directory exists
                Directory.CreateDirectory(destDir);
                logger?.Write($"Downloading {filename}... ");
                string url = UrlsByFilename[filename];
                try
                {
                    await DownloadFile(url, destPath);

                    FileInfo fi = new FileInfo(destPath);
                    long length = fi.Length;

                    logger?.WriteLine($"Done. ({length:N0} bytes)");
                }
                catch (Exception ex)
                {
                    logger?.WriteLine("Failed: " + ex.Message);
                    continue;
                }
            }

            // If archive, unpack it
            if (filename.EndsWith(".zip"))
            {
                try
                {
                    logger?.Write($"Unpacking {filename}... ");
                    ZipFile.ExtractToDirectory(
                        destPath, destDir, true);
                    logger?.WriteLine("Done.");
                }
                catch (Exception ex)
                {
                    logger?.WriteLine("Failed: " + ex.Message);
                    continue;
                }
            }
        }
    }

    private static async Task DownloadFile(string url, string destFilename)
    {
        using (HttpClient client = new HttpClient())
        using (Stream s = await client.GetStreamAsync(url))
        using (FileStream fs = new FileStream(destFilename, FileMode.OpenOrCreate))
        {
            s.CopyTo(fs);
        }
    }
}
