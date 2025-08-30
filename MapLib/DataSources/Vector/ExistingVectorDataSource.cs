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

    public override Task<VectorData> GetData()
        => Task.FromResult(VectorData);

    public override Task<VectorData> GetData(Bounds boundsWgs84)
        => Task.FromResult(VectorData); // for now
}