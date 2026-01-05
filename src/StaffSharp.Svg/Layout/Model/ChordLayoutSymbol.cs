namespace StaffSharp.Layout.Model;

using StaffSharp.Layout;
using StaffSharp.Notation;

/// <summary>
/// Represents a positioned chord.
/// </summary>
public sealed class ChordLayoutSymbol : AugmentationDottedLayoutSymbol, IStemmedSymbol
{
    public required Chord Chord { get; init; }
    public IList<double> NoteheadYPositions { get; } = [];
    public IList<double> NoteheadXShifts { get; } = [];
    public IList<bool> AccidentalShifts { get; } = [];
    public IList<Accidental> Accidentals { get; } = [];
    public IList<double> AccidentalXOffsets { get; } = [];
    public IList<double> AccidentalYPositions { get; } = [];

    // Stem and beam information (IStemmedSymbol implementation)
    public StemInfo Stem { get; set; }
    public BeamInfo Beam { get; set; }

    // Positioned decorations/articulations
    public IList<(Decoration Type, double X, double Y)> PositionedDecorations { get; } = [];

    /// <summary>
    /// Gets the effective top Y position accounting for stem, beam, and flags.
    /// For chords, this is either the topmost notehead (stem up) or stem endpoint (stem down).
    /// </summary>
    public double GetEffectiveTopY()
    {
        if (Stem.Up)
        {
            // Stem up: top is at the topmost notehead
            return NoteheadYPositions.Count > 0 ? NoteheadYPositions.Min() : Y;
        }
        else
        {
            // Stem down: top is at the stem end (which extends above the chord)
            return Stem.Y2;
        }
    }

    /// <summary>
    /// Gets the effective bottom Y position accounting for stem, beam, and flags.
    /// For chords, this is either the bottommost notehead (stem down) or stem endpoint (stem up).
    /// </summary>
    public double GetEffectiveBottomY()
    {
        if (Stem.Up)
        {
            // Stem up: bottom is at the stem end (which extends below the chord)
            return Stem.Y2;
        }
        else
        {
            // Stem down: bottom is at the bottommost notehead
            return NoteheadYPositions.Count > 0 ? NoteheadYPositions.Max() : Y;
        }
    }
}
