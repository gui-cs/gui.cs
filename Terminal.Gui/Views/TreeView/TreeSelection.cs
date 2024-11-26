#nullable enable
namespace Terminal.Gui;

internal class TreeSelection<T> where T : class
{
    /// <summary>Creates a new selection between two branches in the tree</summary>
    /// <param name="from"></param>
    /// <param name="toIndex"></param>
    /// <param name="map"></param>
    public TreeSelection (Branch<T>? from, int toIndex, IReadOnlyCollection<Branch<T>>? map)
    {
        Origin = from;

        if (Origin?.Model is null)
        {
            return;
        }

        if (map is null)
        {
            return;
        }

        _included.Add (Origin.Model);

        int oldIdx = map.IndexOf (from);

        int lowIndex = Math.Min (oldIdx, toIndex);
        int highIndex = Math.Max (oldIdx, toIndex);

        // Select everything between the old and new indexes

        foreach (Branch<T> alsoInclude in map.Skip (lowIndex).Take (highIndex - lowIndex))
        {
            if (alsoInclude.Model is { })
            {
                _included.Add (alsoInclude.Model);
            }
        }
    }

    private readonly HashSet<T?> _included = new ();
    public bool Contains (T? model) { return _included.Contains (model); }

    public Branch<T>? Origin { get; }
}
