using OSGeo.GDAL;

namespace MapLib.GdalSupport;

public static class OgrUtils
{
    public static Dataset GetVectorDataset(string filename)
        => Gdal.OpenEx(filename, (uint) GdalConst.OF_RASTER, null, null, null);

    public static string GetSrsAsWkt(Dataset vectorDataset)
    {
        string wkt;
        vectorDataset.GetLayer(0).GetSpatialRef().ExportToPrettyWkt(out wkt, 0);
        return wkt;
    }

    //public static string? GetSrsAsWkt(DataSource dataSource)
    //{

    //    // TODO: https://gis.stackexchange.com/questions/239112/how-to-get-coordinate-system-info-from-shapefile-using-gdal-ogr

    //    string projection = dataSource.get .GetProjectionRef();
    //    if (projection == null)
    //        throw new ApplicationException("Could not determine projection from GDAL Dataset.");

    //    SpatialReference srs = new SpatialReference(null);
    //    if (srs.ImportFromWkt(ref projection) == 0)
    //    {
    //        srs.ExportToPrettyWkt(out wkt, 0);
    //        string wkt;
    //        return wkt;
    //    }
    //    else
    //    {
    //    }
    //        return projection;
    //}
}