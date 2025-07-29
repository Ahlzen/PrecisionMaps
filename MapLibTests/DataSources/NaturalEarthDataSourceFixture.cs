using MapLib.DataSources.Raster;
using MapLib.DataSources.Vector;
using MapLib.Output;
using MapLib.Render;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Tests.DataSources;

/// <summary>
/// Test fixture for Natural Earth data sources
/// (both vector and raster).
/// </summary>
[TestFixture]
public class NaturalEarthDataSourceFixture : BaseFixture
{
    /// <summary>
    /// Downloads and caches all NE vector data sets (~260 MB download, ~1.1 GB on disk).
    /// </summary>
    [Test]
    [Explicit]
    public async Task DownloadAllNaturalEarthVectorData()
    {
        foreach (NaturalEarthVectorDataSet dataSet in Enum.GetValues<NaturalEarthVectorDataSet>())
        {
            try
            {
                await new NaturalEarthVectorDataSource(dataSet).GetData();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    /// <summary>
    /// Downloads, renders and saves each NE data set to a PNG file.
    /// </summary>
    [Test]
    [Explicit]
    public async Task DownloadAndRenderAllNaturalEarthVectorData()
    {
        foreach (NaturalEarthVectorDataSet dataSet in Enum.GetValues<NaturalEarthVectorDataSet>())
        {
            string dataSetName = dataSet.ToString();
            try
            {
                Console.WriteLine(dataSetName);
                NaturalEarthVectorDataSource dataSource = new(dataSet);
                VectorData data = await dataSource.GetData();

                BitmapCanvasStack stack = new BitmapCanvasStack(CanvasUnit.Mm,
                    297.0, 210.0, Color.White, 1.6);
                Map map = new(data.Bounds, data.Srs);
                map.VectorDataSources.Add(dataSetName, dataSource);
                map.MapLayers.Add(new VectorMapLayer(dataSetName, dataSetName, new VectorStyle
                {
                    FillColor = Color.Beige,
                    LineColor = Color.Black,
                    LineWidth = 0.1, // mm
                    Symbol = SymbolType.Circle,
                    SymbolColor = Color.Red,
                    SymbolSize = 0.5, // mm
                }));
                map.Render(stack, AspectRatioMismatchStrategy.CenterOnCanvas);
                SaveTempBitmap(stack.GetBitmap(), "NaturalEarth_" + dataSetName, ".png");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{dataSetName}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Downloads and caches all NE raster data sets.
    /// </summary>
    [Test]
    [Explicit]
    public async Task DownloadAllNaturalEarthRasterData()
    {
        foreach (NaturalEarthRasterDataSet dataSet in Enum.GetValues<NaturalEarthRasterDataSet>())
        {
            Console.WriteLine("Downloading " + dataSet);
            try
            {
                await new NaturalEarthRasterDataSource(dataSet).GetData();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
