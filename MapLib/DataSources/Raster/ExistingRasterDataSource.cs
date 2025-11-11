using MapLib.GdalSupport;
using MapLib.Geometry;
using MapLib.Util;
using OSGeo.GDAL;
using System.Threading.Tasks;

namespace MapLib.DataSources.Raster;

internal class ExistingRasterDataSource : BaseRasterDataSource
{
    public override string Name => "Existing Raster Data";

    public override Srs Srs => RasterData.Srs;
    public override Bounds? Bounds => RasterData.Bounds;
    public override bool IsBounded => true;
    public int WidthPx => RasterData.WidthPx;
    public int HeightPx => RasterData.HeightPx;

    private RasterData RasterData { get; }

    public ExistingRasterDataSource(RasterData rasterData)
    {
        RasterData = rasterData;
    }

    public override Task<RasterData> GetData(Srs? destSrs)
    {
        if (destSrs == null || destSrs == Srs)
        {
            return Task.FromResult(RasterData);
        }
        else
        {
            // TODO: Reproject in-memory raster

            using Dataset srcDataset = RasterData.ToInMemoryGdalDataset();

            string tempFilename = FileSystemHelpers.GetTempOutputFileName(
                ".tif", "raster_pre_warp");
            using Driver driver = Gdal.GetDriverByName("GTiff");
            using Dataset? tempDataset = driver.CreateCopy(
                tempFilename, srcDataset, 0, [], null, null);
            tempDataset.FlushCache();

            string warpedFilename = GdalUtils.Warp(tempFilename, destSrs);

            GdalDataSource reprojectedDataSource = new(warpedFilename);
            return reprojectedDataSource.GetData();
        }
    }

    public override Task<RasterData> GetData(Bounds boundsWgs84, Srs? destSrs)
        => GetData(destSrs); // for now, no cropping
}
