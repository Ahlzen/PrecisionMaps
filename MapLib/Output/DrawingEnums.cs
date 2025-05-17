namespace MapLib.Output;

/// <summary>
/// Values correspond to SVG stroke-linecap.
/// </summary>
public enum LineCap
{
    Butt,
    Round,
    Square
}

public enum LineJoin
{
    Miter,
    Round,
    Bevel
}

[Flags]
public enum StyleApplicability
{
    Point = 1,
    Line = 2, // linestrings, polygons, multipolygons
    Raster = 4
}

public enum TextHAlign
{
    Left,
    Center,
    Right
}

public enum SymbolType
{
    Circle,
    Square,
    Star,
    Image,
}