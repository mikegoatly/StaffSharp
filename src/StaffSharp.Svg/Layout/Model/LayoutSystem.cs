namespace StaffSharp.Layout.Model;

/// <summary>
/// Represents a horizontal system of music.
/// </summary>
public class LayoutSystem : LayoutElement
{
    private readonly List<LayoutStaff> _staves = [];

    public LayoutSystem()
    {
    }

    internal LayoutSystem(List<LayoutStaff> staves)
    {
        _staves = staves;
    }

    public IReadOnlyList<LayoutStaff> Staves => _staves;

    internal void AddStaff(LayoutStaff staff) => _staves.Add(staff);
}