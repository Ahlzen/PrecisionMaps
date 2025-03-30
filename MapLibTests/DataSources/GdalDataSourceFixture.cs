using MapLib.DataSources.Raster;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing.Imaging;

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
}
