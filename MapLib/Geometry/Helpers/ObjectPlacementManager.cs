namespace MapLib.Geometry.Helpers;

/// <summary>
/// Manages object (usually label) placement to ensure objects
/// are placed optimally without overlap.
/// </summary>
/// <remarks>
/// NOTE: This has pretty bad complexity, and probably only works well for
/// a smaller number of bounds.
/// TODO: Make a more scalable implementation (some form of spatial index?).
/// </remarks>
public class ObjectPlacementManager
{
    private List<Bounds> AllBounds { get; } = new();

    /// <summary>
    /// Adds and returns the first of the possible bounds that doesn't overlap
    /// any current bounds. Returns null if not possible.
    /// </summary>
    /// <param name="possibleBounds">
    /// Possible Bounds, in priority order. The first one with no overlap is added.
    /// </param>
    /// <returns>
    /// The bounds of the successfully added object, or null
    /// if no given possible Bounds could be used.
    /// </returns>
    public Bounds? TryAdd(IEnumerable<Bounds> possibleBounds)
    {
        foreach (Bounds bounds in possibleBounds)
        {
            if (!OverlapsExistingBounds(bounds)) {
                AllBounds.Add(bounds);
                return bounds;
            }
        }
        return null;
    }

    /// <returns>
    /// True iff the given bounds overlap any existing bounds.
    /// </returns>
    private bool OverlapsExistingBounds(Bounds bounds)
        => AllBounds.Any(b => b.Intersects(bounds));

    public Bounds? GetOverlappingItem(Bounds bounds)
        => AllBounds.FirstOrDefault(b => b.Intersects(bounds));

    public int Count => AllBounds.Count;
}