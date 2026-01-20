using StaffSharp.Notation;

namespace StaffSharp.Layout.Model;

/// <summary>
/// Interface for layout symbols that have stems and may be part of beam groups.
/// Provides information needed for proper articulation placement and other layout decisions.
/// </summary>
internal interface IStemmedSymbol : ILayoutSymbol
{
    /// <summary>
    /// Stem information (position and direction).
    /// </summary>
    StemInfo Stem { get; set; }

    /// <summary>
    /// Beam and flag information.
    /// </summary>
    BeamInfo Beam { get; set; }

    /// <summary>
    /// Gets or sets the bounding rectangle of the note head.
    /// </summary>
    Bounds NoteHeadBounds { get; set; }

    /// <summary>
    /// The symbolic duration of the stemmed symbol.
    /// </summary>
    SymbolicDuration Duration { get; }

    /// <summary>
    /// Gets the effective top Y position accounting for stem, beam, and flags.
    /// </summary>
    double GetEffectiveTopY();

    /// <summary>
    /// Gets the effective bottom Y position accounting for stem, beam, and flags.
    /// </summary>
    double GetEffectiveBottomY();
}
