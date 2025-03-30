using MapLib.FileFormats;
using MapLib.FileFormats.Vector;
using MapLib.GdalSupport;
using MapLib.Geometry;
using OSGeo.GDAL;
using System.IO;
using Transformer = MapLib.GdalSupport.Transformer;

namespace MapLib.DataSources.Vector;

public class VectorFileDataSource : BaseVectorDataSource
{
    public override string Name => "Vector file";
    public override string Srs { get; }
    public override Bounds? Bounds { get; }
    public string Filename { get; }

    private IVectorFormatReader _reader;

    public VectorFileDataSource(string filename)
    {
        Filename = filename;
        string extension = Path.GetExtension(filename).ToLower();
        switch (extension)
        {
            case ".osm":
                _reader = new OsmDataReader();
                Srs = Transformer.WktWgs84;
                break;
            case ".geojson":
                _reader = new GeoJsonDataReader();
                Srs = Transformer.WktWgs84;
                break;
            default: // try OGR
                {
                    using Dataset dataset = OgrUtils.GetVectorDataset(filename);
                    Srs = OgrUtils.GetSrsAsWkt(dataset);
                    Bounds = OgrUtils.GetDatasetBounds(dataset);
                    _reader = new OgrDataReader();
                }
                break;
        }
    }

    public override VectorData GetData(Bounds bounds)
    {
        return _reader.ReadFile(Filename);
    }
}