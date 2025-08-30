using MapLib.FileFormats;
using MapLib.FileFormats.Vector;
using MapLib.GdalSupport;
using MapLib.Geometry;
using OSGeo.GDAL;
using System.IO;
using System.Threading.Tasks;
using Transformer = MapLib.GdalSupport.Transformer;

namespace MapLib.DataSources.Vector;

public class VectorFileDataSource : BaseVectorDataSource
{
    public override string Name => "Vector file";
    public override Srs Srs { get; }
    public override Bounds? Bounds { get; }
    public string Filename { get; }

    public override bool IsBounded => true;

    private IVectorFormatReader _reader;

    public VectorFileDataSource(string filename)
    {
        Filename = filename;
        string extension = Path.GetExtension(filename).ToLower();
        switch (extension)
        {
            case ".osm":
                _reader = new OsmDataReader();
                Srs = Srs.Wgs84;
                break;
            case ".geojson":
                _reader = new GeoJsonDataReader();
                Srs = Srs.Wgs84;
                break;
            default: // try OGR
                {
                    using Dataset dataset = OgrUtils.GetVectorDataset(filename);
                    Srs = Srs.FromDataset(dataset);
                    // TODO:
                    //Bounds = OgrUtils.GetDatasetBounds(dataset);
                    _reader = new OgrDataReader();
                }
                break;
        }
    }

    public override Task<VectorData> GetData()
        => Task.FromResult(_reader.ReadFile(Filename));

    public override Task<VectorData> GetData(Bounds bounds)
        => GetData();
}