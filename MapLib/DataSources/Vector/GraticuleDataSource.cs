using System.Threading.Tasks;
using MapLib.GdalSupport;
using MapLib.Geometry;

namespace MapLib.DataSources.Vector;

public class GraticuleDataSource : BaseVectorDataSource
{
    public override string Name => "Graticule";
    public override Srs Srs => Srs.Wgs84; // plain lon/lat

    public override bool IsBounded => false;

    private static readonly Bounds _bounds = new Bounds(-180, 180, -90, 90);
    public override Bounds? Bounds => _bounds;

    /// <summary>
    /// Interval between graticule lines in the X direction (longitudes)
    /// </summary>
    public double XInterval { get; set; } = 10;

    /// <summary>
    /// Interval between graticule lines in the Y direction (latitudes)
    /// </summary>
    public double YInterval { get; set; } = 10;

    /// <summary>
    /// Useful for adding an offset.
    /// </summary>
    public double XStart { get; set; } = _bounds.XMin;

    /// <summary>
    /// Useful for adding an offset.
    /// </summary>
    public double YStart { get; set; } = _bounds.YMin;

    /// <summary>
    /// Number of line segments between each grid line.
    /// </summary>
    /// <remarks>
    /// For projections with straight latitudes and longitudes, this
    /// can be left at 1. For smooth curved lines, increase this value.
    /// </remarks>
    public int Segments { get; set; } = 20;


    public override Task<VectorData> GetData() => GetData(_bounds);

    public override Task<VectorData> GetData(Bounds boundsWgs84)
    {
        VectorDataBuilder builder = new();
        
        // Lines of latitude
        double segmentIntervalX = XInterval / Segments;
        int segmentCountX = (int)Math.Ceiling(boundsWgs84.Width / segmentIntervalX) + 1;
        for (double y = YStart; y < boundsWgs84.YMax + YInterval; y += YInterval)
        {
            List<Coord> lineCoords = new(segmentCountX);
            for (double x = XStart; x < boundsWgs84.XMax + segmentIntervalX; x += segmentIntervalX)
            {
                if (x >= (boundsWgs84.XMin - segmentIntervalX))
                    lineCoords.Add(new Coord(x, y));
            }
            builder.Lines.Add(new Line(
                lineCoords.ToArray(),
                [new("Latitude", y.ToString("F3"))])); // TODO: Adaptive/max decimals. W/E/N/S
        }

        // Lines of longitude
        double segmentIntervalY = YInterval / Segments;
        int segmentCountY = (int)Math.Ceiling(boundsWgs84.Width / segmentIntervalY) + 1;
        for (double x = XStart; x < boundsWgs84.XMax + XInterval; x += XInterval)
        {
            List<Coord> lineCoords = new(segmentCountY);
            for (double y = YStart; y < boundsWgs84.YMax + segmentIntervalY; y += segmentIntervalY)
            {
                if (y >= (boundsWgs84.YMin - segmentIntervalY))
                    lineCoords.Add(new Coord(x, y));
            }
            builder.Lines.Add(new Line(
                lineCoords.ToArray(),
                [new ("Longitude", x.ToString("F3"))])); // TODO: Adaptive/max decimals. W/E/N/S
        }

        return Task.FromResult(builder.ToVectorData(Srs));
    }
}