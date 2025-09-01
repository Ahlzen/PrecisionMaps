using System.IO;
using MapLib.DataSources;
using MapLib.DataSources.Raster;
using MapLib.GdalSupport;
using MapLib.Util;
using OSGeo.GDAL;

namespace MapLib.Tests.GdalSupport;

[TestFixture]
public class GdalContourGeneratorFixture : BaseFixture
{
    private string SourceFilename =>
        Path.Join(TestDataPath, "MassGIS LiDAR/be_19TCG340674.tif");

    [Test]
    public void TestGenerateContours_FromGdalDataset()
    {
        string outputPath = FileSystemHelpers.GetTempOutputFileName(
            ".shp", "contours");
        using Dataset sourceDataset = GdalUtils.OpenDataset(SourceFilename);
        GdalContourGenerator.GenerateContours(
            rasterDataset: sourceDataset,
            bandIndex: 1,
            contourInterval: 3.0,
            baseContour: 0.0,
            outputVectorPath: outputPath);
    }

    [Test]
    public async Task TestGenerateContours_FromRasterDataSource()
    {
        string outputPath = FileSystemHelpers.GetTempOutputFileName(
            ".shp", "contours");
        BaseRasterDataSource dataSource =
            new GdalDataSource(SourceFilename);
        await GdalContourGenerator.GenerateContours(
            dataSource: dataSource,
            bandIndex: 1,
            contourInterval: 3.0,
            baseContour: 0.0,
            outputVectorPath: outputPath);
    }
}