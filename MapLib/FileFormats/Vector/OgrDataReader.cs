using MapLib.Geometry;
using OSGeo.OGR;
using System.Diagnostics;
using System.IO;

namespace MapLib.FileFormats.Vector;

public class OgrDataReader : IVectorFormatReader
{
    public VectorData ReadFile(string filename)
    {
        VectorDataBuilder builder = new();
        
        // Open data source
        using DataSource ds = Ogr.Open(filename, 0);
        if (ds == null)
        {
            throw new IOException(
                "Could not open file: " + filename);
        }

        for (int i = 0; i < ds.GetLayerCount(); i++)
        {
            // Open layer
            Layer layer = ds.GetLayerByIndex(i);
            if (layer == null)
            {
                throw new ApplicationException(
                    "Could not open layer " +  i);
            }
            Console.WriteLine($"Layer {i}: {layer.GetName()}");

            // Read features
            layer.ResetReading();
            Feature feature;
            do
            {
                feature = layer.GetNextFeature();
                if (feature != null)
                {
                    // Get tags
                    TagList tags = ReadTags(feature);

                    // Get geometry
                    OSGeo.OGR.Geometry geometry = feature.GetGeometryRef();
                    wkbGeometryType type = geometry.GetGeometryType();
                    switch (type)
                    {
                        case wkbGeometryType.wkbPoint:
                            Debug.Assert(geometry.GetPointCount() == 1);
                            ReadAndAddPoint(geometry, tags, builder);
                            break;

                        case wkbGeometryType.wkbMultiPoint:
                            Debug.Assert(geometry.GetGeometryCount() > 0);
                            int pointCount = geometry.GetGeometryCount();
                            for (int n = 0; n < pointCount; n++) {
                                OSGeo.OGR.Geometry subGeometry = geometry.GetGeometryRef(n);
                                ReadAndAddPoint(subGeometry, tags, builder);
                            }
                            break;

                        case wkbGeometryType.wkbLineString:
                            Debug.Assert(geometry.GetPointCount() > 0);
                            ReadAndAddLine(geometry, tags, builder);
                            break;

                        case wkbGeometryType.wkbMultiLineString:
                            Debug.Assert(geometry.GetGeometryCount() > 0);
                            int lineCount = geometry.GetGeometryCount();
                            for (int n = 0; n < lineCount; n++) {
                                OSGeo.OGR.Geometry subGeometry = geometry.GetGeometryRef(n);
                                ReadAndAddLine(subGeometry, tags, builder);
                            }
                            break;

                        case wkbGeometryType.wkbPolygon:
                            Debug.Assert(geometry.GetGeometryCount() > 0);
                            ReadAndAddPolygon(geometry, tags, builder);
                            break;

                        case wkbGeometryType.wkbMultiPolygon:
                            Debug.Assert(geometry.GetGeometryCount() > 0);
                            int polygonCount = geometry.GetGeometryCount();
                            for (int n = 0; n < polygonCount; n++) {
                                OSGeo.OGR.Geometry subGeometry = geometry.GetGeometryRef(n);
                                ReadAndAddPolygon(subGeometry, tags, builder);
                            }
                            break;

                        default:
                            throw new InvalidOperationException(
                                "Unsupported geometry type: " + type);
                    }
                }
            }
            while (feature != null);
        }

        return builder.ToVectorData();
    }

    private void ReadAndAddPoint(
        OSGeo.OGR.Geometry geometry,
        TagList tags,
        VectorDataBuilder builder)
    {
        Debug.Assert(geometry.GetPointCount() == 1);
        double[] rawCoords = new double[2];
        geometry.GetPoint(0, rawCoords);
        Coord coord = new(rawCoords[0], rawCoords[1]);
        builder.Points.Add(new Point(coord, tags));
    }

    private void ReadAndAddLine(
        OSGeo.OGR.Geometry geometry,
        TagList tags,
        VectorDataBuilder builder)
    {
        Coord[] coords = Read2DCoords(geometry);
        builder.Lines.Add(new Line(coords, tags));
    }

    private void ReadAndAddPolygon(
        OSGeo.OGR.Geometry geometry,
        TagList tags,
        VectorDataBuilder builder)
    {
        // OGR/SimpleFeatures polygons consist of one or more
        // individual linear rings:

        Debug.Assert(geometry.GetGeometryCount() > 0);

        var rings = ReadSubGeometry2DCoords(geometry);
        if (rings.Length > 1)
        {
            // Polygon has holes. We consider this a multipolygon.
            builder.MultiPolygons.Add(new MultiPolygon(rings, tags));
        }
        else if (rings.Length == 1)
        {
            builder.Polygons.Add(new Polygon(rings[0], tags));
        }
        else
        {
            // Are there any conditions why this would happen?
            throw new ApplicationException("No rings in polygon");
        }
    }


    private Coord[][] ReadSubGeometry2DCoords(
        OSGeo.OGR.Geometry parentGeometry)
    {
        int subGeometryCount = parentGeometry.GetGeometryCount();
        List<Coord[]> lines = new(subGeometryCount);
        for (int i = 0; i < subGeometryCount; i++)
        {
            OSGeo.OGR.Geometry geometry = parentGeometry.GetGeometryRef(i);
            Coord[] coords = Read2DCoords(geometry);
            lines.Add(coords);
        }
        return lines.ToArray();
    }

    private Coord[] Read2DCoords(OSGeo.OGR.Geometry geometry)
    {
        int pointCount = geometry.GetPointCount();
        Coord[] coords = new Coord[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            double[] rawCoords = new double[2];
            geometry.GetPoint(i, rawCoords);
            coords[i] = new Coord(rawCoords[0], rawCoords[1]);
        }
        return coords;
    }

    private TagList ReadTags(OSGeo.OGR.Feature feature)
    {
        int fieldCount = feature.GetFieldCount();
        TagList tags = new KeyValuePair<string, string>[fieldCount];
        for (int i = 0; i < fieldCount; i++)
        {
            FieldDefn fd = feature.GetFieldDefnRef(i);
            string key = fd.GetName();
            FieldType ft = fd.GetFieldType();
            string value;
            switch (ft)
            {
                case FieldType.OFTString:
                case FieldType.OFTWideString:
                    value = feature.GetFieldAsString(i);
                    break;
                case FieldType.OFTReal:
                    value = feature.GetFieldAsDouble(i).ToString();
                    break;
                case FieldType.OFTInteger:
                    value = feature.GetFieldAsInteger(i).ToString();
                    break;
                case FieldType.OFTInteger64:
                    value = feature.GetFieldAsInteger64(i).ToString();
                    break;
                case FieldType.OFTIntegerList:
                case FieldType.OFTInteger64List:
                    {
                        int[] ints = feature.GetFieldAsIntegerList(i, out int count);
                        value = "[" + string.Join(", ", ints.Select(d => d.ToString())) + "]";
                        break;
                    }
                case FieldType.OFTRealList:
                    {
                        double[] doubles = feature.GetFieldAsDoubleList(i, out int count);
                        value = "[" + string.Join(", ", doubles.Select(d => d.ToString())) + "]";
                        break;
                    }
                case FieldType.OFTStringList:
                case FieldType.OFTWideStringList:
                    {
                        string[] strings = feature.GetFieldAsStringList(i);
                        value = "[" + string.Join(", ", strings) + "]";
                        break;
                    }
                case FieldType.OFTDate:
                case FieldType.OFTTime:
                case FieldType.OFTDateTime:
                    value = feature.GetFieldAsISO8601DateTime(i, new string[] {});
                    break;
                case FieldType.OFTBinary:
                    value = "<binary>"; // we don't support this
                    break;
                default:
                    throw new InvalidOperationException(
                        "Unsupported field type: " + ft);
            }
            tags[i] = new KeyValuePair<string, string>(key, value);
        }
        return tags;
    }
}
