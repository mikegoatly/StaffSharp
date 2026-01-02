namespace StaffSharp.Svg.Layout;

using StaffSharp.Notation;

/// <summary>
/// Represents a staff within a system.
/// </summary>
public class LayoutStaff : LayoutElement
{
    public IReadOnlyList<LayoutMeasure> Measures => _measures;
    private readonly List<LayoutMeasure> _measures = [];

    /// <summary>
    /// The current clef for this staff. Used by layout passes to calculate pitch positions.
    /// </summary>
    public Clef CurrentClef { get; set; } = Clef.Treble;

    /// <summary>
    /// The current key signature for this staff. Used to determine which accidentals to display.
    /// </summary>
    public KeySignature CurrentKeySignature { get; set; } = KeySignature.C;

    internal void AddMeasure(LayoutMeasure measure) => _measures.Add(measure);
}