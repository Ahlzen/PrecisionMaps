using MapLib.DataSources.Raster;
using MapLib.DataSources.Vector;
using MapLib.GdalSupport;
using MapLib.Output;
using MapLib.Render;
using MapLib.Util;
using System.Collections;
using System.Drawing;

namespace MapLib.Tests.DataSources;

/// <summary>
/// Test fixture for Natural Earth data sources
/// (both vector and raster).
/// </summary>
[TestFixture]
public class NaturalEarthDataSourceFixture : BaseFixture
{
    #region Vector data sets

    /// <summary>
    /// Downloads and caches all NE vector data sets (~260 MB download, ~1.1 GB on disk).
    /// </summary>
    [Test]
    [Explicit]
    public async Task DownloadAllNaturalEarthVectorData()
    {
        foreach (NaturalEarthVectorDataSet dataSet in Enum.GetValues<NaturalEarthVectorDataSet>())
            await new NaturalEarthVectorDataSource(dataSet).Download();
    }

    /// <summary>
    /// Downloads, renders and saves each NE vector data set to a PNG file.
    /// </summary>
    [Test]
    [Explicit]
    public async Task RenderAllNaturalEarthVectorData()
    {
        foreach (NaturalEarthVectorDataSet dataSet in Enum.GetValues<NaturalEarthVectorDataSet>())
        {
            string dataSetName = dataSet.ToString();
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
            await map.Render(stack, AspectRatioMismatchStrategy.CenterOnCanvas);
            SaveTempBitmap(stack.GetBitmap(), "NaturalEarth_" + dataSetName, ".png");
        }
    }

    #endregion

    #region Raster data sets

    /// <summary>
    /// Downloads and caches all NE raster data sets.
    /// </summary>
    [Test]
    [Explicit]
    public async Task DownloadAllNaturalEarthRasterData()
    {
        foreach (NaturalEarthRasterDataSet dataSet in Enum.GetValues<NaturalEarthRasterDataSet>())
            await new NaturalEarthRasterDataSource(dataSet).Download();
    }

    /// <summary>
    /// Downloads, renders and saves each NE raster data set to a PNG file.
    /// </summary>
    [Test]
    [Explicit]
    public async Task RenderAllNaturalEarthRasterData()
    {
        foreach (NaturalEarthRasterDataSet dataSet in Enum.GetValues<NaturalEarthRasterDataSet>())
        {
            string dataSetName = dataSet.ToString();
            Console.WriteLine(dataSetName);
            NaturalEarthRasterDataSource dataSource = new(dataSet);
            RasterData data = await dataSource.GetData();

            BitmapCanvasStack stack = new BitmapCanvasStack(CanvasUnit.Mm,
                297.0, 210.0, Color.White, 1.6);
            Map map = new(data.Bounds, data.Srs);
            map.RasterDataSources.Add(dataSetName, dataSource);
            map.MapLayers.Add(new RasterMapLayer(dataSetName, dataSetName, new RasterStyle()));
            await map.Render(stack, AspectRatioMismatchStrategy.CenterOnCanvas);
            SaveTempBitmap(stack.GetBitmap(), "NaturalEarth_" + dataSetName, ".png");
        }
    }

    #endregion

    #region Real maps with NE data

    // TODO: move to base class?
    private static Color bgColor = Color.LightGray;
    public static IEnumerable<CanvasStack> A3CanvasStacks()
    {
        yield return new BitmapCanvasStack(CanvasUnit.Mm, 420.0, 297.0, bgColor, 1.0);
        yield return new BitmapCanvasStack(CanvasUnit.Mm, 420.0, 297.0, bgColor, 4.0);
        yield return new SvgCanvasStack(CanvasUnit.Mm, 420.0, 297.0, bgColor);
    }
    public static IEnumerable<Srs> WorldMapProjections()
    {
        yield return Srs.Robinson;
        yield return Srs.VanDerGrinten;
        yield return Srs.WebMercator;
    }
    public static IEnumerable NaturalEarthWorldMapParams()
    {
        foreach (Srs srs in WorldMapProjections())
            foreach (CanvasStack stack in A3CanvasStacks())
                yield return new object[] { stack, srs };
    }

    [Test]
    [Explicit]
    [TestCaseSource(nameof(NaturalEarthWorldMapParams))]
    public async Task RenderNaturalEarthWorldMap(
        CanvasStack canvasStack,
        Srs srs)
    {
        Map map = new Map(new Bounds(-180.0, 180.0, -75.0, 75.0), srs);

        // Data sources
        map.RasterDataSources.Add("neBaseRaster", new NaturalEarthRasterDataSource(
            NaturalEarthRasterDataSet.CrossBlendedHypsometricTintsWithReliefWaterDrainsAndOceanBottom_Medium));
        map.VectorDataSources.Add("countries",
            new NaturalEarthVectorDataSource(NaturalEarthVectorDataSet.Admin0_Countries_50m));
        map.VectorDataSources.Add("graticule",
            new GraticuleDataSource { XInterval = 10, YInterval = 10 });

        // Styles
        map.MapLayers.Add(new RasterMapLayer("neBaseRaster", "neBaseRaster", new RasterStyle()));
        map.MapLayers.Add(new VectorMapLayer("graticule", "graticule", new VectorStyle {
            LineColor = Color.Navy,
            LineWidth = 0.06,
            MaskedBy = { "labelsMask" }
        }));
        map.MapLayers.Add(new VectorMapLayer("borders", "countries", new VectorStyle {
            LineColor = Color.FromArgb(60, Color.Black),
            LineWidth = 0.1,
            MaskedBy = { "labelsMask" }
        }));
        map.MapLayers.Add(new VectorMapLayer("labels", "countries", new VectorStyle {
            MaskName = "labelsMask",
            Symbol = SymbolType.Circle,
            SymbolSize = 0.5,
            SymbolMaskWidth = 0.5,
            TextColor = Color.Black,
            TextSize = 1.2,
            TextTag = "NAME",
            TextMaskWidth = 0.4
        }));

        // Render and save
        await map.Render(canvasStack,
            ratioMismatchStrategy: AspectRatioMismatchStrategy.CenterOnCanvas);
        string filename = FileSystemHelpers.GetTempOutputFileName(
            canvasStack.DefaultFileExtension, "NaturalEarth_WorldMap");
        canvasStack.SaveToFile(filename);
    }

    #endregion
}
