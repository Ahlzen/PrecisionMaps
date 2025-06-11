using System.Drawing.Imaging;
using MapLib.DataSources.Raster;
using MapLib.GdalSupport;
using OSGeo.GDAL;
using System.IO;
using MapLib.RasterOps;
using MapLib.Util;

namespace MapLib.Tests.DataSources;

[TestFixture]
public class GdalDataSource2Fixture : BaseFixture
{
    [Test]
    [TestCase("MassGIS Aerial/19TCG390725.jp2")] // RGB imagery
    [TestCase("USGS Topo Quad 25k/q249882.tif")] // indexed
    public async void TestSaveAsPng(string filename)
    {
        filename = Path.Join(TestDataPath, filename);
        GdalDataSource2 ds = new(filename);
        RasterData2 data = await ds.GetData();
        Assert.That(data, Is.TypeOf<ImageRasterData>());
        var imageData = data as ImageRasterData;
        Assert.That(imageData, Is.Not.Null);
        string outFilename = FileSystemHelpers.GetTempOutputFileName(
            ".png", Path.GetFileNameWithoutExtension(filename) + "_converted_");
        imageData!.Bitmap.Save(outFilename);
    }

    [Test]
    [TestCase("MassGIS Aerial/19TCG390725.jp2")] // RGB imagery
    [TestCase("USGS Topo Quad 25k/q249882.tif")] // indexed
    public async void TestWarpAndSaveAsPng(string filename)
    {
        filename = Path.Join(TestDataPath, filename);
        string warpedFilename = GdalUtils.Warp(filename, GdalSupport.Transformer.WktWebMercator);
        GdalDataSource2 ds = new(warpedFilename);
        RasterData2 data = await ds.GetData();
        Assert.That(data, Is.TypeOf<ImageRasterData>());
        var imageData = data as ImageRasterData;
        Assert.That(imageData, Is.Not.Null);
        string outFilename = FileSystemHelpers.GetTempOutputFileName(
            ".png", Path.GetFileNameWithoutExtension(filename) + "_warped_");
        Console.WriteLine(imageData!.Bitmap.PixelFormat);
        imageData!.Bitmap.Save(outFilename, ImageFormat.Png);
    }

    [Test]
    [TestCase("MassGIS LiDAR/be_19TCG339672.tif")] // Float32 DEM
    [TestCase("USGS NED/USGS_OPR_MA_CentralEastern_2021_B21_be_19TCG339672.tif")]
    public async void TestGenerateHillshade(string filename)
    {
        filename = Path.Join(TestDataPath, filename);
        
        // Load DEM (single-band raster)
        GdalDataSource2 ds = new(filename);
        SingleBandRasterData? dem = await ds.GetData() as SingleBandRasterData;
        Assert.That(dem, Is.Not.Null);

        // Save DEM and hillshade as images
        string demFilename = FileSystemHelpers.GetTempOutputFileName(
            ".png", Path.GetFileNameWithoutExtension(filename) + "_dem_");
        dem!
            .ToImageRasterData(normalize: true)
            .Bitmap.Save(demFilename);

        string hillshadeFilename = FileSystemHelpers.GetTempOutputFileName(
            ".png", Path.GetFileNameWithoutExtension(filename) + "_hillshade_");
        dem!
            .Hillshade_Basic() // appx [-3, 5]
            .Clamp(-1.5f, 1.5f) // cut top and bottom to increase contrast a bit
            .Offset(1.5f)
            .Scale(255/3f)
            .ToImageRasterData(normalize: false)
            .Bitmap.Save(hillshadeFilename);
    }

    [Test]
    [TestCase("MassGIS LiDAR/be_19TCG339672.tif")] // Float32 DEM
    [TestCase("MassGIS Aerial/19TCG390725.jp2")] // RGB imagery
    [TestCase("MassGIS Impervious Surface/imp_ne6.img")]
    [TestCase("USGS Topo Quad 25k/q249882.tif")]
    [TestCase("USGS NED/USGS_OPR_MA_CentralEastern_2021_B21_be_19TCG339672.tif")]
    public void TestGetSummaryAndRasterBandConfiguration(string filename)
    {
        filename = Path.Join(TestDataPath, filename);

        Console.WriteLine(filename);
        using (Dataset dataset = GdalUtils.OpenDataset(filename))
        {
            Console.Write(GdalUtils.GetRasterBandSummary(dataset));
        }
        Console.WriteLine();

        // Test that reading the data completes successfully
        GdalDataSource2 ds = new(filename);
        _ = ds.GetData();
        
        // Warp (reproject) it and show summary again
        string warpedFilename = GdalUtils.Warp(filename, GdalSupport.Transformer.WktWebMercator);
        Console.Write("Warped: " + warpedFilename);
        using (Dataset warpedDataset = GdalUtils.OpenDataset(warpedFilename))
        {
            Console.Write(GdalUtils.GetRasterBandSummary(warpedDataset));
        }
        Console.WriteLine();

        // Test that reading the data completes successfully
        GdalDataSource2 dsWarped = new(warpedFilename);
        _ = dsWarped.GetData();

        /*
        Example raster band configuration for different image types:

        MassGIS LiDAR/be_19TCG339672.tif
        Size: 3000x3000 px
        Raster count: 1
        Band 1
         DataType: GDT_Float32
         Size: 32
         Interpretation: GCI_GrayIndex
        
        MassGIS Aerial/19TCG390725.jp2
        Size: 10000x10000 px
        Raster count: 4
        Band 1
         DataType: GDT_Byte
         Size: 8
         Interpretation: GCI_RedBand
        Band 2
         DataType: GDT_Byte
         Size: 8
         Interpretation: GCI_GreenBand
        Band 3
         DataType: GDT_Byte
         Size: 8
         Interpretation: GCI_BlueBand
        Band 4
         DataType: GDT_Byte
         Size: 8
         Interpretation: GCI_Undefined

        MassGIS Impervious Surface/imp_ne6.img
        Size: 40000x24000 px
        Raster count: 1
        Band 1
         DataType: GDT_Byte
         Size: 8
         Interpretation: GCI_Undefined

        USGS NED/USGS_OPR_MA_CentralEastern_2021_B21_be_19TCG339672.tif
        Size: 3000x3000 px
        Raster count: 1
        Band 1
         DataType: GDT_Float32
         Size: 32
         Interpretation: GCI_GrayIndex

        USGS Topo Quad 25k/q249882.tif
        Size: 1570x1570 px
        Raster count: 1
        Band 1
         DataType: GDT_Byte
         Size: 8
         Interpretation: GCI_PaletteIndex
        */
    }
}
 