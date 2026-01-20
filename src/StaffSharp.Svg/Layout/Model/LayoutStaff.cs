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

    /// <summary>
    /// The Y offset within the bounds where the staff lines (Y=0) are positioned.
    /// When content extends above the staff origin, all content is shifted down and this
    /// tracks where the original Y=0 (top staff line) is within the bounds.
    /// </summary>
    public double StaffYOffset { get; set; }

    // Mapping back to notation structure for span processing
    public int PartIndex { get; set; }
    public int StaffNumber { get; set; }

    public bool Contains(INotationEvent notationEvent)
    {
        return Measures.Any(m => m.Contains(notationEvent));
    }

    public override void Offset(double dx, double dy)
    {
        base.Offset(dx, dy);

        // Offset all child measures and their contents
        foreach (var measure in Measures)
        {
            measure.Offset(dx, dy);
        }
    }
}