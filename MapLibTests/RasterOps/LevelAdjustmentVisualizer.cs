using System.Drawing;
using MapLib.RasterOps;
using System.Drawing.Imaging;

namespace MapLib.Tests.RasterOps;

internal class LevelAdjustmentVisualizer
{
    private List<(string title, LevelAdjustment adjustment)> Items { get; } = new();

    private int GradientWidth => 512;

    private int GradientHeight => 64;

    private int GraphWidth => 128;
    private int GraphHeight => 128;

    private float TextSizePt => 8f;

    private int Margin => 30;

    private readonly float[] _graphInputData;
    private readonly float[] _gradientInputData;
    private readonly Bitmap _referenceGradient;

    public LevelAdjustmentVisualizer()
    {
        // Compute input data and reference (input) gradient
        _graphInputData = new float[GraphWidth];
        for (int i = 0; i < GraphWidth; i++)
            _graphInputData[i] = (float)i / (GraphWidth - 1);
        _gradientInputData = new float[GradientWidth];
        for (int i = 0; i < GradientWidth; i++)
            _gradientInputData[i] = (float)i / (GradientWidth - 1);
        _referenceGradient = GenerateColorGradient(LevelAdjustment.Identity());
    }

    public void Add(string title,  LevelAdjustment levelAdjustment)
        => Items.Add((title, levelAdjustment));

    public Bitmap Render()
    {
        int width = Margin * 3 + GradientWidth + GraphWidth;
        int height = Items.Count * (2 * GradientHeight + Margin) + Margin;

        Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);
        using Font font = new Font("Calibri", TextSizePt);
        using Brush white = new SolidBrush(Color.White);
        using Graphics g = Graphics.FromImage(bitmap);
        g.Clear(Color.FromArgb(63, 63, 63));
        for (int n = 0; n < Items.Count; n++)
        {
            int yOffset = Margin + (n * (2 * GradientHeight + Margin));
            using Bitmap resultGradient = GenerateColorGradient(Items[n].adjustment);
            using Bitmap graph = GenerateTransferFunctionGraph(Items[n].adjustment);

            // Draw bitmaps and label
            g.DrawImageUnscaled(_referenceGradient, Margin, yOffset);
            g.DrawImageUnscaled(resultGradient, Margin, yOffset+GradientHeight+1);
            g.DrawImageUnscaled(graph, Margin * 2 + GradientWidth, yOffset);
            g.DrawString(Items[n].title, font, white,
                new PointF(Margin + GradientWidth, yOffset + 2f * GradientHeight + TextSizePt * 0.7f),
                new StringFormat() { Alignment = StringAlignment.Far });
        }
        return bitmap;
    }

    private Bitmap GenerateColorGradient(LevelAdjustment levelAdjustment)
    {
        // NOTE: This is pretty inefficient... but perf
        // doesn't matter a lot here.

        float[] output = levelAdjustment.Apply(_gradientInputData);
        Bitmap bitmap = new(GradientWidth, GradientHeight, PixelFormat.Format32bppArgb);
        for (int x = 0; x < GradientWidth; x++)
        {
            float scaledValue = Math.Clamp(output[x] * 255f, 0, 255);
            byte value = (byte)scaledValue;
            Color c = Color.FromArgb(value, value, value);
            for (int y = 0; y < GradientHeight; y++)
                bitmap.SetPixel(x, y, c);
        }
        return bitmap;
    }

    private Bitmap GenerateTransferFunctionGraph(LevelAdjustment levelAdjustment)
    {
        float[] output = levelAdjustment.Apply(_graphInputData);
        Bitmap bitmap = new(GraphWidth, GraphHeight, PixelFormat.Format32bppArgb);
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        using Brush background = new SolidBrush(Color.FromArgb(31, 31, 31));
        using Pen axisPen = new Pen(Color.FromArgb(128, 128, 128), 1.0f);
        using Pen gridPen = new Pen(Color.FromArgb(64, 64, 64), 1.0f);
        using Pen graphPen = new Pen(Color.FromArgb(192, 192, 192), 0.8f);
        
        // Draw axes and grid
        g.Clear(Color.FromArgb(31, 31, 31));
        g.DrawLine(gridPen, 0, GraphHeight/2, GraphWidth, GraphHeight/2);
        g.DrawLine(gridPen, GraphWidth / 2, 0, GraphWidth / 2, GraphHeight);
        g.DrawLine(gridPen, 0, GraphHeight, GraphWidth, 0);
        g.DrawRectangle(axisPen, new RectangleF(0, 0, GraphWidth-1, GraphHeight-1));
        
        float lastValue = Math.Clamp(output[0] * (GraphHeight - 2), 0, GraphHeight - 2);
        for (int x = 1; x < GraphWidth; x++)
        {
            float currentValue = Math.Clamp(output[x] * (GraphHeight-2), 0, GraphHeight-2);
            g.DrawLine(graphPen,
                x - 1, GraphHeight - 1 - lastValue,
                x, GraphHeight - 1 - currentValue);
            lastValue = currentValue;
        }
        return bitmap;
    }
}
