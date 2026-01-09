namespace StaffSharp.Layout.Model;

/// <summary>
/// Represents a horizontal system of music.
/// </summary>
internal class LayoutSystem : LayoutElement
{
    public List<LayoutStaff> Staves { get; } = [];

    public LayoutSystem()
    {
    }

    internal LayoutSystem(List<LayoutStaff> staves)
    {
        Staves = staves;
    }

    internal void AddStaff(LayoutStaff staff) => Staves.Add(staff);
}