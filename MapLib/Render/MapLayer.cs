﻿using System.Drawing;

namespace MapLib.Render;

public abstract class MapLayer
{
    public MapLayer(string name, string dataSourceName)
    {
        Name = name;
        DataSourceName = dataSourceName;
    }

    public string Name { get; }
    public string DataSourceName { get; }
}

public class VectorMapLayer : MapLayer
{
    public TagFilter? Filter { get; }

    // TODO: break out and expand
    public Color? FillColor { get; }
    public Color? StrokeColor { get; }
    public double? StrokeWidth { get; }

    public VectorMapLayer(string name, string dataSourceName,
        TagFilter? filter = null,
        Color? fillColor = null,
        Color? strokeColor = null,
        double? strokeWidth = null)
        : base(name, dataSourceName)
    {
        Filter = filter;
        FillColor = fillColor;
        StrokeColor = strokeColor;
        StrokeWidth = strokeWidth;
    }
}

public class TagFilter
{
    private List<(string, string?)> Tags { get; } = new();

    /// <param name="tagName">Name. Case sensitive.</param>
    /// <param name="tagValue">Value. Case sensitive.
    /// Null is wildcard (name must match, but can be any value)
    /// </param>
    public TagFilter(string tagName, string? tagValue = null)
    {
        Tags.Add((tagName, tagValue));
    }

    public TagFilter(IEnumerable<(string, string?)> tags)
    {
        Tags.AddRange(tags);
    }

    public TagFilter(params (string, string?)[] tags)
    {
        Tags.AddRange(tags);
    }

    public bool Matches(TagList featureTags)
    {
        // Find key
        foreach (KeyValuePair<string, string> featureTag in featureTags)
        {
            foreach ((string filterKey, string? filterValue) in Tags)
                if (featureTag.Key == filterKey)
                {
                    if (filterValue == null)
                        return true;
                    else if (filterValue == featureTag.Value)
                        return true;
                }
        }
        return false;
    }

    public VectorData Filter(VectorData source)
    {
        return new VectorData(source.Srs,
            source.Points.Where(p => Matches(p.Tags)).ToArray(),
            source.MultiPoints.Where(mp => Matches(mp.Tags)).ToArray(),
            source.Lines.Where(l => Matches(l.Tags)).ToArray(),
            source.MultiLines.Where(ml => Matches(ml.Tags)).ToArray(),
            source.Polygons.Where(p => Matches(p.Tags)).ToArray(),
            source.MultiPolygons.Where(mp => Matches(mp.Tags)).ToArray());
    }
}