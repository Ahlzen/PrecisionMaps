using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using MapLib.Util;

namespace MapLib.Tests;

internal class TestDataManager
{
    public static readonly Dictionary<string, string> UrlsByFilename = new()
    {
        ///// Vector data sets
        
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

        // MassGIS Contour Lines
        {"MassGIS Contours/contour_1ft_19TCG339674.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/contours/contour_1ft_19TCG339674.zip" },
        {"MassGIS Contours/contour_1ft_19TCG340674.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/contours/contour_1ft_19TCG340674.zip" },
        {"MassGIS Contours/contour_1ft_19TCG339672.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/contours/contour_1ft_19TCG339672.zip" },
        {"MassGIS Contours/contour_1ft_19TCG340672.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/contours/contour_1ft_19TCG340672.zip" },


        ///// Raster data sets

        // MassGIS Elevation from LiDAR
        // These are single-band Float32 rasters, forming a contiguous grid
        {"MassGIS LiDAR/be_19TCG339674.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/be_rasters/be_19TCG339674.zip" },
        {"MassGIS LiDAR/be_19TCG340674.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/be_rasters/be_19TCG340674.zip" },
        {"MassGIS LiDAR/be_19TCG339672.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/be_rasters/be_19TCG339672.zip" },
        {"MassGIS LiDAR/be_19TCG340672.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/lidar/2021_LIDAR/be_rasters/be_19TCG340672.zip" },

        // MassGIS 2023 Aerial Imagery (orthophotos)
        {"MassGIS Aerial/19TCG390740.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/images/coq2023_15cm_jp2/19TCG390740.zip"},
        {"MassGIS Aerial/19TCG390725.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/images/coq2023_15cm_jp2/19TCG390725.zip"},
        {"MassGIS Aerial/19TCG405740.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/images/coq2023_15cm_jp2/19TCG405740.zip"},
        {"MassGIS Aerial/19TCG405725.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/images/coq2023_15cm_jp2/19TCG405725.zip"},

        // MassGIS Impervious Surface
        {"MassGIS Impervious Surface/imp_ne6.zip", "https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/images/impervious/imp_ne6.zip"},

        // USGS Topo Quad 1:25,000
        {"USGS Topo Quad 25k/q249882.zip","https://s3.us-east-1.amazonaws.com/download.massgis.digital.mass.gov/images/usgs_topo25k_tiff/q249882.zip" },
        
        // USGS National Elevation Dataset DEM
        {"USGS NED/USGS_OPR_MA_CentralEastern_2021_B21_be_19TCG339672.tif","https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/OPR/Projects/MA_CentralEastern_2021_B21/MA_CentralEastern_1_2021/TIFF/USGS_OPR_MA_CentralEastern_2021_B21_be_19TCG339672.tif" }

        // MassGIS Shaded Relief
        // TODO
    };

    [Test]
    [Explicit]
    public void DownloadData() => EnsureTestDataReady(Console.Out);

    /// <summary>
    /// Checks that all of the required test data files exist locally, and
    /// downloads any missing file(s).
    /// </summary>
    /// <param name="logger">
    /// Optional TextWriter for status/error output, such as Console.Out.
    /// No output if all files exist.
    /// </param>
    public static void EnsureTestDataReady(TextWriter? logger)
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
                    Task t = UrlHelper.DownloadUrl(url, destPath);
                    t.Wait();

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
}
