using MapLib.DataSources;
using MapLib.Geometry;
using OSGeo.GDAL;
using OSGeo.OGR;
using System.Threading.Tasks;
using Driver = OSGeo.OGR.Driver;

namespace MapLib.GdalSupport;

public static class GdalContourGenerator
{
    /// <summary>
    /// Generates contour lines from a GDAL raster dataset and writes
    /// them to a vector layer.
    /// </summary>
    /// <param name="rasterDataset">Input raster dataset</param>
    /// <param name="bandIndex">Index of the band to use (1-based)</param>
    /// <param name="contourInterval">Contour interval</param>
    /// <param name="baseContour">Base contour value</param>
    /// <param name="outputVectorPath">Output vector file path (e.g., shapefile).</param>
    public static void GenerateContours(
        Dataset rasterDataset,
        int bandIndex,
        double contourInterval,
        double baseContour,
        string outputVectorPath)
    {
        // Create the output data source (e.g., ESRI Shapefile)
        string driverName = "ESRI Shapefile";
        using Driver ogrDriver = Ogr.GetDriverByName(driverName);
        if (ogrDriver == null)
            throw new Exception($"OGR driver {driverName} not available.");

        using DataSource vectorDataSource = ogrDriver.CreateDataSource(
            outputVectorPath, null);
        if (vectorDataSource == null)
            throw new Exception("Failed to create output vector data source.");

        // Create the output layer
        using Layer contourLayer = vectorDataSource.CreateLayer(
            name: "contours",
            srs: rasterDataset.GetSpatialRef(),
            geom_type: wkbGeometryType.wkbLineString,
            options: null);

        // Add fields for ID and elevation
        FieldDefn idField = new FieldDefn("ID", FieldType.OFTInteger);
        contourLayer.CreateField(idField, 1);

        FieldDefn elevField = new FieldDefn("ELEV", FieldType.OFTReal);
        contourLayer.CreateField(elevField, 1);

        // Get the raster band
        Band band = rasterDataset.GetRasterBand(bandIndex);

        // Generate contours
        Gdal.ContourGenerate(
            band,
            contourInterval,
            baseContour,
            fixedLevelCount: 0,
            fixedLevels: null,
            useNoData: 0,
            noDataValue: 0,
            dstLayer: contourLayer,
            idField: 0,    // ID field index
            elevField: 1,     // Elevation field index
            callback: null,
            callback_data: null);
    }

    /// <see cref="GenerateContours(BaseRasterDataSource2, int, double, double, string)"/>
    public static async Task GenerateContours(
        BaseRasterDataSource2 dataSource,
        int bandIndex,
        double contourInterval,
        double baseContour,
        string outputVectorPath,
        Bounds? boundsWgs84 = null)
    {
        RasterData2 rasterData;
        if (boundsWgs84 != null)
            rasterData = await dataSource.GetData(boundsWgs84.Value);
        else
            rasterData = await dataSource.GetData();

        if (rasterData is SingleBandRasterData singleBandRasterData)
        {
            float[] rawData = singleBandRasterData.SingleBandData;
            using Dataset rasterDataset = GdalUtils.CreateInMemoryDataset(
                rawData, singleBandRasterData.WidthPx, singleBandRasterData.HeightPx,
                singleBandRasterData.GetGeoTransform(),
                singleBandRasterData.Srs,
                singleBandRasterData.NoDataValue);
            GenerateContours(rasterDataset,
                bandIndex, contourInterval, baseContour, outputVectorPath);
        }
        else
        {
            // TODO: implement?
            throw new NotImplementedException(
                "Contour generation is only supported for single-band raster data.");
        }
    }
}