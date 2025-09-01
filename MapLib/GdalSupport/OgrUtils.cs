using MapLib.Geometry;
using OSGeo.GDAL;
using System.IO;

namespace MapLib.GdalSupport;

public static class OgrUtils
{
    public static Dataset GetVectorDataset(string filename)
    {
        if (!File.Exists(filename))
            throw new FileNotFoundException("File not found: " +  filename);

        Dataset dataset = Gdal.OpenEx(filename, (uint)GdalConst.OF_VECTOR, null, null, null);
        if (dataset == null)
        {
            int errorCode = Gdal.GetLastErrorNo();
            string errorMessage = Gdal.GetLastErrorMsg();
            throw new ApplicationException($"Failed to open OGR dataset. {errorCode}: {errorMessage}");
        }

        return dataset;
    }

    public static string GetSrsAsWkt(Dataset vectorDataset)
    {
        string wkt;
        vectorDataset.GetLayer(0).GetSpatialRef().ExportToPrettyWkt(out wkt, 0);
        return wkt;
    }

    public static Bounds GetDatasetBounds(Dataset vectorDataset)
    {
        // TODO
        throw new NotImplementedException();
    }
}