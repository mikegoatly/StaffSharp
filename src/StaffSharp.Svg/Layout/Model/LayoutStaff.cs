namespace StaffSharp.Layout.Model;

using StaffSharp.Notation;

/// <summary>
/// Represents a staff within a system.
/// </summary>
internal class LayoutStaff : LayoutElement
{
    public List<LayoutMeasure> Measures { get; } = [];

    /// <summary>
    /// The current clef for this staff. Used by layout passes to calculate pitch positions.
    /// </summary>
    public Clef CurrentClef { get; set; } = Clef.Treble;

    /// <summary>
    /// The current key signature for this staff. Used to determine which accidentals to display.
    /// </summary>
    public KeySignature CurrentKeySignature { get; set; } = KeySignature.C;

    // Mapping back to notation structure for span processing
    public int PartIndex { get; set; }
    public int StaffNumber { get; set; }

    public bool Contains(INotationEvent notationEvent)
    {
        return Measures.Any(m => m.Contains(notationEvent));
    }
}