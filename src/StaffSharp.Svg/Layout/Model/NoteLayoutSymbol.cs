namespace StaffSharp.Layout.Model;

using StaffSharp.Notation;

/// <summary>
/// Represents a positioned note.
/// </summary>
internal sealed class NoteLayoutSymbol : StemmedSymbol
{
    public required NotationNote Note { get; init; }

    public Accidental? Accidental { get; set; }
    public double AccidentalX { get; set; }
    public double AccidentalY { get; set; }

    public override SymbolicDuration Duration => Note.Duration;

    /// <summary>
    /// Gets the effective top Y position accounting for stem, beam, and flags.
    /// </summary>
    public override double GetEffectiveTopY()
    {
        // If stem goes up, top is at the notehead
        // If stem goes down, top is at the stem end (which extends above)
        return Stem.Up ? Bounds.Y : Stem.Y2;
    }

    /// <summary>
    /// Gets the effective bottom Y position accounting for stem, beam, and flags.
    /// </summary>
    public override double GetEffectiveBottomY()
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
    }
}
