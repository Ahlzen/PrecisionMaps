﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    /// Based on benchmark results, somewhere in the range 16-32 items per
    /// node seems to result in good performance for most workloads.
    /// See <see cref="MapLibTests.Geometry.LabelPlacementBenchmarks"/>
    /// </remarks>
    public const int DefaultMaxItemsPerNode = 32;

    private readonly int _maxItemsPerNode;
    private readonly Bounds _bounds;
    private readonly List<Bounds> _items = new();

    /// <summary>
    /// Child nodes. Order: [TopLeft, TopRight, BottomLeft, BottomRight]
    /// </summary>
    private QuadtreeNode[]? _children = null;

    public QuadtreeNode(Bounds bounds, int maxItemsPerNode = DefaultMaxItemsPerNode)
    {
        _maxItemsPerNode = maxItemsPerNode;
        _bounds = bounds;
    }


    /// <returns>
    /// True if the item was added. False otherwise, for example
    /// if the item is entirely outside the bounds of this quadtree.
    /// </returns>
    public bool Add(Bounds bounds)
    {
        // Check if item is fully within the bounds of this quadtree
        if (!bounds.IsFullyWithin(_bounds))
            return false;

        if (_items.Count >= _maxItemsPerNode)
            Subdivide();

        // Add item to the appropriate quadrant, if it fits fully within.
        if (HasChildren)
        {
            QuadtreeNode? enclosingNode = GetFullyEnclosingChildNode(bounds);
            if (enclosingNode != null)
                return enclosingNode.Add(bounds);
        }

        // This is a leaf node, or item doesn't fit whithin one
        // quadrant: Add directly to this note's items.
        _items.Add(bounds);
        return true;
    }

    /// <returns>
    /// True iff the item overlaps any existing item in this quadtree.
    /// </returns>
    public bool Overlaps(Bounds item)
        => GetOverlappingItem(item) != null;

    /// <returns>
    /// The (first) item in this node that overlaps the
    /// specified item, or null if none.
    /// </returns>
    public Bounds? GetOverlappingItem(Bounds item)
    {
        // If item is entirely outside our bounds it cannot overlap
        if (!item.Intersects(_bounds))
            return null;

        // First check if item overlaps any of this node's own items
        foreach (Bounds ownItem in _items)
            if (item.Intersects(ownItem))
                return ownItem;

        // Then check children (if applicable)
        if (HasChildren)
        {
            foreach (QuadtreeNode node in _children)
            {
                Bounds? overlap = node!.GetOverlappingItem(item);
                if (overlap != null)
                    return overlap;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds the specified item to the quadtree iff
    /// the item falls within the quadtree's bounds AND
    /// it does not overlap any other item.
    /// </summary>
    /// <returns>True if the item was added, false otherwise.</returns>
    public bool AddIfNotOverlapping(Bounds item)
        => AddOrGetOverlap(item) == null;

    /// <summary>
    /// Adds the specified item and returns null, or returns
    /// the first item from the quadtree that overlaps the
    /// specified item.
    /// </summary>
    public Bounds? AddOrGetOverlap(Bounds item)
    {
        Bounds? overlappingItem = GetOverlappingItem(item);
        if (overlappingItem != null)
            return overlappingItem;
        Add(item);
        return null;
    }


    /// <summary>Removes the item from this quadtree, if exists.</summary>
    /// <returns>True iff an item was removed.</returns>
    public bool Remove(Bounds item)
    {
        throw new NotImplementedException("TODO: implement if needed");
    }

    /// <summary>
    /// Subdivides this node into four quadrants (child nodes).
    /// </summary>
    private void Subdivide()
    {
        if (HasChildren) return; // already subdivided

        // Create child nodes
        _children = [
            new QuadtreeNode(new Bounds(_bounds.TopLeft, _bounds.Center), _maxItemsPerNode),
            new QuadtreeNode(new Bounds(_bounds.TopRight, _bounds.Center), _maxItemsPerNode),
            new QuadtreeNode(new Bounds(_bounds.BottomLeft, _bounds.Center), _maxItemsPerNode),
            new QuadtreeNode(new Bounds(_bounds.BottomRight, _bounds.Center), _maxItemsPerNode)];

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

    /// <summary>
    /// Returns the number of items under this node in the quadtree.
    /// </summary>
    /// <remarks>
    /// NOTE: This requires fully traversing the tree.
    /// </remarks>
    public int Count
    {
        get
        {
            int count = _items.Count;
            if (HasChildren)
                foreach (QuadtreeNode child in _children)
                    count += child.Count;
            return count;
        }
    }

    [MemberNotNullWhen(true, nameof(_children))]
    private bool HasChildren => _children != null;

    /// <returns>
    /// Returns the child node (quadrant) that fully encloses the
    /// specified item, or null if this is a leaf or the item
    /// extends multiple quadrants.
    /// </returns>
    private QuadtreeNode? GetFullyEnclosingChildNode(Bounds item)
    {
        if (HasChildren)
            foreach (QuadtreeNode child in _children)
                if (item.IsFullyWithin(child._bounds))
                    return child;
        return null;
    }

}
