using MapLib.GdalSupport;
using MapLib.Geometry;
using System.Threading.Tasks;

namespace MapLib.DataSources.Raster;

/// <summary>
/// USGS 3D Elevation Program (3DEP) and
/// National Elevation Dataset (NED)
/// </summary>
public class Usgs3depDataSource : BaseRasterDataSource
{
    public override string Name => "USGS 3D Elevation Program (3DEP)";

    public override string Srs => Epsg.Nad83;

    private string Subdirectory => "USGS_3DEP";

    // Approximate bounds (Lower 48 + AK + HI)
    // TODO: Find actual bounds of 3DEP
    public override Bounds? Bounds =>
        new(xmin: -179.8, xmax: -65.7, ymin: 18.3, ymax: 71.9);
    
    public override bool IsBounded => false;

    public double ScaleFactor { get; set; }

    public Usgs3depDataSource(double scaleFactor = 1)
    {
        ScaleFactor = scaleFactor;
    }

    public override Task<RasterData> GetData()
    {
        throw new NotImplementedException();
    }

    public override Task<RasterData> GetData(string destSrs)
    {
        throw new NotImplementedException();
    }

    public override async Task<RasterData> GetData(Bounds boundsWgs84)
    {
        List<string> localFiles = new();
        foreach (string url in GetDownloadUrls(boundsWgs84))
        {
            // TODO: Handle non-existent files better
            try
            {
                string filePath = await DownloadAndCache(url, Subdirectory);
                Console.WriteLine("Including file: " + filePath);
                localFiles.Add(filePath);
            }
            catch (ApplicationException ex)
            {
                // Download failed. This may be ok, for example if the
                // request includes data beyond the bounds of the dataset.
                Console.WriteLine(ex.Message);
            }
        }

        // Read data using GDAL
        GdalDataSource gdalSource = new GdalDataSource(localFiles, ScaleFactor);
        RasterData data = await gdalSource.GetData(boundsWgs84);
        return data;
    }

    public override Task<RasterData> GetData(Bounds boundsWgs84, string destSrs)
    {
        throw new NotImplementedException();
    }


    public IEnumerable<string> GetDownloadUrls(Bounds boundsWgs84)
    {
        /*
         * Example URL:
         * https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/13/TIFF/current/n47w084/USGS_13_n47w084.tif 
         * For x: [-84,-83], y: [46,47]
         */

        string urlTemplate =
            "https://prd-tnm.s3.amazonaws.com/StagedProducts/Elevation/13/TIFF/current/{0}/USGS_13_{0}.tif";

        int fromX = (int)(Math.Floor(boundsWgs84.XMin));
        int toX = (int)(Math.Ceiling(boundsWgs84.XMax));
        int fromY = (int)(Math.Floor(boundsWgs84.YMin));
        int toY = (int)(Math.Ceiling(boundsWgs84.YMax));
        for (int x = fromX; x < toX; x++)
        {
            for (int y = fromY; y < toY; y++)
            {
                // Filename coordinates are for the top left (xmin, ymax).
                // Longitude is inverted since it's denoted W.
                string filenameBase = $"n{y+1:D2}w{-x:D3}";
                yield return string.Format(urlTemplate, filenameBase);
            }
        }
    }
}
