using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapLib.Geometry.Helpers;

/// <summary>
/// Simple area quadtree implementation for efficiently
/// determining whether bounding boxes overlap
/// (e.g. for text labels).
/// </summary>
/// <remarks>
/// This implementation allows items in both leaf nodes (when
/// fully fits within one quadrant) and parent nodes (when spanning
/// multiple child nodes).
/// This might lead to better or worse performance depending on the data.
/// </remarks>
public class QuadtreeNode
{
    // TODO: Having both an upper AND lower bound for items-per-node before
    // splitting/merging (i.e. some hysteresis) might help if we're doing
    // a lot of small removals and insertions.

    private readonly int _maxItemsPerNode; // TODO: Benchmark different values


    private readonly Bounds _bounds;
    
    private readonly List<Bounds> _items = new();

    // Child nodes. These are either all null or all non-null.
    private QuadtreeNode? _topLeft, _topRight, _bottomLeft, _bottomRight;

    public QuadtreeNode(int maxItemsPerNode, Bounds bounds)
    {
        _maxItemsPerNode = maxItemsPerNode;
        _bounds = bounds;
    }

    

    /// <returns>
    /// True if the item was added. False otherwise, for example
    /// if the item does not overlap the bounds of this quadtree.
    /// </returns>
    public bool Add(Bounds item)
    {
        if (!_bounds.Intersects(item))
            return false; // fully outside the bounds of this quadtree

        // Check if we're at max capacity
        if (_items.Count >= _maxItemsPerNode)
        {
            // Need to subdivide this quadtree node to fit the new item
            Subdivide();
        }

        if (!IsLeaf)
        {
            // Add item to the appropriate quadrant, if it fits fully within.
            QuadtreeNode? enclosingNode = GetFullyEnclosingChildNode(item);
            if (enclosingNode != null)
                return enclosingNode.Add(item);
        }

        // This is a leaf, or item doesn't fit whithin one
        // quadrant: Add directly to this note's items.
        _items.Add(item);
        return true;
    }

    /// <returns>
    /// True iff the item overlaps any existing item in this quadtree.
    /// </returns>
    public bool Overlaps(Bounds item)
    {
        //// First check if item overlaps any of this node's items
        //foreach (Bounds ownItem in _items)
        //    if (item.Intersects(ownItem))
        //        return true;

        //// Items in child nodes should all be fully enclosed
        //// by that node (or they would be on this node's list),
        //// so we only need to check at most one child:
        //QuadtreeNode? child = GetFullyEnclosingChildNode(item);
        //if (child != null)
        //    return child.Overlaps(item);

        //// No overlaps!
        //return false;

        return GetOverlappingItem(item) != null;
    }

    /// <returns>
    /// The (first) item in this node that overlaps the
    /// specified item, or null if none.
    /// </returns>
    public Bounds? GetOverlappingItem(Bounds item)
    {
        // First check if item overlaps any of this node's items
        foreach (Bounds ownItem in _items)
            if (item.Intersects(ownItem))
                return ownItem;

        // Items in child nodes should all be fully enclosed
        // by that node (or they would be on this node's list),
        // so we only need to check at most one child:
        //QuadtreeNode? child = GetFullyEnclosingChildNode(item);
        //if (child != null)
        //    return child.GetOverlappingItem(item);

        // Check subtrees
        if (!IsLeaf)
        {
            Bounds? overlapItem;
            overlapItem = _topLeft!.GetOverlappingItem(item);
            if (overlapItem != null) return overlapItem;
            overlapItem = _topRight!.GetOverlappingItem(item);
            if (overlapItem != null) return overlapItem;
            overlapItem = _bottomLeft!.GetOverlappingItem(item);
            if (overlapItem != null) return overlapItem;
            overlapItem = _bottomRight!.GetOverlappingItem(item);
            if (overlapItem != null) return overlapItem;
        }

        // No overlaps!
        return null;
    }

    /// <summary>
    /// Adds the specified item to the quadtree iff
    /// the item falls within the quadtree's bounds AND
    /// it does not overlap any other item.
    /// </summary>
    /// <returns>True if the item was added, false otherwise.</returns>
    public bool AddIfNotOverlapping(Bounds item)
    {
        return AddOrGetOverlap(item) == null;
        //if (Overlaps(item))
        //    return false;
        //return Add(item);
    }

    /// <summary>
    /// Adds the specified item and returns null, or returns
    /// the first item from the quadtree that overlaps the
    /// specified item.
    /// </summary>
    public Bounds? AddOrGetOverlap(Bounds item)
    {
        Bounds? overlappingItem = GetOverlappingItem(item);
        if (overlappingItem != null)
        {
            return overlappingItem;
        }
        Add(item);
        return null;
    }


    /// <summary>Removes the item from this quadtree, if exists.</summary>
    /// <returns>True iff an item was removed.</returns>
    public bool Remove(Bounds item)
    {
        throw new NotImplementedException();
    }



    /// <summary>
    /// Subdivides this node into four quadrants (child nodes).
    /// </summary>
    private void Subdivide()
    {
        if (!IsLeaf) return; // already split

        // Add child nodes
        _topLeft = new QuadtreeNode(_maxItemsPerNode,
            new Bounds(_bounds.TopLeft, _bounds.Center));
        _topRight = new QuadtreeNode(_maxItemsPerNode,
            new Bounds(_bounds.TopRight, _bounds.Center));
        _bottomLeft = new QuadtreeNode(_maxItemsPerNode,
            new Bounds(_bounds.BottomLeft, _bounds.Center));
        _bottomRight = new QuadtreeNode(_maxItemsPerNode,
            new Bounds(_bounds.BottomRight, _bounds.Center));

        // Move any item fully within a child node to that node
        foreach (Bounds item in _items.ToArray())
        {
            QuadtreeNode? enclosingNode = GetFullyEnclosingChildNode(item);
            if (enclosingNode != null)
            {
                _items.Remove(item);
                enclosingNode.Add(item);
            }
        }
    }

    /// <remarks>If one child is null, all are null and this is a leaf.</remarks>
    private bool IsLeaf => _topLeft == null;

    /// <returns>
    /// Returns the child node (quadrant) that fully encloses the
    /// specified item, or null if this is a leaf or the item
    /// extends multiple quadrants.
    /// </returns>
    private QuadtreeNode? GetFullyEnclosingChildNode(Bounds item)
    {
        if (IsLeaf) return null;
        if (item.IsFullyWithin(_topLeft!._bounds))
            return _topLeft;
        else if (item.IsFullyWithin(_topRight!._bounds))
            return _topRight;
        else if (item.IsFullyWithin(_bottomLeft!._bounds))
            return _bottomLeft;
        else if (item.IsFullyWithin(_bottomRight!._bounds))
            return _bottomRight;
        return null;
    }

}
