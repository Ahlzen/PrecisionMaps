﻿using System.Threading.Tasks;
using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Util;

namespace MapLib.DataSources.Raster;

public enum NaturalEarthRasterDataSet
{
    // Rasters may be available in:
    //   Large size, recommended at 1:10m scale
    //   Medium size, recommended at 1:10m scale
    //   Small size, recommended at 1:50m scale

    CrossBlendedHypsometricTints_Large,
    CrossBlendedHypsometricTints_Medium,
    CrossBlendedHypsometricTintsWithRelief_Large,
    CrossBlendedHypsometricTintsWithRelief_Medium,
    CrossBlendedHypsometricTintsWithRelief_Small,
    CrossBlendedHypsometricTintsWithReliefAndWater_Large,
    CrossBlendedHypsometricTintsWithReliefAndWater_Medum,
    CrossBlendedHypsometricTintsWithReliefAndWater_Small,
    CrossBlendedHypsometricTintsWithReliefWaterAndDrains_Large,
    CrossBlendedHypsometricTintsWithReliefWaterAndDrains_Medium,
    CrossBlendedHypsometricTintsWithReliefWaterDrainsAndOceanBottom_Large,
    CrossBlendedHypsometricTintsWithReliefWaterDrainsAndOceanBottom_Medium,

    NaturalEarth1_Large,
    NaturalEarth1_Medium,
    NaturalEarth1WithRelief_Large,
    NaturalEarth1WithRelief_Medium,
    NaturalEarth1WithRelief_Small,
    NaturalEarth1WithReliefAndWater_Large,
    NaturalEarth1WithReliefAndWater_Medium,
    NaturalEarth1WithReliefAndWater_Small,
    NaturalEarth1WithReliefWaterAndDrains_Large,
    NaturalEarth1WithReliefWaterAndDrains_Medium,

    NaturalEarth2_Large,
    NaturalEarth2_Medium,
    NaturalEarth2WithRelief_Large,
    NaturalEarth2WithRelief_Medium,
    NaturalEarth2WithRelief_Small,
    NaturalEarth2WithReliefAndWater_Large,
    NaturalEarth2WithReliefAndWater_Medium,
    NaturalEarth2WithReliefAndWater_Small,
    NaturalEarth2WithReliefWaterAndDrains_Large,
    NaturalEarth2WithReliefWaterAndDrains_Medium,

    OceanBottom_Medium,
    OceanBottom_Small,
    Bathymetry_Small,

    ShadedRelief_Large,
    ShadedRelief_Medium,
    ShadedRelief_Small,

    GrayEarthWithReliefAndHypsography_Large,
    GrayEarthWithReliefAndHypsography_Medium,
    GrayEarthWithReliefAndHypsography_Small,
    GrayEarthWithReliefHypsographyAndFlatWater_Large,
    GrayEarthWithReliefHypsographyAndFlatWater_Medium,
    GrayEarthWithReliefHypsographyAndFlatWater_Small,
    GrayEarthWithReliefHypsographyAndOceanBottom_Large,
    GrayEarthWithReliefHypsographyAndOceanBottom_Medium,
    GrayEarthWithReliefHypsographyAndOceanBottom_Small,
    GrayEarthWithReliefHypsographyOceanBottomAndDrains_Large,
    GrayEarthWithReliefHypsographyOceanBottomAndDrains_Medium,

    ManualShadedRelief_ContiguousUS_Medium,
    ManualShadedRelief_Small,
    PrismaShadedRelief_Small,
}

public class NaturalEarthRasterDataSource(NaturalEarthRasterDataSet dataSet)
    : BaseRasterDataSource
{
    public override string Name => "Natural Earth Raster Data";
    public override string Srs => Epsg.Wgs84;

    private string Subdirectory => "NaturalEarth";

    public override Bounds? Bounds => Geometry.Bounds.GlobalWgs84;
    public override bool IsBounded => true;

    public NaturalEarthRasterDataSet DataSet { get; }

    public override async Task<RasterData> GetData()
    {
        GdalDataSource source = await GetDataSource();
        return await source.GetData();
    }

    public override async Task<RasterData> GetData(string destSrs)
    {
        GdalDataSource source = await GetDataSource();
        return await GetData(destSrs);
    }

    public override async Task<RasterData> GetData(Bounds boundsWgs84)
    {
        GdalDataSource source = await GetDataSource();
        return await GetData(boundsWgs84);
    }

    public override async Task<RasterData> GetData(Bounds boundsWgs84, string destSrs)
    {
        GdalDataSource source = await GetDataSource();
        return await GetData(boundsWgs84, destSrs);
    }

    private async Task<GdalDataSource> GetDataSource()
    {
        string url = BaseUrl + DataSetUrls[DataSet];
        string filePath = await DownloadAndCache(url, Subdirectory);

        // If zip archive, return the corresponding GeoTIFF
        if (filePath.EndsWith(".zip"))
            filePath = filePath.TrimEnd(".zip") + ".tif";

        GdalDataSource source = new(filePath);
        return source;
    }


    // Here's how/where to obtain the data...

    // NOTE: Downloading directly from naturalearthdata.com returns a HTTP 500
    // (known limitation; intentional?) so we get the files from the NACIS CDN instead.
    private static readonly string BaseUrl = "https://naciscdn.org/naturalearth/";

    private static readonly Dictionary<NaturalEarthRasterDataSet, string>
        DataSetUrls = new()
        {
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTints_Large, "10m/raster/HYP_HR.zip" },
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTints_Medium, "10m/raster/HYP_LR.zip" },
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTintsWithRelief_Large, "10m/raster/HYP_HR_SR.zip" },
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTintsWithRelief_Medium, "10m/raster/HYP_LR_SR.zip" },
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTintsWithRelief_Small, "50m/raster/HYP_50M_SR.zip" },
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTintsWithReliefAndWater_Large, "10m/raster/HYP_HR_SR_W.zip" },
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTintsWithReliefAndWater_Medum, "10m/raster/HYP_LR_SR_W.zip" },
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTintsWithReliefAndWater_Small, "50m/raster/HYP_50M_SR_W.zip" },
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTintsWithReliefWaterAndDrains_Large, "10m/raster/HYP_HR_SR_W_DR.zip" },
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTintsWithReliefWaterAndDrains_Medium, "10m/raster/HYP_LR_SR_W_DR.zip" },
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTintsWithReliefWaterDrainsAndOceanBottom_Large, "10m/raster/HYP_HR_SR_OB_DR.zip" },
            { NaturalEarthRasterDataSet.CrossBlendedHypsometricTintsWithReliefWaterDrainsAndOceanBottom_Medium, "10m/raster/HYP_LR_SR_OB_DR.zip" },

            { NaturalEarthRasterDataSet.NaturalEarth1_Large, "10m/raster/NE1_HR_LC.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth1_Medium, "10m/raster/NE1_LR_LC.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth1WithRelief_Large, "10m/raster/NE1_HR_LC_SR.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth1WithRelief_Medium, "10m/raster/NE1_LR_LC_SR.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth1WithRelief_Small, "10m/raster/NE1_50M_SR.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth1WithReliefAndWater_Large, "10m/raster/NE1_HR_LC_SR_W.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth1WithReliefAndWater_Medium, "10m/raster/NE1_LR_LC_SR_W.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth1WithReliefAndWater_Small, "10m/raster/NE1_50M_SR_W.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth1WithReliefWaterAndDrains_Large, "10m/raster/NE1_HR_LC_SR_W_DR.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth1WithReliefWaterAndDrains_Medium, "10m/raster/NE1_LR_LC_SR_W_DR.zip" },

            { NaturalEarthRasterDataSet.NaturalEarth2_Large, "10m/raster/NE2_HR_LC.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth2_Medium, "10m/raster/NE2_LR_LC.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth2WithRelief_Large, "10m/raster/NE2_HR_LC_SR.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth2WithRelief_Medium, "10m/raster/NE2_LR_LC_SR.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth2WithRelief_Small, "50m/raster/NE2_50M_SR.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth2WithReliefAndWater_Large, "10m/raster/NE2_HR_LC_SR_W.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth2WithReliefAndWater_Medium, "10m/raster/NE2_LR_LC_SR_W.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth2WithReliefAndWater_Small, "50m/raster/NE2_50M_SR_W.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth2WithReliefWaterAndDrains_Large, "10m/raster/NE2_HR_LC_SR_W_DR.zip" },
            { NaturalEarthRasterDataSet.NaturalEarth2WithReliefWaterAndDrains_Medium, "10m/raster/NE2_LR_LC_SR_W_DR.zip" },

            { NaturalEarthRasterDataSet.OceanBottom_Medium, "10m/raster/OB_LR.zip" },
            { NaturalEarthRasterDataSet.OceanBottom_Small, "50m/raster/OB_50M.zip" },
            { NaturalEarthRasterDataSet.Bathymetry_Small, "50m/raster/BATH_50M.zip" },

            { NaturalEarthRasterDataSet.ShadedRelief_Large, "10m/raster/SR_HR.zip" },
            { NaturalEarthRasterDataSet.ShadedRelief_Medium, "10m/raster/SR_LR.zip" },
            { NaturalEarthRasterDataSet.ShadedRelief_Small, "50m/raster/SR_50M.zip" },

            { NaturalEarthRasterDataSet.GrayEarthWithReliefAndHypsography_Large, "10m/raster/GRAY_HR_SR.zip" },
            { NaturalEarthRasterDataSet.GrayEarthWithReliefAndHypsography_Medium, "10m/raster/GRAY_LR_SR.zip" },
            { NaturalEarthRasterDataSet.GrayEarthWithReliefAndHypsography_Small, "50m/raster/GRAY_50M_SR.zip" },
            { NaturalEarthRasterDataSet.GrayEarthWithReliefHypsographyAndFlatWater_Large, "10m/raster/GRAY_HR_SR_W.zip" },
            { NaturalEarthRasterDataSet.GrayEarthWithReliefHypsographyAndFlatWater_Medium, "10m/raster/GRAY_LR_SR_W.zip" },
            { NaturalEarthRasterDataSet.GrayEarthWithReliefHypsographyAndFlatWater_Small, "50m/raster/GRAY_50M_SR_W.zip" },
            { NaturalEarthRasterDataSet.GrayEarthWithReliefHypsographyAndOceanBottom_Large, "10m/raster/GRAY_HR_SR_OB.zip" },
            { NaturalEarthRasterDataSet.GrayEarthWithReliefHypsographyAndOceanBottom_Medium, "10m/raster/GRAY_LR_SR_OB.zip" },
            { NaturalEarthRasterDataSet.GrayEarthWithReliefHypsographyAndOceanBottom_Small, "50m/raster/GRAY_50M_SR_OB.zip" },
            { NaturalEarthRasterDataSet.GrayEarthWithReliefHypsographyOceanBottomAndDrains_Large, "10m/raster/GRAY_HR_SR_OB_DR.zip" },
            { NaturalEarthRasterDataSet.GrayEarthWithReliefHypsographyOceanBottomAndDrains_Medium, "10m/raster/GRAY_LR_SR_OB_DR.zip" },
            
            { NaturalEarthRasterDataSet.ManualShadedRelief_ContiguousUS_Medium, "10m/raster/US_MSR_10M.zip" },
            { NaturalEarthRasterDataSet.ManualShadedRelief_Small, "50m/raster/MSR_50M.zip" },
            { NaturalEarthRasterDataSet.PrismaShadedRelief_Small, "50m/raster/PRISMA_SR_50M.zip" }
        };
}