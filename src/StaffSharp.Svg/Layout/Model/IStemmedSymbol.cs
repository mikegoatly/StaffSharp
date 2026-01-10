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
    /// Gets the effective top Y position accounting for stem, beam, and flags.
    /// This is the highest point of the symbol that articulations need to clear.
    /// </summary>
    double GetEffectiveTopY();

    /// <summary>
    /// Gets the effective bottom Y position accounting for stem, beam, and flags.
    /// This is the lowest point of the symbol that articulations need to clear.
    /// </summary>
    double GetEffectiveBottomY();

    /// <summary>
    /// The symbolic duration of the stemmed symbol.
    /// </summary>
    SymbolicDuration Duration { get; }
}
