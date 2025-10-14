using MapLib.GdalSupport;
using MapLib.Geometry;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace MapLib.FileFormats.Vector;

/// <summary>
/// Base class for OSM data readers (OSM XML, Overpass JSON, etc)
/// </summary>
public abstract class OsmDataReader : IVectorFormatReader
{
    public abstract VectorData ReadFile(string filePath);
    public abstract VectorData Parse(string data);

    // Default tag filter
    protected static List<string> DefaultIgnoredTags = new List<string>
    {
        "note", "note:*", "source", "source_ref", "source:*",
        "attribution", "comment", "fixme", "tiger:*", "NHD:*",
        "nhd:*", "gnis:*", "massgis:*"
    };

    /// <summary>
    /// Tags for which closed ways should be considered polygons rather
    /// than lines.
    /// </summary>
    /// <remarks>
    /// Adapted from osm2pgsql default.style
    /// https://github.com/osm2pgsql-dev/osm2pgsql/blob/master/default.style
    /// </remarks>
    protected static HashSet<string> PolygonTags = new HashSet<string>
    {
        "aeroway",
        "amenity",
        "area",
        "building",
        "harbour",
        "historic",
        "landuse",
        "leisure",
        "man_made",
        "military",
        "natural",
        "office",
        "place",
        "power",
        "public_transport",
        "shop",
        "sport",
        "tourism",
        "water",
        "waterway",
        "wetland",
    };
}