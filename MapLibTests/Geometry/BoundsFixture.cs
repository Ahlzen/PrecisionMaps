using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Tests.Geometry;

[TestFixture]
public class BoundsFixture : BaseFixture
{
    [Test]
    public void TestFromCoords()
    {
        Bounds b = Bounds.FromCoords(TestPolygon1.Coords);
        Assert.That(b.XMin == 1);
        Assert.That(b.XMax == 8);
        Assert.That(b.YMin == -2);
        Assert.That(b.YMax == 5);
    }

    [Test]
    public void TestCreate()
    {
        Bounds b = new(4, 8, 2, 4);
        Assert.That(b.XMin == 4);
        Assert.That(b.XMax == 8);
        Assert.That(b.YMax == 4);
        Assert.That(b.YMin == 2);
        Assert.That(b.Width == 4);
        Assert.That(b.Height == 2);
        Assert.That(b.TopLeft == new Coord(4, 4));
        Assert.That(b.TopRight == new Coord(8, 4));
        Assert.That(b.BottomLeft == new Coord(4, 2));
        Assert.That(b.BottomRight == new Coord(8, 2));
        Assert.That(b.Center == new Coord(6, 3));
    }

    [Test]
    public void TestResizeAndCenterX()
    {
        Bounds b = new(4, 8, 2, 4);
        
        Bounds wide = b.ResizeAndCenterX(newWidth: 6);
        Assert.That(wide.BottomLeft == new Coord(3, 2));
        Assert.That(wide.TopRight == new Coord(9, 4));

        Bounds narrow = b.ResizeAndCenterX(newWidth: 2);
        Assert.That(narrow.BottomLeft == new Coord(5, 2));
        Assert.That(narrow.TopRight == new Coord(7, 4));
    }

    [Test]
    public void TestResizeAndCenterY()
    {

        Bounds b = new(4, 8, 2, 4);
        Bounds tall = b.ResizeAndCenterY(newHeight: 8);
        Assert.That(tall.BottomLeft == new Coord(4, -1));
        Assert.That(tall.TopRight == new Coord(8, 7));

        Bounds shallow = b.ResizeAndCenterY(newHeight: 1);
        Assert.That(shallow.BottomLeft == new Coord(4, 2.5));
        Assert.That(shallow.TopRight == new Coord(8, 3.5));
    }

    [Test]
    public void TestIntersection()
    {
        Bounds b1 = new(4, 8, 2, 4);
        Bounds b2 = new(5, 7, 1, 5); // partial overlap with b1
        Bounds b3 = new(8, 10, 2, 4); // shares side with b1
        Bounds b4 = new(8, 9, 4, 5); // shares corner with b1
        Bounds b5 = new(3, 9, 1, 5); // completely encompasses b1
        Bounds b6 = new(12, 13, 12, 13); // no overlap our touching

        Assert.That(b1.Intersection(b2), Is.EqualTo(new Bounds(5, 7, 2, 4)));
        Assert.That(b1.Intersection(b3), Is.Null);
        Assert.That(b1.Intersection(b4), Is.Null);
        Assert.That(b1.Intersection(b5), Is.EqualTo(b1));
        Assert.That(b1.Intersection(b6), Is.Null);
    }
}
