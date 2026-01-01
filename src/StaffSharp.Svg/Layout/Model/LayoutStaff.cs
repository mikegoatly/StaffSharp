namespace StaffSharp.Svg.Layout;

/// <summary>
/// Represents a staff within a system.
/// </summary>
public class LayoutStaff : LayoutElement
{
    public IReadOnlyList<LayoutMeasure> Measures => _measures;
    private readonly List<LayoutMeasure> _measures = new();

    internal void AddMeasure(LayoutMeasure measure) => _measures.Add(measure);
}