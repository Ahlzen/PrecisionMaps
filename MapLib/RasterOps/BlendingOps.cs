﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.RasterOps;

public enum BlendMode
{
    Normal, // top-over-bottom (alpha composited)
    Dissolve,
    Multiply, // darken
    Screen, // lighten
    Overlay, // lighten and darken
    HardLight,
    SoftLight,
}

/// <summary>
/// Methods for compositing and blending two images.
/// </summary>
/// <remarks>
/// References:
/// CSS Compositing and Blending: https://drafts.fxtf.org/compositing/
/// Blend modes: https://en.wikipedia.org/wiki/Blend_modes
/// </remarks>
public static class BlendingOps
{
    private static float ByteToFloatScale = 1f / 255f;
    private static float FloatToByteScale = 255f / 1f;

    public static ImageRasterData Blend(
        this ImageRasterData bottomLayer,
        ImageRasterData topLayer,
        BlendMode blendMode = BlendMode.Normal,
        float strength = 1.0f)
    {
        long pixelCount = bottomLayer.WidthPx * bottomLayer.HeightPx;
        if (bottomLayer.WidthPx != topLayer.WidthPx ||
            bottomLayer.HeightPx != topLayer.HeightPx)
            throw new InvalidOperationException("Blend: Both source images must be the same size");

        (float[] r, float[] g, float[] b, float[] a) bottom = SplitChannels(bottomLayer.ImageData);
        (float[] r, float[] g, float[] b, float[] a) top = SplitChannels(topLayer.ImageData);
        (float[] r, float[] g, float[] b, float[] a) dest = (
            new float[pixelCount],
            new float[pixelCount],
            new float[pixelCount],
            new float[pixelCount]);

        if (strength < 1.0f)
            for (int i = 0; i < pixelCount; i++)
                top.a[i] *= strength;

        switch (blendMode)
        {
            case BlendMode.Normal:
                BlendAndComposite(top, bottom, dest, BlendNormal);
                break;
            //case BlendMode.Dissolve:
            //    break;
            case BlendMode.Multiply:
                BlendAndComposite(top, bottom, dest, BlendMultiply);
                break;
            //case BlendMode.Screen:
            //    break;
            //case BlendMode.Overlay:
            //    break;
            //case BlendMode.HardLight:
            //    break;
            //case BlendMode.SoftLight:
            //    break;
            default:
                throw new NotImplementedException(
                    "Unsupported blend mode: " + blendMode);
        }

        byte[] blendedImageData = MergeChannels(dest.r, dest.g, dest.b, dest.a);
        return bottomLayer.CloneWithNewData(blendedImageData);
    }

    private static (float[] r, float[] g, float[] b, float[] a)
        SplitChannels(byte[] imageData) => (
            GetChannel(imageData, 2),
            GetChannel(imageData, 1),
            GetChannel(imageData, 0),
            GetChannel(imageData, 3));

    private static float[] GetChannel(byte[] imageData, int byteOffset)
    {
        int pixelCount = imageData.Length / 4;
        float[] channelData = new float[pixelCount];
        for (int p = 0; p < pixelCount; p++)
        {
            byte sourceData = imageData[p * 4 + byteOffset];
            channelData[p] = ByteToFloatScale * sourceData;
        }
        return channelData;
    }

    private static byte[] MergeChannels(float[] r, float[] g, float[] b, float[] a) {
        byte[] dest = new byte[r.Length * 4];
        SetChannel(dest, 2, r);
        SetChannel(dest, 1, g);
        SetChannel(dest, 0, b);
        SetChannel(dest, 3, a);
        return dest;
    }
    
    private static void SetChannel(byte[] dest, int byteOffset, float[] channelData)
    {
        int pixelCount = channelData.Length;
        for (int p = 0; p < pixelCount; p++)
        {
            int destOffset = byteOffset + p * 4;
            dest[destOffset] = (byte)(channelData[p] * FloatToByteScale);
        }
    }

    private static void BlendAndComposite(
        (float[] r, float[] g, float[] b, float[] a) top,
        (float[] r, float[] g, float[] b, float[] a) bottom,
        (float[] r, float[] g, float[] b, float[] a) dest,
        Action<float[], float[], float[]> blendFunction)
    {
        // compute color using blend function
        blendFunction(top.r, bottom.r, dest.r);
        blendFunction(top.g, bottom.g, dest.g);
        blendFunction(top.b, bottom.b, dest.b);

        // composite color channels
        CompositeColorChannel(dest.r, top.a, bottom.r, bottom.a, dest.r);
        CompositeColorChannel(dest.g, top.a, bottom.g, bottom.a, dest.g);
        CompositeColorChannel(dest.b, top.a, bottom.b, bottom.a, dest.b);

        // composite alpha channel
        CompositeAlphaChannel(top.a, bottom.a, dest.a);
    }

    private static void BlendNormal(float[] top, float[] bottom, float[] dest)
    {
        for (int i = 0; i < top.Length; i++)
            dest[i] = top[i];
    }

    private static void BlendMultiply(float[] top, float[] bottom, float[] dest)
    {
        for (int i = 0; i < top.Length; i++)
            dest[i] = top[i] * bottom[i];
    }

    private static void CompositeColorChannel(float[] color, float[] topA, float[] background, float[] bottomA, float[] dest)
    {
        for (int i = 0; i < color.Length; i++)
            dest[i] = color[i] * topA[i] + background[i] * bottomA[i] * (1 - topA[i]);
    }

    private static void CompositeAlphaChannel(float[] topA, float[] bottomA, float[] dest)
    {
        for (int i = 0; i < topA.Length; i++)
            dest[i] = topA[i] + bottomA[i] * (1 - topA[i]);
    }
}

