namespace StaffSharp.Layout.Model;

/// <summary>
/// Represents a horizontal system of music.
/// </summary>
public class LayoutSystem : LayoutElement
{
    public IReadOnlyList<LayoutStaff> Staves => _staves;
    private readonly List<LayoutStaff> _staves = new();

    internal void AddStaff(LayoutStaff staff) => _staves.Add(staff);
}