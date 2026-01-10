namespace StaffSharp.Layout.Model;

using StaffSharp.Notation;

/// <summary>
/// Represents a measure within a staff.
/// </summary>
internal class LayoutMeasure : LayoutElement
{
    public List<LayoutSymbol> Symbols { get; } = [];
    public List<LayoutCurve> Curves { get; } = [];

    /// <summary>
    /// The time signature for this measure, if it differs from the score's default.
    /// </summary>
    public TimeSignature? TimeSignature { get; set; }

    public bool Contains(INotationEvent notationEvent)
    {
        return Symbols.Any(s => 
            (s is NoteLayoutSymbol n && ReferenceEquals(n.Note, notationEvent))
            || (s is ChordLayoutSymbol c && ReferenceEquals(c.Chord, notationEvent)));
    }
}