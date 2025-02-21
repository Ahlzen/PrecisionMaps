using MapLib.Geometry;
using System.IO;
using System.Text.Json;

namespace MapLib.FileFormats.Vector;

public class GeoJsonDataReader : IVectorFormatReader
{
    public VectorData ReadFile(string filename)
    {
        // TODO: Is a full deserialization into a JsonDocument performant
        // and memory-efficient enough for large GeoJSON files?
        // We should perhaps do some benchmarking....

        string json = File.ReadAllText(filename);
        JsonDocument doc = JsonDocument.Parse(json);
        VectorDataBuilder builder = new();

        // Parse document
        JsonElement root = doc.RootElement;

        string? type = GetStringProperty(root, "type", "root");
        switch (type)
        {
            case "Feature":
                ParseFeature(root, builder);
                break;
            case "FeatureCollection":
                ParseFeatureCollection(root, builder);
                break;
            default:
                throw new NotSupportedException(
                    $"GeoJson: Unsupported root type value: \"{type}\".");
        }
        return builder.ToVectorData();
    }

    private void ParseFeatureCollection(JsonElement parent, VectorDataBuilder builder)
    {
        JsonElement featuresArray = GetProperty(parent, "features", JsonValueKind.Array, "FeatureCollection");
        foreach (JsonElement featureElement in featuresArray.EnumerateArray())
            ParseFeature(featureElement, builder);
    }

    private void ParseFeature(JsonElement parent, VectorDataBuilder builder, TagList? existingTags = null)
    {
        //string? type = GetTypeValue(parent, true);
        string? type = GetStringProperty(parent, "type", "Feature");

        if (type != "Feature")
            throw new NotSupportedException(
                $"GeoJson: Unsupported element type value: \"{type}\".");
        // Parse properties
        TagList tags = existingTags ?? ParseTags(parent);

        // Parse geometry
        JsonElement geometryObject = GetProperty(parent, "geometry", JsonValueKind.Object, "Feature");
        string? geometryType = GetStringProperty(geometryObject, "type", "geometry");
        switch (geometryType)
        {
            case "Point": {
                    JsonElement coordsArray = GetCoordsArray(geometryObject);
                    Coord coord = ParseCoords0D(coordsArray);
                    builder.Points.Add(new Point(coord, tags));
                    break;
            }
            case "LineString": {
                    JsonElement coordsArray = GetCoordsArray(geometryObject);
                    Coord[] coords = ParseCoords1D(coordsArray).ToArray();
                    builder.Lines.Add(new Line(coords, tags));
                    break;
                }
            case "Polygon": {
                    JsonElement coordsArray = GetCoordsArray(geometryObject);
                    Coord[][] coords = ParseCoords2D(coordsArray).Select(ca => ca.ToArray()).ToArray();
                    // If it's a single polygon with no holes, we can add it as a polygon.
                    // If it has holes, our data model considers it a multipolygon:
                    if (coords.Length == 1)
                        builder.Polygons.Add(new Polygon(coords[0], tags));
                    else
                        builder.MultiPolygons.Add(new MultiPolygon(coords, tags));
                    break;
                }
            case "MultiPoint": {
                    JsonElement coordsArray = GetCoordsArray(geometryObject);
                    Coord[] coords = ParseCoords1D(coordsArray).ToArray();
                    builder.MultiPoints.Add(new MultiPoint(coords, tags));
                    break;
                }
            case "MultiLineString": {
                    JsonElement coordsArray = GetCoordsArray(geometryObject);
                    Coord[][] coords = ParseCoords2D(coordsArray).Select(ca => ca.ToArray()).ToArray();
                    builder.MultiLines.Add(new MultiLine(coords, tags));
                    break;
                }
            case "MultiPolygon":
                {
                    // NOTE: we don't have a structure that's equivalent to GeoJSON's
                    // multipolygon. A single GeoJSON polygon may be the equivalent of
                    // out of our model's multipolygons. Hence we add each one individually
                    JsonElement coordsArray = GetCoordsArray(geometryObject);
                    IEnumerable<IEnumerable<IEnumerable<Coord>>> coordsList = ParseCoords3D(coordsArray);
                    foreach (Coord[][] coords in coordsList.Select(ca => ca.ToArray()).ToArray())
                    {
                        // If it's a single polygon with no holes, we can add it as a polygon.
                        // If it has holes, our data model considers it a multipolygon:
                        if (coords.Length == 1)
                            builder.Polygons.Add(new Polygon(coords[0], tags));
                        else
                            builder.MultiPolygons.Add(new MultiPolygon(coords, tags));
                    }
                    break;
                }
            case "GeometryCollection":
                JsonElement geometriesArray = GetProperty(
                    parent, "geometries", JsonValueKind.Array, "GeometryCollection");
                foreach (JsonElement child in geometriesArray.EnumerateArray())
                    ParseFeature(child, builder, tags);
                break;
            default:
                throw new NotSupportedException(
                    $"GeoJson: Unsupported geometry type: \"{geometryType}\".");
        }
    }

    public TagList ParseTags(JsonElement parent)
    {
        JsonElement propertyObject = GetProperty(parent, "properties", JsonValueKind.Object, "Feature");
        List<KeyValuePair<string, string>> tags = new();
        foreach (JsonProperty property in propertyObject.EnumerateObject())
            tags.Add(new KeyValuePair<string, string>(property.Name, property.Value.ToString() ?? ""));
        return tags.ToArray();
    }

    public JsonElement GetCoordsArray(JsonElement parent)
        => GetProperty(parent, "coordinates", JsonValueKind.Array, "geometry");

    /// <summary>
    /// Parses a single coord (0-dimensional array of coords)
    /// </summary>
    /// <param name="singleCoordArray">
    /// For a single coordinate.
    /// E.g. [-75.334, 42.818]
    /// </param>
    public Coord ParseCoords0D(JsonElement singleCoordArray)
    {
        int arrayLength = singleCoordArray.GetArrayLength();
        if (arrayLength < 2 || arrayLength > 3)
            throw new FormatException("GeoJson: Coordinate does not have the expected number of elements.");
        double x, y;
        var iterator = singleCoordArray.EnumerateArray();
        iterator.MoveNext();
        x = iterator.Current.GetDouble();
        iterator.MoveNext();
        y = iterator.Current.GetDouble();
        return new Coord(x, y);
    }

    /// <summary>
    /// Parses a list of coords (1-dimensional array of coords)
    /// </summary>
    /// <param name="coordsArray">
    /// For a set of points, a line or a polygon with no holes.
    /// E.g. [[-75.334, 42.818], [-73.112, 42.909]]
    /// </param>
    public IEnumerable<Coord> ParseCoords1D(JsonElement coordsArray)
    {
        foreach (JsonElement coordElement in coordsArray.EnumerateArray())
            yield return ParseCoords0D(coordElement);
    }

    /// <summary>
    /// Parses a list of list of coords (2-dimensional array of coords)
    /// </summary>
    /// <param name="coordsArray">
    /// For a multiline or a polygon with hole(s).
    /// E.g. [[[x11, y11], [x12, y12], ...], [[x21, y21], [x22, y22], ...], ...]
    /// </param>
    public IEnumerable<IEnumerable<Coord>> ParseCoords2D(JsonElement coordsArray)
    {
        foreach (JsonElement coordsElement in coordsArray.EnumerateArray())
            yield return ParseCoords1D(coordsElement);
    }

    /// <summary>
    /// Parses a list of list of list of coords (3-dimensional array of coords)
    /// </summary>
    /// <param name="coordsArray">
    /// For a multipolygon (several polygons) optionally with hole(s).
    /// </param>
    public IEnumerable<IEnumerable<IEnumerable<Coord>>> ParseCoords3D(JsonElement coordsArray)
    {
        foreach (JsonElement coordsElement in coordsArray.EnumerateArray())
            yield return ParseCoords2D(coordsElement);
    }

    private string? GetStringProperty(JsonElement parent, string propertyName, string parentDescription)
        => GetProperty(parent, propertyName, JsonValueKind.String, parentDescription).GetString();

    private JsonElement GetProperty(
        JsonElement parent, string propertyName,
        JsonValueKind expectedKind, string parentDescription)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement element))
            throw new FormatException(
                $"GeoJson: Element \"{parentDescription}\" has no property \"{propertyName}\".");
        if (element.ValueKind != expectedKind)
            throw new FormatException(
                $"GeoJson: Property \"{propertyName}\" of \"{parentDescription}\" property must be {expectedKind}.");
        return element;
    }
}
