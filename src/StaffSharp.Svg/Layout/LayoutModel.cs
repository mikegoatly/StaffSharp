namespace StaffSharp.Layout;

using StaffSharp.Layout.Model;
using StaffSharp.Notation;

/// <summary>
/// The root of the layout model.
/// </summary>
public class LayoutModel
{
    public IReadOnlyList<LayoutSystem> Systems => _systems;
    private readonly List<LayoutSystem> _systems = new();

    /// <summary>
    /// Gets the total width of all content, calculated from system bounds.
    /// </summary>
    public double TotalWidth => Systems.Count > 0 
        ? Systems.Max(s => s.X + s.Width) 
        : 0;

    /// <summary>
    /// Gets the total height of all content, calculated from system bounds.
    /// </summary>
    public double TotalHeight => Systems.Count > 0
        ? Systems.Max(s => s.Y + s.Height)
        : 0;

    /// <summary>
    /// Score metadata needed for system symbol insertion (time signature, etc.)
    /// </summary>
    public ScoreMetadata? Metadata { get; set; }

    internal void AddSystem(LayoutSystem system) => _systems.Add(system);

    internal void ClearSystems() => _systems.Clear();

    internal void ReplaceSystems(IEnumerable<LayoutSystem> newSystems)
    {
        _systems.Clear();
        _systems.AddRange(newSystems);
    }
}