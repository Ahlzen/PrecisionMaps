using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.RasterOps;

public static class Gaussian
{
    public static SingleBandRasterData GaussianBlur(
        this SingleBandRasterData source, float radius)
    {
        // TEST KERNEL
        // box blur only
        // TODO: implement true Gaussian kernel
        int kernelSize = (int)Math.Ceiling(radius*2+1);
        if (kernelSize % 2 == 0) kernelSize += 1; // make odd
        int kernelRadiusPixels = (kernelSize - 1) / 2; // excluding center pixel

        float[] kernel = new float[kernelSize];
        Array.Fill(kernel, 1.0f / kernelSize);

        // Pad (need additional pixels around edges to handle kernel)
        float[] paddedSource = PadAndCrop.PadExtendingEdges(
            source, kernelRadiusPixels, kernelRadiusPixels, kernelRadiusPixels, kernelRadiusPixels);
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
}
