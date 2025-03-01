using MapLib.GdalSupport;
using MapLib.Geometry;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace MapLib.FileFormats.Vector;

public class OsmDataReader : IVectorFormatReader
{
    // Default tag filter
    private static List<string> DefaultIgnoredTags = new List<string> {
            "note", "note:*", "source", "source_ref", "source:*",
            "attribution", "comment", "fixme", "tiger:*", "NHD:*",
            "nhd:*", "gnis:*", "massgis:*"
        };

    public OsmDataReader()
    {
    }

    public VectorData ReadFile(string filename)
    {
        Debug.Assert(!string.IsNullOrEmpty(filename));
        //_filename = filename;

        string xmlData = File.ReadAllText(filename);
        VectorData data = ParseOsmXml(xmlData);

        return data;
    }

    #region Data retrieval

    //private string GetXmlDataFromOverpass()
    //{
    //    // TODO: Save file and add caching

    //    // Example: http://overpass-api.de/api/map?bbox=-71.28,41.98,-70.62,42.53
    //    string baseUrl = "http://overpass-api.de/api/map";
    //    string bounds = $"{_bounds.XMin},{_bounds.YMin},{_bounds.XMax},{_bounds.YMax}";
    //    string url = $"{baseUrl}?bbox={bounds}";
    //    using (var wc = new WebClient())
    //    {
    //        byte[] data = wc.DownloadData(new Uri(url)); // TODO: Async
    //        string xml = Encoding.UTF8.GetString(data, 0, data.Length);
    //        return xml;
    //    }
    //}

    #endregion

    #region OSM XML Data Parsing

    // Example:
    // <?xml version="1.0" encoding="UTF-8"?>
    // <osm version="0.6" generator="CGImap 0.0.2">
    //   <bounds minlat="54.0889580" minlon="12.2487570" maxlat="54.0913900" maxlon="12.2524800"/>
    //   <node id="298884269" lat="54.0901746" lon="12.2482632" user="SvenHRO" uid="46882" visible="true" version="1" changeset="676636" timestamp="2008-09-21T21:37:45Z"/>
    //   ...
    
    /// <exception cref="FormatException">
    /// Thrown if the OSM XML data is invalid or can otherwise not be parsed.
    /// </exception>
    public VectorData ParseOsmXml(string xmlData)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xmlData);

        // These dicts hold all nodes/ways/multipolys, since
        // they may be references by other objects.
        var nodes = new Dictionary<long, Point>();
        var ways = new Dictionary<long, Line>();
        var multiPolygons = new Dictionary<long, MultiPolygon>();

        // The builde holds only objects that are to be included
        // in the final data (for example, not nodes with no tags,
        // that only make up coordinates in lines).
        VectorDataBuilder builder = new();

        XmlNode? rootNode = doc.ChildNodes[1];
        if (rootNode == null)
            throw new FormatException("Root node not found in OSM XML data.");

        foreach (XmlNode xmlNode in rootNode.ChildNodes)
        {
            switch (xmlNode.Name)
            {
                case "node":
                    ParseNode(xmlNode, nodes, builder);
                    break;
                case "way":
                    ParseWay(xmlNode, nodes, ways, builder);
                    break;
                case "relation":
                    ParseRelation(xmlNode, ways, multiPolygons, builder);
                    break;
                case "bounds":
                    break; // don't care. we calculate our own
                default:
                    Debug.Fail("Unknown node type: " + xmlNode.Name);
                    break;
            }
        }

        // OSM data is plain lon/lat WGS84
        return builder.ToVectorData(Transformer.WktWgs84);
    }

    // Example:
    // <node id="62546181" visible="true" version="2" changeset="64199720" timestamp="2018-11-05T14:23:00Z" user="Rassilon" uid="9008948" lat="42.1914593" lon="-70.9153779">
    //   <tag k="attribution" v="Office of Geographic and Environmental Information (MassGIS)"/>
    //   <tag k="source" v="massgis_import_v0.1_20071009085031"/>
    // </node>

    private void ParseNode(XmlNode xmlNode,
        Dictionary<long, Point> nodes,
        VectorDataBuilder builder)
    {
        long id = long.Parse(xmlNode.Attributes?["id"]?.Value ?? "");
        var coord = new Coord(
            double.Parse(xmlNode.Attributes?["lon"]?.Value ?? ""),
            double.Parse(xmlNode.Attributes?["lat"]?.Value ?? ""));
        TagList tags = ParseTags(xmlNode);
        Point point = new(coord, tags);
        nodes.Add(id, point);
        if (tags.Length > 0)
            builder.Points.Add(point);

    }

    // Example:
    // <way id="697610604" visible="true" version="1" changeset="71306221" timestamp="2019-06-16T17:35:02Z" user="arielatom" uid="9568985">
    //   <nd ref="6551351658"/>
    //   <nd ref="6551351659"/>
    //   <nd ref="6551351660"/>
    //   <tag k="golf" v="hole"/>
    //   <tag k="name" v="18"/>
    //   <tag k="par" v="4"/>
    //   <tag k="ref" v="18"/>
    // </way>

    private void ParseWay(XmlNode xmlNode,
        Dictionary<long, Point> nodes,
        Dictionary<long, Line> ways,
        VectorDataBuilder builder)
    {
        // TODO: Add a mechanism to determine whether an OSM line should
        // be a way or a polygon.

        long id = long.Parse(xmlNode.Attributes?["id"]?.Value ?? "");
        Coord[] coords = xmlNode.ChildNodes
                .Cast<XmlNode>()
                .Where(child => child.Name == "nd")
                .Select(child => long.Parse(child.Attributes?["ref"]?.Value ?? ""))
                .Select(pointId => nodes[pointId].Coord)
                .ToArray();
        TagList tags = ParseTags(xmlNode);
        Line line = new(coords, tags);
        ways.Add(id, line);
        if (tags.Length > 0)
            builder.Lines.Add(line);

    }

    // Example:
    // <relation id="56688" user="kmvar" uid="56190" visible="true" version="28" changeset="6947637" timestamp="2011-01-12T14:23:49Z">
    //   <member type="node" ref="294942404" role=""/>
    //   ...
    //   <member type="node" ref="364933006" role=""/>
    //   <member type="way" ref="4579143" role=""/>
    //   ...
    //   <member type="node" ref="249673494" role=""/>
    //   <tag k="name" v="Küstenbus Linie 123"/>
    //   <tag k="network" v="VVW"/>
    //   <tag k="operator" v="Regionalverkehr Küste"/>
    //   <tag k="ref" v="123"/>
    //   <tag k="route" v="bus"/>
    //   <tag k="type" v="route"/>
    // </relation>
    private void ParseRelation(XmlNode xmlNode,
        Dictionary<long, Line> ways,
        Dictionary<long, MultiPolygon> multiPolygons,
        VectorDataBuilder builder)
    {
        long id = long.Parse(xmlNode.Attributes?["id"]?.Value ?? "");
        TagList tags = ParseTags(xmlNode);

        // We're (currently) only interested in multipolygon relations
        KeyValuePair<string, string>? typeTag = tags.Where(tag => tag.Key == "type").SingleOrDefault();
        if (typeTag == null || typeTag.Value.Value != "multipolygon")
            return;

        // TODO: Parse routes and other types of relations?

        Coord[][] polygons = xmlNode.ChildNodes
            .Cast<XmlNode>()
            .Where(child => child.Name == "member")
            .Where(child => child.Attributes?["type"]?.Value == "way")
            .Select(child => long.Parse(child.Attributes?["ref"]?.Value ?? ""))
            .Where(ways.ContainsKey) // no guarantee we have all relation members
            .Select(wayId => ways[wayId].Coords.ToArray())
            .ToArray();
        MultiPolygon multiPolygon = new(polygons, tags);
        multiPolygons.Add(id, multiPolygon);
        if (tags.Length > 0)
            builder.MultiPolygons.Add(multiPolygon);
    }

    protected TagList ParseTags(XmlNode node)
    {
        return node.ChildNodes
            .Cast<XmlNode>()
            .Where(child => child.Name == "tag")
            .Where(child => IncludeTag(child.Attributes?["k"]?.Value ?? ""))
            .Select(child => new KeyValuePair<string, string>(
                    child.Attributes?["k"]?.Value ?? "",
                    child.Attributes?["v"]?.Value ?? ""))
            .ToArray();
    }

    protected bool IncludeTag(string tagName)
    {
        foreach (string ignoredTag in DefaultIgnoredTags)
        {
            if (ignoredTag.EndsWith("*"))
            {
                if (tagName.StartsWith(ignoredTag.Substring(0, ignoredTag.Length - 1))) return false;
            }
            else
            {
                if (tagName == ignoredTag) return false;
            }
        }
        return true;
    }

    #endregion
}
