using System.Threading.Tasks;
using MapLib.GdalSupport;
using MapLib.Geometry;

namespace MapLib.DataSources.Vector;

public class ExistingVectorDataSource(VectorData vectorData) : BaseVectorDataSource
{
    public override string Name => "Existing Vector Data";
    public override Srs Srs => VectorData.Srs;
    public override Bounds? Bounds => VectorData.Bounds;
    public override bool IsBounded => true;
        
    public VectorData VectorData { get; } = vectorData;

    public override Task<VectorData> GetData(Srs? destSrs)
    {
        Task<VectorData> data = Task.FromResult(VectorData);
        return ReprojectIfNeeded(data.Result, destSrs);
    }

    public override Task<VectorData> GetData(Bounds boundsWgs84, Srs? destSrs)
        => GetData(destSrs); // for now
}