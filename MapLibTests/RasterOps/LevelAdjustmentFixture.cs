using MapLib.RasterOps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Tests.RasterOps;

[TestFixture]
public class LevelAdjustmentFixture : BaseFixture
{
    private LevelAdjustmentVisualizer _visualizer = new();

    [SetUp]
    public void SetUpTest()
    {
        _visualizer = new();
    }

    [Test]
    public void TestIdentity()
    {
        _visualizer.Add("Identity", LevelAdjustment.Identity());
        SaveTempBitmap(_visualizer.Render(), "Identity");
    }

    [Test]
    public void TestScale()
    {
        _visualizer.Add("Scale 0.5", LevelAdjustment.Scale(0.5f));
        _visualizer.Add("Scale 0.8", LevelAdjustment.Scale(0.8f));
        _visualizer.Add("Scale 1.0", LevelAdjustment.Scale(1.0f));
        _visualizer.Add("Scale 1.3", LevelAdjustment.Scale(1.3f));
        _visualizer.Add("Scale 2.0", LevelAdjustment.Scale(2.0f));
        SaveTempBitmap(_visualizer.Render(), "Scale");
    }

    [Test]
    public void TestQuantize()
    {
        _visualizer.Add("Quantize 2", LevelAdjustment.Quantize(2));
        _visualizer.Add("Quantize 3", LevelAdjustment.Quantize(3));
        _visualizer.Add("Quantize 8", LevelAdjustment.Quantize(8));
        _visualizer.Add("Quantize 16", LevelAdjustment.Quantize(16));
        _visualizer.Add("Quantize 64", LevelAdjustment.Quantize(64));
        SaveTempBitmap(_visualizer.Render(), "Quantize");
    }

    [Test]
    public void TestAdjustMidpoint()
    {
        _visualizer.Add("AdjustMidpoint 0.1", LevelAdjustment.AdjustMidpoint(0.1f));
        _visualizer.Add("AdjustMidpoint 0.3", LevelAdjustment.AdjustMidpoint(0.3f));
        _visualizer.Add("AdjustMidpoint 0.5", LevelAdjustment.AdjustMidpoint(0.5f));
        _visualizer.Add("AdjustMidpoint 0.7", LevelAdjustment.AdjustMidpoint(0.7f));
        _visualizer.Add("AdjustMidpoint 0.9", LevelAdjustment.AdjustMidpoint(0.9f));
        SaveTempBitmap(_visualizer.Render(), "AdjustMidpoint");
    }
}


