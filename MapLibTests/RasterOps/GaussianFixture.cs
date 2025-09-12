using BenchmarkDotNet.Attributes;
using MapLib.ColorSpace;
using MapLib.DataSources.Raster;
using MapLib.RasterOps;
using OSGeo.GDAL;
using System.Diagnostics.Contracts;
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

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(50)]
    public void TestGaussianSharpen(float radius)
    {
        ImageRasterData sharpenedImage = GetSingleBandTestImage()
            .GaussianSharpen(radius, 0.5f)
            .ToImageRasterData();
        SaveTempBitmap(sharpenedImage.Bitmap, "TestGaussianSharpen_" + radius, ".jpg");
    }

    [Test]
    public void TestMultiSharpen()
    {
        SingleBandRasterData sharpenedImage = GetSingleBandTestImage();

        sharpenedImage = sharpenedImage.GaussianSharpen(2, 0.4f);
        sharpenedImage = sharpenedImage.GaussianSharpen(5, 0.15f);
        sharpenedImage = sharpenedImage.GaussianSharpen(15, 0.05f);
        SaveTempBitmap(sharpenedImage.ToImageRasterData().Bitmap, "TestMultiSharpen.jpg");
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
    public float[] Apply1DKernelVertical_v2()
    {
        return KernelOps.Apply1DKernelVertical_v2(PaddedImageData,
            PaddedWidth, PaddedHeight, Kernel);
    }

    [Benchmark]
    public float[] Apply1DKernelHorizontal()
    {
        return KernelOps.Apply1DKernelHorizontal(PaddedImageData,
            PaddedWidth, PaddedHeight, Kernel);
    }

    [Benchmark]
    public float[] Apply1DKernelHorizontal_v2()
    {
        return KernelOps.Apply1DKernelHorizontal_v2(PaddedImageData,
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

| Method                     | Radius | Mean         | Error        | StdDev       | Median       |
|--------------------------- |------- |-------------:|-------------:|-------------:|-------------:|
| GaussianBlur               | 1      |   5,285.7 us |    131.12 us |    386.62 us |   5,317.7 us |
| Apply1DKernelVertical      | 1      |   1,706.4 us |    127.30 us |    375.34 us |   1,649.1 us |
| Apply1DKernelVertical_v2   | 1      |     969.4 us |     11.25 us |      9.97 us |     968.5 us |
| Apply1DKernelHorizontal    | 1      |   1,341.9 us |      9.58 us |      8.00 us |   1,342.4 us |
| Apply1DKernelHorizontal_v2 | 1      |   1,300.8 us |     11.14 us |      9.30 us |   1,303.6 us |
| PadExtendingEdges          | 1      |     245.8 us |      3.06 us |      2.71 us |     246.2 us |
| Crop                       | 1      |     201.6 us |      3.61 us |      3.02 us |     200.7 us |
| GaussianBlur               | 5      |   8,215.5 us |     97.08 us |     81.06 us |   8,209.5 us |
| Apply1DKernelVertical      | 5      |   3,675.6 us |     71.93 us |     63.77 us |   3,643.5 us |
| Apply1DKernelVertical_v2   | 5      |   3,052.5 us |     18.34 us |     17.15 us |   3,047.3 us |
| Apply1DKernelHorizontal    | 5      |   3,778.2 us |     69.09 us |     64.63 us |   3,753.3 us |
| Apply1DKernelHorizontal_v2 | 5      |   4,474.9 us |     85.26 us |     75.58 us |   4,481.7 us |
| PadExtendingEdges          | 5      |     265.9 us |      3.35 us |      2.97 us |     265.8 us |
| Crop                       | 5      |     207.3 us |      3.03 us |      2.68 us |     207.7 us |
| GaussianBlur               | 20     |  28,288.4 us |    288.19 us |    283.05 us |  28,222.1 us |
| Apply1DKernelVertical      | 20     |  13,870.3 us |    131.33 us |    122.85 us |  13,846.8 us |
| Apply1DKernelVertical_v2   | 20     |  12,503.8 us |    360.01 us |    948.42 us |  12,182.6 us |
| Apply1DKernelHorizontal    | 20     |  14,214.1 us |    281.87 us |    313.29 us |  14,199.9 us |
| Apply1DKernelHorizontal_v2 | 20     |  17,768.9 us |    163.86 us |    153.27 us |  17,726.3 us |
| PadExtendingEdges          | 20     |     365.4 us |      4.48 us |      3.97 us |     365.9 us |
| Crop                       | 20     |     209.8 us |      3.80 us |      3.73 us |     210.0 us |
| GaussianBlur               | 200    | 502,778.7 us | 10,020.19 us | 24,007.73 us | 495,254.0 us |
| Apply1DKernelVertical      | 200    | 254,521.7 us |  4,948.44 us |  5,698.63 us | 253,880.1 us |
| Apply1DKernelVertical_v2   | 200    | 300,646.2 us |  5,715.02 us |  6,115.01 us | 300,217.7 us |
| Apply1DKernelHorizontal    | 200    | 234,339.3 us |  4,672.22 us |  7,544.76 us | 236,847.8 us |
| Apply1DKernelHorizontal_v2 | 200    | 389,412.5 us |  6,840.32 us |  6,718.10 us | 390,918.2 us |
| PadExtendingEdges          | 200    |   1,367.3 us |     17.22 us |     15.27 us |   1,366.4 us |
| Crop                       | 200    |     234.5 us |      2.94 us |      2.60 us |     233.6 us |
 
     */
}