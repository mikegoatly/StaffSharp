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

    /// <summary>
    /// Determines whether the collection contains a symbol that references the specified notation event.
    /// </summary>
    /// <param name="notationEvent">The notation event to locate in the collection. This can be a note or chord event. Cannot be null.</param>
    /// <returns>true if a symbol referencing the specified notation event is found in the collection; otherwise, false.</returns>
    public bool Contains(INotationEvent notationEvent)
    {
        return Symbols.Any(s =>
            (s is NoteLayoutSymbol n && ReferenceEquals(n.Note, notationEvent))
            || (s is ChordLayoutSymbol c && ReferenceEquals(c.Chord, notationEvent)));
    }

    public override void Offset(double dx, double dy)
    {
        base.Offset(dx, dy);

        // Offset all symbols
        foreach (var symbol in Symbols)
        {
            symbol.Offset(dx, dy);
        }

        // Offset all curves
        foreach (var curve in Curves)
        {
            curve.Offset(dx, dy);
        }
    }
}