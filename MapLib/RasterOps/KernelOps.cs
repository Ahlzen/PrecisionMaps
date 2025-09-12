using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.RasterOps;

internal class KernelOps
{
    public static float[] Apply1DKernelVertical(
        float[] srcData, int imageWidth, int imageHeight, float[] kernel)
    {
        if ((kernel.Length % 2) != 1)
            throw new ArgumentException("Kernel length must be odd", nameof(kernel));
        
        int kernelRadius = (kernel.Length - 1) / 2;
        float[] destData = new float[srcData.Length];
        for (int y = kernelRadius; y < imageHeight-kernelRadius; y++)
        {
            for (int x = 0; x < imageWidth; x++)
            {
                float sum = 0;
                for (int offset = -kernelRadius; offset <= kernelRadius; offset++)
                {
                    int srcY = y + offset;
                    sum += srcData[srcY * imageWidth + x] * kernel[kernelRadius + offset];
                }
                destData[y * imageWidth + x] = sum;
            }
        }
        return destData;
    }
    public static float[] Apply1DKernelHorizontal(
        float[] srcData, int imageWidth, int imageHeight, float[] kernel)
    {
        if ((kernel.Length % 2) != 1)
            throw new ArgumentException("Kernel length must be odd", nameof(kernel));
        
        int kernelRadius = (kernel.Length - 1) / 2;
        float[] destData = new float[srcData.Length];
        for (int x = kernelRadius; x < imageWidth-kernelRadius; x++)
        {
            for (int y = 0; y < imageHeight; y++)
            {
                float sum = 0;
                for (int offset = -kernelRadius; offset <= kernelRadius; offset++)
                {
                    int srcX = x + offset;
                    sum += srcData[y * imageWidth + srcX] * kernel[kernelRadius + offset];
                }
                destData[y * imageWidth + x] = sum;
            }
        }
        return destData;
    }

    public static float[] Apply1DKernelVertical_v2(
        float[] srcData, int imageWidth, int imageHeight, float[] kernel)
    {
        if ((kernel.Length % 2) != 1)
            throw new ArgumentException("Kernel length must be odd", nameof(kernel));
        float[] destData = new float[srcData.Length];
        int kernelRadius = (kernel.Length - 1) / 2;
        long overlapWindowLength = srcData.Length - imageWidth * kernel.Length;
        for (int kernelSample = 0; kernelSample < kernel.Length; kernelSample++)
        {
            float weight = kernel[kernelSample];
            long sourceIndex = kernelSample * imageWidth;
            long destIndex = kernelRadius * imageWidth;
            for (long i = 0; i < overlapWindowLength; i++)
                destData[destIndex++] += srcData[sourceIndex++] * weight;
        }
        return destData;
    }
    public static float[] Apply1DKernelHorizontal_v2(
        float[] srcData, int imageWidth, int imageHeight, float[] kernel)
    {
        if ((kernel.Length % 2) != 1)
            throw new ArgumentException("Kernel length must be odd", nameof(kernel));
        float[] destData = new float[srcData.Length];
        int kernelRadius = (kernel.Length - 1) / 2;
        int overlapRowLength = imageWidth - kernel.Length;
        for (int kernelSample = 0; kernelSample < kernel.Length; kernelSample++)
        {
            float weight = kernel[kernelSample];
            int sourceIndex = kernelSample;
            int destIndex = kernelRadius;
            for (int row = 0; row < imageHeight; row++)
            {
                for (int i = 0; i < overlapRowLength; i++)
                    destData[destIndex++] += srcData[sourceIndex++] * weight;
                sourceIndex += kernel.Length; // next row
                destIndex += kernel.Length; // next row
            }
        }
        return destData;
    }

    public static void Apply2DKernel(
        float[] data, int imageWidth, int imageHeight, float[,] kernel)
    {
        if ((kernel.GetLength(0) % 2) != 1 || (kernel.GetLength(1) % 1) != 1)
            throw new ArgumentException("Kernel length must be odd", nameof(kernel));

        throw new NotImplementedException();
    }
}
