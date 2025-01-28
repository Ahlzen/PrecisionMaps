using System.Drawing;
using System.IO;
using MapLib;
using MapLib.FileFormats.Vector;
using MapLib.Output;

namespace MapLibTests.Output;

[TestFixture]
public class CanvasFixture : BaseFixture
{
    [Test]
    public void TestRenderMultipolygon()
    {
        // Read data
        OgrDataReader reader = new OgrDataReader();
        VectorData data = reader.ReadFile(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"));
        Console.WriteLine(FormatVectorDataSummary(data));

        double canvasWidth = 600;
        double canvasHeight = 600;
        VectorData transformedData = TransformToFit(data, canvasWidth, canvasHeight);

        BitmapCanvas canvas = new BitmapCanvas(canvasWidth, canvasHeight, Color.AntiqueWhite);
        CanvasLayer layer = canvas.AddNewLayer("water");
        
        layer.DrawLines(
            transformedData.Polygons.Select(p => p.Coords),
            1.2, Color.Navy, LineCap.Round, LineJoin.Round);
        foreach (var multipolygon in transformedData.MultiPolygons)
            layer.DrawLines(multipolygon, 1.2, Color.Navy, LineCap.Round, LineJoin.Round);
        var bitmap = canvas.GetBitmap();

        string filename = GetTempFileName(".png");
        bitmap.Save(filename);
        ShowFile(filename);
    }

    [Test]
    public void TestRenderShorelinePolygons()
    {
        // Read data
        OgrDataReader reader = new OgrDataReader();
        VectorData data = reader.ReadFile(
            Path.Join(TestDataPath, "Aaron River Reservoir.geojson"));
        Console.WriteLine(FormatVectorDataSummary(data));

        double canvasWidth = 600;
        double canvasHeight = 600;
        VectorData transformedData = TransformToFit(data, canvasWidth, canvasHeight);
        
        BitmapCanvas canvas = new BitmapCanvas(canvasWidth, canvasHeight, Color.AntiqueWhite);
        CanvasLayer layer = canvas.AddNewLayer("water");

        List<MultiPolygon> multiPolygons = new();
        multiPolygons.AddRange(transformedData.MultiPolygons);
        multiPolygons.AddRange(transformedData.Polygons.Select(p => p.AsMultiPolygon()));

        foreach (MultiPolygon multipolygon in multiPolygons)
        {
            MultiPolygon currentPolygon = multipolygon;

            double opacity = 1;
            double lineWidth = 1.2;
            for (int i = 0; i < 15; i++)
            {
                Color color = Color.FromArgb((int)(255*opacity), Color.Navy);
                layer.DrawLines(currentPolygon.Coords, lineWidth, color,
                    LineCap.Round, LineJoin.Round);

                if (currentPolygon.Count == 0)
                    break;

                opacity *= 0.7;
                lineWidth *= 0.9;

                currentPolygon = currentPolygon.Offset(-7);

            }


        }

        layer.DrawLines(
            transformedData.Polygons.Select(p => p.Coords),
            1.2, Color.Navy, LineCap.Round, LineJoin.Round);
        foreach (var multipolygon in transformedData.MultiPolygons)
            layer.DrawLines(multipolygon, 1.2, Color.Navy, LineCap.Round, LineJoin.Round);
        var bitmap = canvas.GetBitmap();

        string filename = GetTempFileName(".png");
        bitmap.Save(filename);
        ShowFile(filename);
    }

    ///// Helpers
    
    // TODO: Move?

    private VectorData TransformToFit(VectorData source, double width, double height)
    {
        Bounds sourceBounds = source.Bounds;
        double scale = Math.Min(
            width / sourceBounds.Width,
            height / sourceBounds.Height);
        double offsetX = -(sourceBounds.XMin * scale);
        double offsetY = -(sourceBounds.YMin * scale);
        return source.Transform(scale, offsetX, offsetY);
    }

//    private Coord[] Transform(Coord[] source, double scale, double offsetX, double offsetY)
//    {
//        var dest = new Coord[source.Length];
//        for (int i =0; i < source.Length; i++)
//            dest[i] = Transform(source[i], scale, offsetX, offsetY);
//        return dest;
//    }

//    private Coord Transform(Coord source, double scale, double offsetX, double offsetY)
//        => new(
//            source.X * scale + offsetX,
//            source.Y * scale + offsetY);
}
