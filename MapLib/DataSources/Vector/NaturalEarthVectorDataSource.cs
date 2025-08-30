using System.Threading.Tasks;
using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Util;

namespace MapLib.DataSources.Vector;

public enum NaturalEarthVectorDataSet
{
    // Layers may be available in:
    //   1:10m (large scale data; high detail)
    //   1:50m (medium scale data; medium detail)
    //   1:110m (small scale data; low detail)

    ///// Cultural Features

    // Admin level 0 - Countries
    Admin0_Countries_10m,
    Admin0_Countries_50m,
    Admin0_Countries_110m,
    Admin0_Countries_WithoutBoundaryLakes_10m,
    Admin0_Countries_WithoutBoundaryLakes_50m,
    Admin0_Countries_WithoutBoundaryLakes_110m,
    Admin0_CountryBoundaryLines_110m,

    // Admin level 0 - Details
    Admin0_TinyCountryPoints_50m,
    Admin0_TinyCountryPoints_110m,
    Admin0_TinyCountryPoints_ScaleRanks_50m,
    Admin0_Sovereignty_10m,
    Admin0_Sovereignty_50m,
    Admin0_Sovereignty_110m,
    Admin0_MapUnits_10m,
    Admin0_MapUnits_50m,
    Admin0_MapUnits_110m,
    Admin0_MapSubUnits_10m,
    Admin0_MapSubUnits_50m,
    Admin0_ScaleRanks_10m,
    Admin0_ScaleRanks_50m,
    Admin0_ScaleRanks_110m,
    Admin0_ScaleRanks_WithMinorIslands_10m,
    
    // Admin level 0 - Boundary Lines
    Admin0_LandBoundaries_10m,
    Admin0_LandBoundaries_50m,
    Admin0_MapUnitLines_10m,
    //Admin0_MapUnitLines_50m, // unavailable? (403 forbidden)
    Admin0_MaritimeIndicators_10m,
    Admin0_MaritimeIndicators_50m,
    Admin0_MaritimeIndicators_ChinaSupplement_10m,
    Admin0_MaritimeIndicators_ChinaSupplement_50m,
    Admin0_PacificGroupingLines_10m,
    Admin0_PacificGroupingLines_50m,
    Admin0_PacificGroupingLines_110m,

    // Admin level 0 - Breakaway, Disputed areas
    Admin0_BreakawayAndDisputedAreas_10m,
    //Admin0_BreakawayAndDisputedAreas_50m, // unavailable? (403 forbidden)
    Admin0_BreakawayAndDisputedAreas_WithScaleRanks_10m,
    Admin0_BreakawayAndDisputedAreas_BoundaryLines_10m,
    Admin0_BreakawayAndDisputedAreas_BoundaryLines_50m,
    Admin0_AntarcticClaims_10m,
    Admin0_AntarcticClaimLimitLines_10m,

    // Admin level 1 - States, Provinces
    Admin1_StatesAndProvinces_10m,
    Admin1_StatesAndProvinces_50m,
    Admin1_StatesAndProvinces_110m,
    Admin1_StatesAndProvinces_ScaleRanks_10m,
    Admin1_StatesAndProvinces_ScaleRanks_50m,
    Admin1_StatesAndProvinces_ScaleRanks_110m,
    Admin1_StatesAndProvinces_WithoutLargeLakes_10m,
    Admin1_StatesAndProvinces_WithoutLargeLakes_50m,
    Admin1_StatesAndProvinces_WithoutLargeLakes_110m,
    Admin1_StatesAndProvinces_BoundaryLines_10m,
    Admin1_StatesAndProvinces_BoundaryLines_50m,
    Admin1_StatesAndProvinces_BoundaryLines_110m,

    // Admin level 2 - Counties
    Admin2_Counties_10m,
    Admin2_Counties_WithScaleRanks_10m,
    Admin2_Counties_WithoutLargeLakes_10m,
    Admin2_Counties_WithScaleRanksAndMinorIslands_10m,

    // Man-made features
    PopulatedPlaces_10m,
    PopulatedPlaces_50m,
    PopulatedPlaces_110m,
    PopulatedPlaces_Simple_10m,
    PopulatedPlaces_Simple_50m,
    PopulatedPlaces_Simple_110m,
    Roads_10m,
    Roads_NorthAmericaSupplement_10m,
    Railroads_10m,
    Railroads_NorthAmericaSupplement_10m,
    Airports_10m,
    //Airports_50m, // unavailable? (403 forbidden)
    Ports_10m,
    //Ports_50m, // unavailable? (403 forbidden)
    UrbanAreas_10m,
    UrbanAreas_50m,
    //USNationalParks_10m, // TODO: This contains several shapfiles (for different feature types)
    TimeZones_10m,

    // Cultural building blocks
    Admin0_LabelPoints_10m,
    Admin0_Seams_10m,
    Admin1_LabelPoints_10m,
    Admin1_LabelPointDetails_10m,
    Admin1_Seams_10m,
    Admin2_LabelPoints_10m,
    Admin2_LabelPointDetails_10m,
    //All_Cultural_Building_Blocks_10m, // unavailable? (403 forbidden)


    ///// Physical features

    // Land and Ocean
    Coastline_10m,
    Coastline_50m,
    Coastline_110m,
    LandPolygons_10m,
    LandPolygons_50m,
    LandPolygons_110m,
    LandPolygons_WithScaleRank_10m,
    MinorIslands_10m,
    MinorIslandsCoastline_10m,
    Reefs_10m,
    Ocean_10m,
    Ocean_50m,
    Ocean_110m,
    Ocean_WithScaleRank_10m,

    // Rivers
    RiversAndLakeCenterlines_10m,
    RiversAndLakeCenterlines_50m,
    RiversAndLakeCenterlines_110m,
    RiversAndLakeCenterlines_WithScaleRanksAndTapering_10m,
    RiversAndLakeCenterlines_WithScaleRanksAndTapering_50m,
    RiversAndLakeCenterlines_AustraliaSupplement_10m,
    RiversAndLakeCenterlines_EuropeSupplement_10m,
    RiversAndLakeCenterlines_NorthAmericaSupplement_10m,
    
    // Lakes
    Lakes_10m,
    Lakes_50m,
    Lakes_110m,
    HistoricLakes_10m,
    HistoricLakes_50m,
    PluvialLakes_10m,
    Lakes_AustraliaSupplement_10m,
    Lakes_EuropeSupplement_10m,
    Lakes_NorthAmericaSupplement_10m,
    
    // Physical labels
    LabelAreas_10m,
    LabelAreas_50m,
    LabelAreas_110m,
    LabelPoints_10m,
    LabelPoints_50m,
    LabelPoints_110m,
    ElevationPoints_10m,
    ElevationPoints_50m,
    ElevationPoints_110m,
    MarineAreas_10m,
    MarineAreas_50m,
    MarineAreas_110m,

    // Misc
    Playas_10m,
    Playas_50m,
    AntarcticIceShelves_10m,
    AntarcticIceShelves_50m,
    AntarcticIceShelfEdge_10m,
    AntarcticIceShelfEdge_50m,
    GlaciatedAreas_10m,
    GlaciatedAreas_50m,
    GlaciatedAreas_110m,
    //Bathymetry_10m, // unavailable? (403 forbidden)
    GeographicLines_10m,
    GeographicLines_50m,
    GeographicLines_110m,
    //Graticules_10m, // unavailable? (403 forbidden)
    //Graticules_50m, // unavailable? (403 forbidden)
    //Graticules_110m, // unavailable? (403 forbidden)
    LandAndOceanLabelPoints_10m,
    MinorIslandsLabelPoints_10m,
    LandAndOceanSeams_10m,
    //Physical_Building_Blocks_10m, // unavailable? (403 forbidden)
}

/// <summary>
/// Direct access to Natural Earth vector data sets.
/// See: https://www.naturalearthdata.com/downloads/
/// </summary>
/// <param name="dataSet"></param>
public class NaturalEarthVectorDataSource(NaturalEarthVectorDataSet dataSet)
    : BaseVectorDataSource
{
    public override string Name => "Natural Earth Vector Data";
    public override Srs Srs => Srs.Wgs84;

    private string Subdirectory => "NaturalEarth_Vector";

    public override Bounds? Bounds => Geometry.Bounds.GlobalWgs84;
    public override bool IsBounded => true;

    public NaturalEarthVectorDataSet DataSet => dataSet;

    /// <summary>Downloads the data file to cache (if not already there).</summary>
    /// <returns>Path to the target file.</returns>
    public async Task<string> Download()
    {
        string url = BaseUrl + DataSetUrls[DataSet];
        string targetFileName = UrlHelper.GetFilenameFromUrl(url).TrimEnd(".zip") + ".shp";
        return await DownloadAndCache(url, Subdirectory, targetFileName);
    }

    public override async Task<VectorData> GetData()
    {
        string targetFilePath = await Download();
        VectorFileDataSource source = new(targetFilePath);
        VectorData data = await source.GetData();
        return data;
    }

    public override async Task<VectorData> GetData(Bounds boundsWgs84)
        => await GetData();



    // Here's how/where to obtain the data...

    // NOTE: Downloading directly from naturalearthdata.com returns a HTTP 500
    // (known limitation; intentional?) so we get the files from the NACIS CDN instead.
    private static readonly string BaseUrl = "https://naciscdn.org/naturalearth/";

    private static readonly Dictionary<NaturalEarthVectorDataSet, string>
        DataSetUrls = new()
        {
            // Admin level 0 : Countries
            { NaturalEarthVectorDataSet.Admin0_Countries_10m, "10m/cultural/ne_10m_admin_0_countries.zip" },
            { NaturalEarthVectorDataSet.Admin0_Countries_50m, "50m/cultural/ne_50m_admin_0_countries.zip" },
            { NaturalEarthVectorDataSet.Admin0_Countries_110m, "110m/cultural/ne_110m_admin_0_countries.zip" },
            { NaturalEarthVectorDataSet.Admin0_Countries_WithoutBoundaryLakes_10m, "10m/cultural/ne_10m_admin_0_countries_lakes.zip" },
            { NaturalEarthVectorDataSet.Admin0_Countries_WithoutBoundaryLakes_50m, "50m/cultural/ne_50m_admin_0_countries_lakes.zip" },
            { NaturalEarthVectorDataSet.Admin0_Countries_WithoutBoundaryLakes_110m, "110m/cultural/ne_110m_admin_0_countries_lakes.zip" },
            { NaturalEarthVectorDataSet.Admin0_CountryBoundaryLines_110m, "110m/cultural/ne_110m_admin_0_boundary_lines_land.zip" },
            { NaturalEarthVectorDataSet.Admin0_TinyCountryPoints_50m, "50m/cultural/ne_50m_admin_0_tiny_countries.zip" },
            { NaturalEarthVectorDataSet.Admin0_TinyCountryPoints_110m, "110m/cultural/ne_110m_admin_0_tiny_countries.zip" },
            { NaturalEarthVectorDataSet.Admin0_TinyCountryPoints_ScaleRanks_50m, "50m/cultural/ne_50m_admin_0_tiny_countries_scale_rank.zip" },
            { NaturalEarthVectorDataSet.Admin0_Sovereignty_10m, "10m/cultural/ne_10m_admin_0_sovereignty.zip" },
            { NaturalEarthVectorDataSet.Admin0_Sovereignty_50m, "50m/cultural/ne_50m_admin_0_sovereignty.zip" },
            { NaturalEarthVectorDataSet.Admin0_Sovereignty_110m, "110m/cultural/ne_110m_admin_0_sovereignty.zip" },
            { NaturalEarthVectorDataSet.Admin0_MapUnits_10m, "10m/cultural/ne_10m_admin_0_map_units.zip" },
            { NaturalEarthVectorDataSet.Admin0_MapUnits_50m, "50m/cultural/ne_50m_admin_0_map_units.zip" },
            { NaturalEarthVectorDataSet.Admin0_MapUnits_110m, "110m/cultural/ne_110m_admin_0_map_units.zip" },
            { NaturalEarthVectorDataSet.Admin0_MapSubUnits_10m, "10m/cultural/ne_10m_admin_0_map_subunits.zip" },
            { NaturalEarthVectorDataSet.Admin0_MapSubUnits_50m, "50m/cultural/ne_50m_admin_0_map_subunits.zip" },
            { NaturalEarthVectorDataSet.Admin0_ScaleRanks_10m, "10m/cultural/ne_10m_admin_0_scale_rank.zip" },
            { NaturalEarthVectorDataSet.Admin0_ScaleRanks_50m, "50m/cultural/ne_50m_admin_0_scale_rank.zip" },
            { NaturalEarthVectorDataSet.Admin0_ScaleRanks_110m, "110m/cultural/ne_110m_admin_0_scale_rank.zip" },
            { NaturalEarthVectorDataSet.Admin0_ScaleRanks_WithMinorIslands_10m, "10m/cultural/ne_10m_admin_0_scale_rank_minor_islands.zip" },
            { NaturalEarthVectorDataSet.Admin0_LandBoundaries_10m, "10m/cultural/ne_10m_admin_0_boundary_lines_land.zip" },
            { NaturalEarthVectorDataSet.Admin0_LandBoundaries_50m, "50m/cultural/ne_50m_admin_0_boundary_lines_land.zip" },
            { NaturalEarthVectorDataSet.Admin0_MapUnitLines_10m, "10m/cultural/ne_10m_admin_0_boundary_lines_map_units.zip" },
            //{ NaturalEarthVectorDataSet.Admin0_MapUnitLines_50m, "50m/cultural/ne_50m_admin_0_boundary_lines_map_units.zip" }, // unavailable? (403 forbidden)
            { NaturalEarthVectorDataSet.Admin0_MaritimeIndicators_10m, "10m/cultural/ne_10m_admin_0_boundary_lines_maritime_indicator.zip" },
            { NaturalEarthVectorDataSet.Admin0_MaritimeIndicators_50m, "50m/cultural/ne_50m_admin_0_boundary_lines_maritime_indicator.zip" },
            { NaturalEarthVectorDataSet.Admin0_MaritimeIndicators_ChinaSupplement_10m, "10m/cultural/ne_10m_admin_0_boundary_lines_maritime_indicator_chn.zip" },
            { NaturalEarthVectorDataSet.Admin0_MaritimeIndicators_ChinaSupplement_50m, "50m/cultural/ne_50m_admin_0_boundary_lines_maritime_indicator_chn.zip" },
            { NaturalEarthVectorDataSet.Admin0_PacificGroupingLines_10m, "10m/cultural/ne_10m_admin_0_pacific_groupings.zip" },
            { NaturalEarthVectorDataSet.Admin0_PacificGroupingLines_50m, "50m/cultural/ne_50m_admin_0_pacific_groupings.zip" },
            { NaturalEarthVectorDataSet.Admin0_PacificGroupingLines_110m, "110m/cultural/ne_110m_admin_0_pacific_groupings.zip" },
            { NaturalEarthVectorDataSet.Admin0_BreakawayAndDisputedAreas_10m, "10m/cultural/ne_10m_admin_0_disputed_areas.zip" },
            //{ NaturalEarthVectorDataSet.Admin0_BreakawayAndDisputedAreas_50m, "50m/cultural/ne_50m_admin_0_disputed_areas.zip" },
            { NaturalEarthVectorDataSet.Admin0_BreakawayAndDisputedAreas_WithScaleRanks_10m, "10m/cultural/ne_10m_admin_0_disputed_areas_scale_rank_minor_islands.zip" },
            { NaturalEarthVectorDataSet.Admin0_BreakawayAndDisputedAreas_BoundaryLines_10m, "10m/cultural/ne_10m_admin_0_boundary_lines_disputed_areas.zip" },
            { NaturalEarthVectorDataSet.Admin0_BreakawayAndDisputedAreas_BoundaryLines_50m, "50m/cultural/ne_50m_admin_0_boundary_lines_disputed_areas.zip" },
            { NaturalEarthVectorDataSet.Admin0_AntarcticClaims_10m, "10m/cultural/ne_10m_admin_0_antarctic_claims.zip" },
            { NaturalEarthVectorDataSet.Admin0_AntarcticClaimLimitLines_10m, "10m/cultural/ne_10m_admin_0_antarctic_claim_limit_lines.zip" },

            // Admin level 1 : States and Provinces
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_10m, "10m/cultural/ne_10m_admin_1_states_provinces.zip" },
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_50m, "50m/cultural/ne_50m_admin_1_states_provinces.zip" },
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_110m, "110m/cultural/ne_110m_admin_1_states_provinces.zip" },
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_ScaleRanks_10m, "10m/cultural/ne_10m_admin_1_states_provinces_scale_rank.zip" },
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_ScaleRanks_50m, "50m/cultural/ne_50m_admin_1_states_provinces.zip" },
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_ScaleRanks_110m, "110m/cultural/ne_110m_admin_1_states_provinces.zip" },
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_WithoutLargeLakes_10m, "10m/cultural/ne_10m_admin_1_states_provinces_lakes.zip" },
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_WithoutLargeLakes_50m, "50m/cultural/ne_50m_admin_1_states_provinces_lakes.zip" },
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_WithoutLargeLakes_110m, "110m/cultural/ne_110m_admin_1_states_provinces_lakes.zip" },
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_BoundaryLines_10m, "10m/cultural/ne_10m_admin_1_states_provinces_lines.zip" },
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_BoundaryLines_50m, "50m/cultural/ne_50m_admin_1_states_provinces_lines.zip" },
            { NaturalEarthVectorDataSet.Admin1_StatesAndProvinces_BoundaryLines_110m, "110m/cultural/ne_110m_admin_1_states_provinces_lines.zip" },

            // Admin level 2 : Counties
            { NaturalEarthVectorDataSet.Admin2_Counties_10m, "10m/cultural/ne_10m_admin_2_counties.zip" },
            { NaturalEarthVectorDataSet.Admin2_Counties_WithScaleRanks_10m, "10m/cultural/ne_10m_admin_2_counties_scale_rank.zip" },
            { NaturalEarthVectorDataSet.Admin2_Counties_WithoutLargeLakes_10m, "10m/cultural/ne_10m_admin_2_counties_lakes.zip" },
            { NaturalEarthVectorDataSet.Admin2_Counties_WithScaleRanksAndMinorIslands_10m, "10m/cultural/ne_10m_admin_2_counties_scale_rank_minor_islands.zip" },

            // Man-made features
            { NaturalEarthVectorDataSet.PopulatedPlaces_10m, "10m/cultural/ne_10m_populated_places.zip" },
            { NaturalEarthVectorDataSet.PopulatedPlaces_50m, "50m/cultural/ne_50m_populated_places.zip" },
            { NaturalEarthVectorDataSet.PopulatedPlaces_110m, "110m/cultural/ne_110m_populated_places.zip" },
            { NaturalEarthVectorDataSet.PopulatedPlaces_Simple_10m, "10m/cultural/ne_10m_populated_places_simple.zip" },
            { NaturalEarthVectorDataSet.PopulatedPlaces_Simple_50m, "50m/cultural/ne_50m_populated_places_simple.zip" },
            { NaturalEarthVectorDataSet.PopulatedPlaces_Simple_110m, "110m/cultural/ne_110m_populated_places_simple.zip" },
            { NaturalEarthVectorDataSet.Roads_10m, "10m/cultural/ne_10m_roads.zip" },
            { NaturalEarthVectorDataSet.Roads_NorthAmericaSupplement_10m, "10m/cultural/ne_10m_roads_north_america.zip" },
            { NaturalEarthVectorDataSet.Railroads_10m, "10m/cultural/ne_10m_railroads.zip" },
            { NaturalEarthVectorDataSet.Railroads_NorthAmericaSupplement_10m, "10m/cultural/ne_10m_railroads_north_america.zip" },
            { NaturalEarthVectorDataSet.Airports_10m, "10m/cultural/ne_10m_airports.zip" },
            //{ NaturalEarthVectorDataSet.Airports_50m, "50m/cultural/ne_50m_airports.zip" }, // unavailable? (403 forbidden)
            { NaturalEarthVectorDataSet.Ports_10m, "10m/cultural/ne_10m_ports.zip" },
            //{ NaturalEarthVectorDataSet.Ports_50m, "50m/cultural/ne_50m_ports.zip" }, // unavailable? (403 forbidden)
            { NaturalEarthVectorDataSet.UrbanAreas_10m, "10m/cultural/ne_10m_urban_areas.zip" },
            { NaturalEarthVectorDataSet.UrbanAreas_50m, "50m/cultural/ne_50m_urban_areas.zip" },
            //{ NaturalEarthVectorDataSet.USNationalParks_10m, "10m/cultural/ne_10m_parks_and_protected_lands.zip" },
            { NaturalEarthVectorDataSet.TimeZones_10m, "10m/cultural/ne_10m_time_zones.zip" },
            
            // Label points etc
            { NaturalEarthVectorDataSet.Admin0_LabelPoints_10m, "10m/cultural/ne_10m_admin_0_label_points.zip" },
            { NaturalEarthVectorDataSet.Admin0_Seams_10m, "10m/cultural/ne_10m_admin_0_seams.zip" },
            { NaturalEarthVectorDataSet.Admin1_LabelPoints_10m, "10m/cultural/ne_10m_admin_1_label_points.zip" },
            { NaturalEarthVectorDataSet.Admin1_LabelPointDetails_10m, "10m/cultural/ne_10m_admin_1_label_points_details.zip" },
            { NaturalEarthVectorDataSet.Admin1_Seams_10m, "10m/cultural/ne_10m_admin_1_seams.zip" },
            { NaturalEarthVectorDataSet.Admin2_LabelPoints_10m, "10m/cultural/ne_10m_admin_2_label_points.zip" },
            { NaturalEarthVectorDataSet.Admin2_LabelPointDetails_10m, "10m/cultural/ne_10m_admin_2_label_points_details.zip" },
            //{ NaturalEarthVectorDataSet.All_Cultural_Building_Blocks_10m, "10m/cultural/ne_10m_cultural_building_blocks_all.zip" }, // unavailable? (403 forbidden)

            // Physical features
            { NaturalEarthVectorDataSet.Coastline_10m, "10m/physical/ne_10m_coastline.zip" },
            { NaturalEarthVectorDataSet.Coastline_50m, "50m/physical/ne_50m_coastline.zip" },
            { NaturalEarthVectorDataSet.Coastline_110m, "110m/physical/ne_110m_coastline.zip" },
            { NaturalEarthVectorDataSet.LandPolygons_10m, "10m/physical/ne_10m_land.zip" },
            { NaturalEarthVectorDataSet.LandPolygons_50m, "50m/physical/ne_50m_land.zip" },
            { NaturalEarthVectorDataSet.LandPolygons_110m, "110m/physical/ne_110m_land.zip" },
            { NaturalEarthVectorDataSet.LandPolygons_WithScaleRank_10m, "10m/physical/ne_10m_land_scale_rank.zip" },
            { NaturalEarthVectorDataSet.MinorIslands_10m, "10m/physical/ne_10m_minor_islands.zip" },
            { NaturalEarthVectorDataSet.MinorIslandsCoastline_10m, "10m/physical/ne_10m_minor_islands_coastline.zip" },
            { NaturalEarthVectorDataSet.Reefs_10m, "10m/physical/ne_10m_reefs.zip" },
            { NaturalEarthVectorDataSet.Ocean_10m, "10m/physical/ne_10m_ocean.zip" },
            { NaturalEarthVectorDataSet.Ocean_50m, "50m/physical/ne_50m_ocean.zip" },
            { NaturalEarthVectorDataSet.Ocean_110m, "110m/physical/ne_110m_ocean.zip" },
            { NaturalEarthVectorDataSet.Ocean_WithScaleRank_10m, "10m/physical/ne_10m_ocean_scale_rank.zip" },
            { NaturalEarthVectorDataSet.RiversAndLakeCenterlines_10m, "10m/physical/ne_10m_rivers_lake_centerlines.zip" },
            { NaturalEarthVectorDataSet.RiversAndLakeCenterlines_50m, "50m/physical/ne_50m_rivers_lake_centerlines.zip" },
            { NaturalEarthVectorDataSet.RiversAndLakeCenterlines_110m, "110m/physical/ne_110m_rivers_lake_centerlines.zip" },
            { NaturalEarthVectorDataSet.RiversAndLakeCenterlines_WithScaleRanksAndTapering_10m, "10m/physical/ne_10m_rivers_lake_centerlines_scale_rank.zip" },
            { NaturalEarthVectorDataSet.RiversAndLakeCenterlines_WithScaleRanksAndTapering_50m, "50m/physical/ne_50m_rivers_lake_centerlines_scale_rank.zip" },
            { NaturalEarthVectorDataSet.RiversAndLakeCenterlines_AustraliaSupplement_10m, "10m/physical/ne_10m_rivers_australia.zip" },
            { NaturalEarthVectorDataSet.RiversAndLakeCenterlines_EuropeSupplement_10m, "10m/physical/ne_10m_rivers_europe.zip" },
            { NaturalEarthVectorDataSet.RiversAndLakeCenterlines_NorthAmericaSupplement_10m, "10m/physical/ne_10m_rivers_north_america.zip" },
            { NaturalEarthVectorDataSet.Lakes_10m, "10m/physical/ne_10m_lakes.zip" },
            { NaturalEarthVectorDataSet.Lakes_50m, "50m/physical/ne_50m_lakes.zip" },
            { NaturalEarthVectorDataSet.Lakes_110m, "110m/physical/ne_110m_lakes.zip" },
            { NaturalEarthVectorDataSet.HistoricLakes_10m, "10m/physical/ne_10m_lakes_historic.zip" },
            { NaturalEarthVectorDataSet.HistoricLakes_50m, "50m/physical/ne_50m_lakes_historic.zip" },
            { NaturalEarthVectorDataSet.PluvialLakes_10m, "10m/physical/ne_10m_lakes_pluvial.zip" },
            { NaturalEarthVectorDataSet.Lakes_AustraliaSupplement_10m, "10m/physical/ne_10m_lakes_australia.zip" },
            { NaturalEarthVectorDataSet.Lakes_EuropeSupplement_10m, "10m/physical/ne_10m_lakes_europe.zip" },
            { NaturalEarthVectorDataSet.Lakes_NorthAmericaSupplement_10m, "10m/physical/ne_10m_lakes_north_america.zip" },
            { NaturalEarthVectorDataSet.LabelAreas_10m, "10m/physical/ne_10m_geography_regions_polys.zip" },
            { NaturalEarthVectorDataSet.LabelAreas_50m, "50m/physical/ne_50m_geography_regions_polys.zip" },
            { NaturalEarthVectorDataSet.LabelAreas_110m, "110m/physical/ne_110m_geography_regions_polys.zip" },
            { NaturalEarthVectorDataSet.LabelPoints_10m, "10m/physical/ne_10m_geography_regions_points.zip" },
            { NaturalEarthVectorDataSet.LabelPoints_50m, "50m/physical/ne_50m_geography_regions_points.zip" },
            { NaturalEarthVectorDataSet.LabelPoints_110m, "110m/physical/ne_110m_geography_regions_points.zip" },
            { NaturalEarthVectorDataSet.ElevationPoints_10m, "10m/physical/ne_10m_geography_regions_elevation_points.zip" },
            { NaturalEarthVectorDataSet.ElevationPoints_50m, "50m/physical/ne_50m_geography_regions_elevation_points.zip" },
            { NaturalEarthVectorDataSet.ElevationPoints_110m, "110m/physical/ne_110m_geography_regions_elevation_points.zip" },
            { NaturalEarthVectorDataSet.MarineAreas_10m, "10m/physical/ne_10m_geography_marine_polys.zip" },
            { NaturalEarthVectorDataSet.MarineAreas_50m, "50m/physical/ne_50m_geography_marine_polys.zip" },
            { NaturalEarthVectorDataSet.MarineAreas_110m, "110m/physical/ne_110m_geography_marine_polys.zip" },
            { NaturalEarthVectorDataSet.Playas_10m, "10m/physical/ne_10m_playas.zip" },
            { NaturalEarthVectorDataSet.Playas_50m, "50m/physical/ne_50m_playas.zip" },
            { NaturalEarthVectorDataSet.AntarcticIceShelves_10m, "10m/physical/ne_10m_antarctic_ice_shelves_polys.zip" },
            { NaturalEarthVectorDataSet.AntarcticIceShelves_50m, "50m/physical/ne_50m_antarctic_ice_shelves_polys.zip" },
            { NaturalEarthVectorDataSet.AntarcticIceShelfEdge_10m, "10m/physical/ne_10m_antarctic_ice_shelves_lines.zip" },
            { NaturalEarthVectorDataSet.AntarcticIceShelfEdge_50m, "50m/physical/ne_50m_antarctic_ice_shelves_lines.zip" },
            { NaturalEarthVectorDataSet.GlaciatedAreas_10m, "10m/physical/ne_10m_glaciated_areas.zip" },
            { NaturalEarthVectorDataSet.GlaciatedAreas_50m, "50m/physical/ne_50m_glaciated_areas.zip" },
            { NaturalEarthVectorDataSet.GlaciatedAreas_110m, "110m/physical/ne_110m_glaciated_areas.zip" },
            //{ NaturalEarthVectorDataSet.Bathymetry_10m, "10m/physical/ne_10m_bathymetry_all.zip" }, // unavailable? (403 forbidden)
            { NaturalEarthVectorDataSet.GeographicLines_10m, "10m/physical/ne_10m_geographic_lines.zip" },
            { NaturalEarthVectorDataSet.GeographicLines_50m, "50m/physical/ne_50m_geographic_lines.zip" },
            { NaturalEarthVectorDataSet.GeographicLines_110m, "110m/physical/ne_110m_geographic_lines.zip" },
            //{ NaturalEarthVectorDataSet.Graticules_10m, "10m/physical/ne_10m_graticules_all.zip" }, // unavailable? (403 forbidden)
            //{ NaturalEarthVectorDataSet.Graticules_50m, "50m/physical/ne_50m_graticules_all.zip" }, // unavailable? (403 forbidden)
            //{ NaturalEarthVectorDataSet.Graticules_110m, "110m/physical/ne_110m_graticules_all.zip" }, // unavailable? (403 forbidden)
            { NaturalEarthVectorDataSet.LandAndOceanLabelPoints_10m, "10m/physical/ne_10m_land_ocean_label_points.zip" },
            { NaturalEarthVectorDataSet.MinorIslandsLabelPoints_10m, "10m/physical/ne_10m_minor_islands_label_points.zip" },
            { NaturalEarthVectorDataSet.LandAndOceanSeams_10m, "10m/physical/ne_10m_land_ocean_seams.zip" },
            //{ NaturalEarthVectorDataSet.Physical_Building_Blocks_10m, "10m/physical/ne_10m_physical_building_blocks_all.zip" } // unavailable? (403 forbidden)
        };
}