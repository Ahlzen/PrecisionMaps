using System.Threading.Tasks;
using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Util;

namespace MapLib.DataSources.Vector;


public enum NaturalEarthVectorDataSet
{
    ///// Cultural, Large scale (1:10m)

    // Admin 0 - Countries
    NE10m_Admin0_Countries,
    NE10m_Admin0_Countries_WithoutBoundryLakes,
    
    // Admin 0 - Details
    NE10m_Admin0_Soverignty,
    NE10m_Admin0_MapUnits,
    NE10m_Admin0_MapSubUnits,
    NE10m_Admin0_ScaleRanks,
    NE10m_Admin0_ScaleRanks_WithMinorIslands,
    
    // Admin 0 - Boundary Lines
    NE10m_Admin0_LandBoundaries,
    NE10m_Admin0_MapUnitLines,
    NE10m_Admin0_MaritimeIndicators,
    NE10m_Admin0_MaritimeIndicators_ChinaSupplement,
    NE10m_Admin0_PacificGroupingLines,
    
    // Admin 0 - Breakaway, Disputed areas
    NE10m_Admin0_BreakawayAndDisputedAreas,
    NE10m_Admin0_BreakawayAndDisputedAreas_WithScaleRanks,
    NE10m_Admin0_BreakawayAndDisputedAreas_BoundaryLines,
    NE10m_Admin0_AntarcticClaims,
    NE10m_Admin0_AntarcticClaimLimitLines,

    // Admin 1 - States, Provinces
    // TODO: Add layers
    NE10m_Admin1_StatesAndProvinces,
    NE10m_Admin1_StatesAndProvinces_WithScaleRanks,
    NE10m_Admin1_StatesAndProvinces_WithoutLargeLakes,
    NE10m_Admin1_StatesAndProvinces_BoundaryLines,

    // Admin 2 - Counties
    NE10m_Admin2_Counties,
    NE10m_Admin2_Counties_WithScaleRanks,
    NE10m_Admin2_Counties_WithoutLargeLakes,
    NE10m_Admin2_Counties_WithScaleRanksAndMinorIslands,

    // Man-made features
    NE10M_PopulatedPlaces,
    NE10M_PopulatedPlaces_Simple,
    NE10M_Roads,
    NE10M_Roads_NorthAmericaSupplement,
    NE10M_Railroads,
    NE10M_Railroads_NorthAmericaSupplement,
    NE10M_Airports,
    NE10M_Ports,
    NE10M_UrbanAreas,
    NE10M_USNationalParks,
    NE10M_TimeZones,

    // Cultural building blocks
    NE10M_Admin0_LabelPoints,
    NE10M_Admin0_Seams,
    NE10M_Admin1_LabelPoints,
    NE10M_Admin1_LabelPointDetails,
    NE10M_Admin1_Seams,
    NE10M_Admin2_LabelPoints,
    NE10M_Admin2_LabelPointDetails,
    NE10M_All_Cultural_Building_Blocks,


    ///// Physical, Large scale (1:10m)

    // Land and Ocean
    NE10M_Coastline,
    NE10M_LandPolygons,
    NE10M_LandPolygons_WithScaleRank,
    NE10M_MinorIslands,
    NE10M_MinorIslandsCoastline,
    NE10M_Reefs,
    NE10M_Ocean,
    NE10M_Ocean_WithScaleRank,
    // Rivers
    NE10M_RiversAndLakeCenterlines,
    NE10M_RiversAndLakeCenterlines_WithScaleRanksAndTapering,
    NE10M_RiversAndLakeCenterlines_AustraliaSupplement,
    NE10M_RiversAndLakeCenterlines_EuropeSupplement,
    NE10M_RiversAndLakeCenterlines_NorthAmericaSupplement,
    // Lakes
    NE10M_Lakes,
    NE10M_HistoricalLakes,
    NE10M_PluvialLakes,
    NE10M_Lakes_AustraliaSupplement,
    NE10M_Lakes_EuropeSupplement,
    NE10M_Lakes_NorthAmericaSupplement,
    // Physical labels
    NE10M_LabelAreas,
    NE10M_LabelPoints,
    NE10M_ElevationPoints,
    NE10M_MarineAreas,
    // Misc
    NE10M_Playas,
    NE10M_AntarcticIceShelves,
    NE10M_AntarcticIceShelfEdge,
    NE10M_GlaciatedAreas,
    NE10M_Bathymetry,
    NE10M_GeographicLines,
    NE10M_Graticules,
    NE10M_LandAndOceanLabelPoints,
    NE10M_MinorIslandsLabelPoints,
    NE10M_LandAndOceanSeams,
    NE10M_Physical_Building_Blocks,


    ///// Cultural, Medium scale (1:50m)

    // Admin 0 - Countries
    NE50M_Admin0_Countries,
    NE50M_Admin0_Countries_WithoutBoundaryLakes,

    // Admin 0 - Details
    NE50M_Admin0_TinyCountryPoints,
    NE50M_Admin0_TinyCountryPoints_ScaleRanks,
    NE50M_Admin0_Soverignty,
    NE50M_Admin0_MapUnits,
    NE50M_Admin0_MapSubUnits,
    NE50M_Admin0_ScaleRanks,

    // Admin 0 - Boundary Lines
    NE50M_Admin0_LandLines,
    NE50M_Admin0_MapUnitLines,
    NE50M_Admin0_MaritimeIndicators,
    NE50M_Admin0_MaritimeIndicators_ChinaSupplement,
    NE50M_Admin0_PacificGroupingLines,

    // Admin 0 - Breakaway, Disputed areas
    NE50M_Admin0_BreakawayAndDisputedAreas,
    NE50M_Admin0_BreakawayAndDisputedAreas_BoundaryLines,
    
    // Admin 1 - States, Provinces
    NE50M_Admin1_StatesAndProvinces,
    NE50M_Admin1_StatesAndProvinces_WithoutLargeLakes,
    NE50M_Admin1_StatesAndProvinces_BoundaryLines,
    NE50M_Admin1_StatesAndProvinces_ScaleRanks,

    NE50M_PopulatedPlaces,
    NE50M_PopulatedPlaces_Simple,

    NE50M_Airports,
    NE50M_Ports,
    NE50M_UrbanAreas,


    ///// Physical, Medium scale (1:50m)
    
    NE50M_Coastline,
    NE50M_Land,
    NE50M_Ocean,
    NE50M_RiversAndLakeCenterlines,
    NE50M_RiversAndLakeCenterlines_WithScaleRanksAndTapering,
    NE50M_Lakes,
    NE50M_HistoricLakes,
    NE50M_LabelAreas,
    NE50M_LabelPoints,
    NE50M_ElevationPoints,
    NE50M_MarineAreas,
    NE50M_Playas,
    NE50M_GlaciatedAreas,
    NE50M_AntarcticIceShelves,
    NE50M_AntarcticIceShelfEdge,
    NE50M_GeographicLines,
    NE50M_Graticules,


    ///// Cultural, Small scale (1:110m)

    NE110m_Admin0_Countries,
    NE110m_Admin0_Countries_WithoutBoundryLakes,
    NE110m_Admin0_Sovereignty,
    NE110m_Admin0_MapUnits,
    NE110m_Admin0_ScaleRanks,
    NE110m_Admin0_TinyCountryPoints,
    NE110m_Admin0_CountryBoundaries,
    NE110m_Admin0_PacificGroupingLines,
    
    NE110m_Admin1_StatesAndProvinces,
    NE110m_Admin1_StatesAndProvinces_WithoutLargeLakes,
    NE110m_Admin1_Boundaries,
    NE110m_Admin1_ScaleRanks,
    
    NE110m_PopulatedPlaces,
    NE110m_PopulatedPlaces_Simple,



    // Physical, Small scale (1:110m)

    NE110m_Coastline,
    NE110m_Land,
    NE110m_Ocean,
    NE110m_RiversAndLakesCenterlines,
    NE110m_Lakes,
    NE110m_LabelAreas,
    NE110m_LabelPoints,
    NE110m_ElevationPoints,
    NE110m_MarineAreas,
    NE110m_GlaciatedAreas,
    NE110m_GeographicLines,
    NE110m_Graticules,
}

/// <summary>
/// Direct access to Natural Earth vector data sets.
/// See: https://www.naturalearthdata.com/downloads/
/// </summary>
/// <param name="dataSet"></param>
public class NaturalEarthVectorDataSource(NaturalEarthVectorDataSet dataSet)
    : BaseVectorDataSource
{
    // NOTE: Downloading directly from naturalearthdata.com returns a HTTP 500
    // (known limitation; intentional?) so we get the files from the NACIS CDN instead.
    private static readonly string BaseUrl = "https://naciscdn.org/naturalearth/";

    private static readonly Dictionary<NaturalEarthVectorDataSet, string>
        DataSetUrls = new()
        {
            { NaturalEarthVectorDataSet.NE10m_Admin0_Countries, "10m/cultural/ne_10m_admin_0_countries.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_Countries_WithoutBoundryLakes, "10m/cultural/ne_10m_admin_0_countries_lakes.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_Soverignty, "10m/cultural/ne_10m_admin_0_sovereignty.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_MapUnits, "10m/cultural/ne_10m_admin_0_map_units.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_MapSubUnits, "10m/cultural/ne_10m_admin_0_map_subunits.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_ScaleRanks, "10m/cultural/ne_10m_admin_0_scale_rank.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_ScaleRanks_WithMinorIslands, "10m/cultural/ne_10m_admin_0_scale_rank_minor_islands.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_LandBoundaries, "10m/cultural/ne_10m_admin_0_boundary_lines_land.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_MapUnitLines, "10m/cultural/ne_10m_admin_0_boundary_lines_map_units.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_MaritimeIndicators, "10m/cultural/ne_10m_admin_0_boundary_lines_maritime_indicator.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_MaritimeIndicators_ChinaSupplement, "10m/cultural/ne_10m_admin_0_boundary_lines_maritime_indicator_chn.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_PacificGroupingLines, "10m/cultural/ne_10m_admin_0_pacific_groupings.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_BreakawayAndDisputedAreas, "10m/cultural/ne_10m_admin_0_disputed_areas.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_BreakawayAndDisputedAreas_WithScaleRanks, "10m/cultural/ne_10m_admin_0_disputed_areas_scale_rank_minor_islands.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_BreakawayAndDisputedAreas_BoundaryLines, "10m/cultural/ne_10m_admin_0_boundary_lines_disputed_areas.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_AntarcticClaims, "10m/cultural/ne_10m_admin_0_antarctic_claims.zip" },
            { NaturalEarthVectorDataSet.NE10m_Admin0_AntarcticClaimLimitLines, "10m/cultural/ne_10m_admin_0_antarctic_claim_limit_lines.zip" },

            { NaturalEarthVectorDataSet.NE110m_Admin0_Countries, "110m/cultural/ne_110m_admin_0_countries.zip" },
            { NaturalEarthVectorDataSet.NE110m_Admin0_Countries_WithoutBoundryLakes, "110m/cultural/ne_110m_admin_0_countries_lakes.zip" },

            { NaturalEarthVectorDataSet.NE110m_Land, "110m/physical/ne_110m_land.zip" },
        };

    public override string Name => "Natural Earth Vector Data";

    public override string Srs => Epsg.Wgs84;

    private string Subdirectory => "NaturalEarth";

    public override Bounds? Bounds => Geometry.Bounds.GlobalWgs84;
    public override bool IsBounded => true;

    public NaturalEarthVectorDataSet DataSet { get; } = dataSet;

    public override async Task<VectorData> GetData()
    {
        string url = BaseUrl + DataSetUrls[DataSet];
        string filePath = await DownloadAndCache(url, Subdirectory);

        // If zip archive, return the corresponding .shp file
        if (filePath.EndsWith(".zip"))
            filePath = filePath.TrimEnd(".zip") + ".shp";

        VectorFileDataSource source = new(filePath);
        VectorData data = await source.GetData();
        return data;
    }

    public override async Task<VectorData> GetData(Bounds boundsWgs84)
        => await GetData();
}