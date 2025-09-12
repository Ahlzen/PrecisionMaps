using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.RasterOps;

public static class Gaussian
{
    public static SingleBandRasterData GaussianBlur(
        this SingleBandRasterData source, float radius)
    {
        if (radius == 0) return source;

        float[] kernel = CalculateGaussianKernel1D(radius);
        int kernelRadiusPixels = (kernel.Length - 1) / 2; // excluding center pixel
        Debug.WriteLine($"Kernel {radius}: [{string.Join(",",kernel)}]");

        // Pad (need additional pixels around edges to handle kernel)
        float[] paddedSource = PadAndCrop.PadExtendingEdges(source,
            kernelRadiusPixels, kernelRadiusPixels, kernelRadiusPixels, kernelRadiusPixels);
        int paddedWidth = source.WidthPx + 2 * kernelRadiusPixels;
        int paddedHeight = source.HeightPx + 2 * kernelRadiusPixels;

        // Note: since this is a separable kernel, we do two separate 1D passes
        float[] blurred;
        blurred = KernelOps.Apply1DKernelHorizontal(
            paddedSource, paddedWidth, paddedHeight, kernel);
        blurred = KernelOps.Apply1DKernelVertical(
            blurred, paddedWidth, paddedHeight, kernel);

        // Crop (remove padding)
        float[] cropped = PadAndCrop.Crop(
            blurred, paddedWidth, paddedHeight,
            kernelRadiusPixels, kernelRadiusPixels,
            source.WidthPx, source.HeightPx);

        return source.CloneWithNewData(cropped);
    }

    public static SingleBandRasterData GaussianSharpen(
        this SingleBandRasterData source, float radius, float amount)
    {
        // This is basically a simple "unsharp mask":
        // We're subtracting low frequencies from the original image,
        // thus increasing the relative high frequency content (sharp details)
        SingleBandRasterData blurred = GaussianBlur(source, radius);
        float[] unsharpMask = ArrayArithmetics.Multiply(blurred.SingleBandData, amount);
        float[] result = ArrayArithmetics.Subtract(source.SingleBandData, unsharpMask);
        result = ArrayArithmetics.Multiply(result, 1 / (1-amount)); // Adjust gain
        return source.CloneWithNewData(result);
    }

    #region Helpers

    internal static float[] CalculateGaussianKernel1D(float radius)
    {
        if (radius <= 0)
            throw new ArgumentException("Radius must be positive.", nameof(radius));

        int kernelSize = (int)Math.Ceiling(radius * 2 + 1);
        if (kernelSize % 2 == 0) kernelSize += 1; // make odd
        int kernelRadiusPixels = (kernelSize - 1) / 2; // excluding center pixel
        float sigma = radius / 2.5f; // 2.5 stddev each side; pretty close to 0 at that point

        float[] kernel = new float[kernelSize];
        float twoSigmaSq = 2f * sigma * sigma;
        for (int i = 0; i < kernelSize; i++)
        {
            int x = i - (kernelSize/2);
            kernel[i] = (float)Math.Exp(-(x * x) / twoSigmaSq);
        }
        NormalizeKernel1D(kernel);
        return kernel;
    }

    internal static float[] NormalizeKernel1D(float[] kernel)
    {
        float sum = kernel.Sum();
        if (sum == 0) return kernel; // avoid division by zero
        for (int i = 0; i < kernel.Length; i++)
            kernel[i] /= sum;
        return kernel;
    }

    #endregion
}
