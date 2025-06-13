using OSGeo.GDAL;
using OSGeo.OGR;
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
        // Register all OGR drivers
        Ogr.RegisterAll();

        // Create the output data source (e.g., ESRI Shapefile)
        string driverName = "ESRI Shapefile";
        Driver ogrDriver = Ogr.GetDriverByName(driverName);
        if (ogrDriver == null)
            throw new Exception($"OGR driver {driverName} not available.");

        DataSource vectorDataSource = ogrDriver.CreateDataSource(outputVectorPath, null);
        if (vectorDataSource == null)
            throw new Exception("Failed to create output vector data source.");

        // Create the output layer
        Layer contourLayer = vectorDataSource.CreateLayer(
            "contours", null, wkbGeometryType.wkbLineString, null);

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

        // Cleanup
        contourLayer.Dispose();
        vectorDataSource.Dispose();
    }
}