﻿using MapLib.DataSources.Raster;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;
using MapLib.GdalSupport;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace MapLib.Tests.DataSources;

[TestFixture]
public class GdalDataSourceFixture : BaseFixture
{
    [Test]
    public void TestReadGdalRaster()
    {
        GdalDataSource dataSource = new(Path.Join(TestDataPath, "MassGIS LiDAR/be_19TCG339672.tif"));
        Console.WriteLine("SRS: " + dataSource.Srs);
        Console.WriteLine("Bounds: " + dataSource.Bounds);
        Console.WriteLine("Bounds (WGS84): " + dataSource.BoundsWgs84);
        Console.WriteLine("Dimensions: " + dataSource.WidthPx + " x " + dataSource.HeightPx);

        RasterData data = dataSource.GetData();
        Console.WriteLine("Bitmap PixelFormat: " + data.Bitmap.PixelFormat);

        Assert.That(data.Bitmap, Is.Not.Null);
        Assert.That(data.Bitmap.Width, Is.EqualTo(dataSource.WidthPx));
        Assert.That(data.Bitmap.Height, Is.EqualTo(dataSource.HeightPx));
    }

    [Test]
    public void TestTransform()
    {
        string sourceFilename = Path.Join(
            TestDataPath, "MassGIS LiDAR/be_19TCG339672.tif");

        // Source is "EPSG:6348 - NAD83(2011) / UTM zone 19N"
        // Transform to EPSG:3857
        string destFilename = GdalDataSource.Transform(sourceFilename, "EPSG:3857");

        Console.WriteLine(destFilename);

        using Dataset destDataset = GdalUtils.GetRasterDataset(destFilename);
        SpatialReference sr = GdalUtils.GetSpatialReference(destDataset);

        // Check that dest is indeed 3857
        Assert.That(sr.AutoIdentifyEPSG(), Is.EqualTo(3857));
    }
}
