using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.RasterOps;

public static class Gaussian
{
    public static SingleBandRasterData GaussianBlur(
        this SingleBandRasterData source, float radius)
    {
        if (radius == 0) return source;

        int kernelSize = (int)Math.Ceiling(radius*2+1);
        if (kernelSize % 2 == 0) kernelSize += 1; // make odd
        int kernelRadiusPixels = (kernelSize - 1) / 2; // excluding center pixel
        float sigma = radius / 3f; // 3 stddev each side (almost 0 at that point)
        float[] kernel = CalculateGaussianKernel1D(kernelSize, sigma);

        Debug.WriteLine($"Kernel {radius}: [{string.Join(",",kernel)}]");

        // Pad (need additional pixels around edges to handle kernel)
        float[] paddedSource = PadAndCrop.PadExtendingEdges(source,
            kernelRadiusPixels, kernelRadiusPixels, kernelRadiusPixels, kernelRadiusPixels);
        int paddedWidth = source.WidthPx + 2 * kernelRadiusPixels;
        int paddedHeight = source.HeightPx + 2 * kernelRadiusPixels;

        // Note: since this is a separable kernel, we do two separate 1D passes
        float[] blurred;
        blurred = KernelOp.Apply1DKernelHorizontal(
            paddedSource, paddedWidth, paddedHeight, kernel);
        blurred = KernelOp.Apply1DKernelVertical(
            blurred, paddedWidth, paddedHeight, kernel);

        // Crop (remove padding)
        float[] cropped = PadAndCrop.Crop(
            blurred, paddedWidth, paddedHeight,
            kernelRadiusPixels, kernelRadiusPixels,
            source.WidthPx, source.HeightPx);

        return source.CloneWithNewData(cropped);
    }

    private static float[] CalculateGaussianKernel1D(int kernelSize, float? sigma = null)
    {
        if (kernelSize % 2 == 0)
            throw new ArgumentException("Kernel size must be odd.", nameof(kernelSize));
        if (sigma <= 0)
            throw new ArgumentException("Sigma must be positive.", nameof(sigma));

        // default to three sigma per side (six total)
        sigma ??= (kernelSize - 1) / 6f;

        float[] kernel = new float[kernelSize];
        int radius = kernelSize / 2;
        float sum = 0f;
        float twoSigmaSq = 2f * sigma.Value * sigma.Value;

        for (int i = 0; i < kernelSize; i++)
        {
            int x = i - radius;
            kernel[i] = (float)Math.Exp(-(x * x) / twoSigmaSq);
            sum += kernel[i];
        }
        NormalizeKernel1D(kernel);
        return kernel;
    }

    private static float[] NormalizeKernel1D(float[] kernel)
    {
        float sum = kernel.Sum();
        if (sum == 0) return kernel; // avoid division by zero
        for (int i = 0; i < kernel.Length; i++)
            kernel[i] /= sum;
        return kernel;
    }

}
