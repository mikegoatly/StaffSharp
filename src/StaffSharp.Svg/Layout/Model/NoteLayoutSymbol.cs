namespace StaffSharp.Svg.Layout;

using StaffSharp.Layout.Model;
using StaffSharp.Notation;

/// <summary>
/// Represents a positioned note.
/// </summary>
public sealed class NoteLayoutSymbol : AugmentationDottedLayoutSymbol, IStemmedSymbol
{
    public required NotationNote Note { get; init; }

    public Accidental? Accidental { get; set; }
    public double AccidentalX { get; set; }
    public double AccidentalY { get; set; }

    // Stem and beam information (IStemmedSymbol implementation)
    public StemInfo Stem { get; set; }
    public BeamInfo Beam { get; set; }

    // Positioned decorations/articulations
    public IList<(Decoration Type, double X, double Y)> PositionedDecorations { get; } = [];

    /// <summary>
    /// Gets the effective top Y position accounting for stem, beam, and flags.
    /// </summary>
    public double GetEffectiveTopY()
    {
        // If stem goes up, top is at the notehead
        // If stem goes down, top is at the stem end (which extends above)
        return Stem.Up ? Y : Stem.Y2;
    }

    /// <summary>
    /// Gets the effective bottom Y position accounting for stem, beam, and flags.
    /// </summary>
    public double GetEffectiveBottomY()
    {
        // If stem goes up, bottom is at the stem end (which extends below)
        // If stem goes down, bottom is at the notehead
        return Stem.Up ? Stem.Y2 : Y;
    }
}
