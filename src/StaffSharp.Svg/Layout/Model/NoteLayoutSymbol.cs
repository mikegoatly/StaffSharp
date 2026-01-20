namespace StaffSharp.Layout.Model;

using StaffSharp.Layout;
using StaffSharp.Notation;

/// <summary>
/// Represents a positioned note.
/// </summary>
internal sealed class NoteLayoutSymbol : AugmentationDottedLayoutSymbol, IStemmedSymbol
{
    public required NotationNote Note { get; init; }

    public Accidental? Accidental { get; set; }
    public double AccidentalX { get; set; }
    public double AccidentalY { get; set; }

    // Stem and beam information (IStemmedSymbol implementation)
    public StemInfo Stem { get; set; }
    public BeamInfo Beam { get; set; }
    public Bounds NoteHeadBounds { get; set; }

    public SymbolicDuration Duration => Note.Duration;

    // Positioned decorations/articulations
    public List<(Decoration Type, double X, double Y)> PositionedDecorations { get; } = [];

    /// <summary>
    /// Gets the effective top Y position accounting for stem, beam, and flags.
    /// </summary>
    public double GetEffectiveTopY()
    {
        // If stem goes up, top is at the notehead
        // If stem goes down, top is at the stem end (which extends above)
        return Stem.Up ? Bounds.Y : Stem.Y2;
    }

    /// <summary>
    /// Gets the effective bottom Y position accounting for stem, beam, and flags.
    /// </summary>
    public double GetEffectiveBottomY()
    {
        // If stem goes up, bottom is at the stem end (which extends below)
        // If stem goes down, bottom is at the notehead
        return Stem.Up ? Stem.Y2 : Bounds.Y;
    }

    public override void Offset(double dx, double dy)
    {
        base.Offset(dx, dy);

        // Offset accidental position
        AccidentalY += dy;

        // Offset stem positions
        Stem = Stem with { Y1 = Stem.Y1 + dy, Y2 = Stem.Y2 + dy };

        // Offset note head bounds
        NoteHeadBounds = NoteHeadBounds.Offset(dx, dy);

        // Offset positioned decorations
        for (int i = 0; i < PositionedDecorations.Count; i++)
        {
            var (type, x, y) = PositionedDecorations[i];
            PositionedDecorations[i] = (type, x + dx, y + dy);
        }
    }
}
