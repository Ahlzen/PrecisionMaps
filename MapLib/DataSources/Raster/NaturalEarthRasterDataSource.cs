using System.Threading.Tasks;
using MapLib.Geometry;

namespace MapLib.DataSources.Raster;

public class NaturalEarthRasterDataSource : BaseRasterDataSource
{
    public override string Name { get; }
    public override string Srs { get; }
    public override Bounds? Bounds { get; }
    public override bool IsBounded { get; }
    public override Task<RasterData> GetData()
    {
        throw new NotImplementedException();
    }

    public override Task<RasterData> GetData(string destSrs)
    {
        throw new NotImplementedException();
    }

    public override Task<RasterData> GetData(Bounds boundsWgs84)
    {
        throw new NotImplementedException();
    }

    public override Task<RasterData> GetData(Bounds boundsWgs84, string destSrs)
    {
        throw new NotImplementedException();
    }
}