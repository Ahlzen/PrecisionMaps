using BenchmarkDotNet.Attributes;
using MapLib.ColorSpace;
using MapLib.DataSources.Raster;
using MapLib.RasterOps;
using OSGeo.GDAL;
using System.Drawing;

namespace MapLib.Tests.RasterOps;

[TestFixture]
public class GaussianFixture : BaseFixture
{
    [Test]
    [TestCase(0)]
    [TestCase(0.5f)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    [TestCase(20)]
    [TestCase(200)]
    public void TestGaussianBlur(float radius)
    {
        ImageRasterData blurredImage = GetSingleBandTestImage()
            .GaussianBlur(radius)
            .ToImageRasterData();
        SaveTempBitmap(blurredImage.Bitmap, "TestGaussianBlur_" + radius, ".jpg");
    }
}

public class GaussianBenchmark : BaseBenchmark
{
    [Params(1f, 5f, 20f, 200f)]
    public float Radius;

    public int Size => (int)Radius;

    private SingleBandRasterData TestImage = null!;
    private float[] Kernel = null!;
    private float[] PaddedImageData = null!;
    int PaddedWidth, PaddedHeight;

    [GlobalSetup]
    public void Setup()
    {
        Kernel = Gaussian.CalculateGaussianKernel1D(Radius);
        TestImage = BaseFixture.GetSingleBandTestImage();
        PaddedImageData = PadAndCrop.PadExtendingEdges(TestImage,
            Size, Size, Size, Size);
        PaddedWidth = TestImage.WidthPx + 2 * Size;
        PaddedHeight = TestImage.HeightPx + 2 * Size;
    }


    [Benchmark]
    public SingleBandRasterData GaussianBlur()
        => TestImage.GaussianBlur(Radius);

    [Benchmark]
    public float[] Apply1DKernelVertical()
    {
        return KernelOps.Apply1DKernelVertical(PaddedImageData,
            PaddedWidth, PaddedHeight, Kernel);
    }

    [Benchmark]
    public float[] Apply1DKernelHorizontal()
    {
        return KernelOps.Apply1DKernelHorizontal(PaddedImageData,
            PaddedWidth, PaddedHeight, Kernel);
    }

    [Benchmark]
    public float[] PadExtendingEdges()
    {
        return PadAndCrop.PadExtendingEdges(TestImage, Size, Size, Size, Size);
    }

    [Benchmark]
    public float[] Crop()
    {
        return PadAndCrop.Crop(PaddedImageData, PaddedWidth, PaddedHeight,
            Size, Size, TestImage.WidthPx, TestImage.HeightPx);
    }

    /*
    
    Benchmark results (on my fairly slow laptop):

| Method                  | Radius | Mean         | Error        | StdDev       | Median       |
|------------------------ |------- |-------------:|-------------:|-------------:|-------------:|
| GaussianBlur            | 1      |   6,173.3 us |    142.65 us |    420.59 us |   6,179.6 us |
| Apply1DKernelVertical   | 1      |   2,015.4 us |    169.03 us |    498.40 us |   1,682.7 us |
| Apply1DKernelHorizontal | 1      |   1,604.5 us |     29.98 us |     23.41 us |   1,599.0 us |
| PadExtendingEdges       | 1      |     277.2 us |      5.49 us |      4.86 us |     278.6 us |
| Crop                    | 1      |     231.2 us |      4.58 us |      4.71 us |     231.3 us |
| GaussianBlur            | 5      |   8,983.8 us |     83.89 us |     74.36 us |   8,997.9 us |
| Apply1DKernelVertical   | 5      |   4,142.2 us |     79.07 us |     81.20 us |   4,132.6 us |
| Apply1DKernelHorizontal | 5      |   4,253.4 us |     72.12 us |     98.71 us |   4,215.2 us |
| PadExtendingEdges       | 5      |     286.1 us |      4.02 us |      3.35 us |     286.5 us |
| Crop                    | 5      |     230.2 us |      3.93 us |      3.48 us |     230.3 us |
| GaussianBlur            | 20     |  31,262.8 us |    551.15 us |    430.30 us |  31,186.6 us |
| Apply1DKernelVertical   | 20     |  14,959.5 us |     81.40 us |     76.14 us |  14,943.4 us |
| Apply1DKernelHorizontal | 20     |  15,299.1 us |    224.68 us |    199.17 us |  15,238.6 us |
| PadExtendingEdges       | 20     |     374.4 us |      4.98 us |      4.41 us |     374.6 us |
| Crop                    | 20     |     234.3 us |      4.58 us |      5.95 us |     235.3 us |
| GaussianBlur            | 200    | 567,962.6 us | 10,948.62 us | 11,714.90 us | 570,256.6 us |
| Apply1DKernelVertical   | 200    | 313,777.9 us |  4,203.05 us |  3,509.74 us | 313,362.8 us |
| Apply1DKernelHorizontal | 200    | 249,445.2 us |  4,885.98 us |  9,056.48 us | 250,874.3 us |
| PadExtendingEdges       | 200    |   1,472.8 us |     28.29 us |     34.75 us |   1,474.1 us |
| Crop                    | 200    |     246.5 us |      4.39 us |      3.89 us |     245.8 us |
    
     */
}