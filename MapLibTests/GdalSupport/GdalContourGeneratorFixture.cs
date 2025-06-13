using System.IO;
using MapLib.GdalSupport;
using MapLib.Util;
using OSGeo.GDAL;

namespace MapLib.Tests.GdalSupport;

[TestFixture]
public class GdalContourGeneratorFixture : BaseFixture
{
    [Test]
    [Explicit]
    public void TestGenerateContours()
    {
        string outputPath = FileSystemHelpers.GetTempOutputFileName(
            ".shp", "contours");
        string sourceFilename = Path.Join(TestDataPath, "MassGIS LiDAR/be_19TCG340674.tif");
        using Dataset sourceDataset = GdalUtils.OpenDataset(sourceFilename);
        GdalContourGenerator.GenerateContours(
            rasterDataset: sourceDataset,
            bandIndex: 1,
            contourInterval: 3.0,
            baseContour: 0.0,
            outputVectorPath: outputPath);
    }
}