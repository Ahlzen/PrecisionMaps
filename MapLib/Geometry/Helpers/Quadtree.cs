using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    /// <remarks>
    /// This seems to result in good performance for most workloads.
    /// See <see cref="MapLibTests.Geometry.LabelPlacementBenchmarks"/>
    /// </remarks>
    public const int DefaultMaxItemsPerNode = 256;

    private readonly int _maxItemsPerNode;
    private readonly Bounds _bounds;
    private readonly List<Bounds> _items = new();

    /// <summary>
    /// Child nodes. These are either all null or all non-null.  
    /// </summary>
    private QuadtreeNode? _topLeft, _topRight, _bottomLeft, _bottomRight;

    public QuadtreeNode(Bounds bounds, int maxItemsPerNode = DefaultMaxItemsPerNode)
    {
        _maxItemsPerNode = maxItemsPerNode;
        _bounds = bounds;
    }


    /// <returns>
    /// True if the item was added. False otherwise, for example
    /// if the item is entirely outside the bounds of this quadtree.
    /// </returns>
    public bool Add(Bounds item)
    {
        // Check if item is fully within the bounds of this quadtree
        if (!item.IsFullyWithin(_bounds))
            return false;

        //if (!_bounds.Intersects(item))
        //    return false; 

        // If we're at max capacity, we need to subdivide this quadtree
        if (_items.Count >= _maxItemsPerNode)
            Subdivide();

        // Add item to the appropriate quadrant, if it fits fully within.
        if (HasChildren)
        {
            QuadtreeNode? enclosingNode = GetFullyEnclosingChildNode(item);
            if (enclosingNode != null)
            {
                bool result = enclosingNode.Add(item);
                Debug.Assert(result);
                SanityCheck(this);
                return result;
            }
                
        }

        // This is a leaf node, or item doesn't fit whithin one
        // quadrant: Add directly to this note's items.
        _items.Add(item);
        SanityCheck(this);
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
        // If item is entirely outside these bounds,
        // it cannot overlap
        if (!item.Intersects(this._bounds))
            return null;

        // First check if item overlaps any of this node's own items
        foreach (Bounds ownItem in _items)
            if (item.Intersects(ownItem))
                return ownItem;

        // We only need to check chilren whose bounds intersect
        // with our item
        if (HasChildren)
        {
            return _topLeft!.GetOverlappingItem(item) ??
                _topRight!.GetOverlappingItem(item) ??
                _bottomLeft!.GetOverlappingItem(item) ??
                _bottomRight!.GetOverlappingItem(item);
        }

        // Items in child nodes should all be fully enclosed
        // by that node (or they would be on this node's list),
        // so we only need to check at most one child:
        //QuadtreeNode? child = GetFullyEnclosingChildNode(item);
        //if (child != null)
        //    return child.GetOverlappingItem(item);

        //// Check items of children
        //if (HasChildren)
        //{
        //    Bounds? overlapItem;
        //    overlapItem = _topLeft!.GetOverlappingItem(item);
        //    if (overlapItem != null) return overlapItem;
        //    overlapItem = _topRight!.GetOverlappingItem(item);
        //    if (overlapItem != null) return overlapItem;
        //    overlapItem = _bottomLeft!.GetOverlappingItem(item);
        //    if (overlapItem != null) return overlapItem;
        //    overlapItem = _bottomRight!.GetOverlappingItem(item);
        //    if (overlapItem != null) return overlapItem;
        //}

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
        SanityCheck(this);
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
        if (HasChildren) return; // already subdivided

        // Add child nodes
        _topLeft = new QuadtreeNode(new Bounds(_bounds.TopLeft, _bounds.Center), _maxItemsPerNode);
        _topRight = new QuadtreeNode(new Bounds(_bounds.TopRight, _bounds.Center), _maxItemsPerNode);
        _bottomLeft = new QuadtreeNode(new Bounds(_bounds.BottomLeft, _bounds.Center), _maxItemsPerNode);
        _bottomRight = new QuadtreeNode(new Bounds(_bounds.BottomRight, _bounds.Center), _maxItemsPerNode);

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

        SanityCheck(this);
    }

    // TEST CODE
    [Conditional("DEBUG")]
    private void SanityCheck(QuadtreeNode node)
    {
        // Verify none of own items can't fit in a single child node
        Debug.Assert(_items.All(item => GetFullyEnclosingChildNode(item) == null));

        // Verify each child items fit within that node
        if (HasChildren)
            foreach (var child in new QuadtreeNode[] {_topLeft!, _topRight!, _bottomLeft!, _bottomRight!})
                Debug.Assert(child._items.All(item => GetFullyEnclosingChildNode(item) == child));
    }
    
    // TEST CODE
    public int Count
    {
        get
        {
            int count = _items.Count;
            if (HasChildren)
            {
                count += _topLeft!.Count;
                count += _topRight!.Count;
                count += _bottomLeft!.Count;
                count += _bottomRight!.Count;
            }
            return count;
        }
    }

    // TEST CODE
    public QuadtreeNode? GetQuadtreeContaining(Bounds item)
    {
        if (_items.Contains(item))
            return this;
        if (HasChildren)
        {
            return
                _topLeft!.GetQuadtreeContaining(item) ??
                _topRight!.GetQuadtreeContaining(item) ??
                _bottomLeft!.GetQuadtreeContaining(item) ??
                _bottomRight!.GetQuadtreeContaining(item) ??
                null;
        }
        return null;
    }
    public bool Contains(Bounds item)
    {
        if (_items.Contains(item))
            return true;
        if (HasChildren)
        {
            if (_topLeft!.Contains(item)) return true;
            if (_topRight!.Contains(item)) return true;
            if (_bottomLeft!.Contains(item)) return true;
            if (_bottomRight!.Contains(item)) return true;
        }
        return false;
    }


    /// <remarks>If one child is non-null, all are non-null.</remarks>
    private bool HasChildren => _topLeft != null;

    /// <returns>
    /// Returns the child node (quadrant) that fully encloses the
    /// specified item, or null if this is a leaf or the item
    /// extends multiple quadrants.
    /// </returns>
    private QuadtreeNode? GetFullyEnclosingChildNode(Bounds item)
    {
        if (!HasChildren)
            return null;

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
