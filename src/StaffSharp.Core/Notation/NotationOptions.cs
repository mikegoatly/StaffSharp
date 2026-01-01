using StaffSharp.Notation;

namespace StaffSharp.Core.Notation;

/// <summary>
/// Specifies how to determine which clef to use for notation.
/// </summary>
public enum ClefPreference
{
    /// <summary>
    /// Automatically detect the appropriate clef based on pitch range.
    /// Uses treble for high notes, bass for low notes.
    /// </summary>
    Auto,

    /// <summary>
    /// Force all parts to use treble clef regardless of pitch range.
    /// </summary>
    ForceTreble,

    /// <summary>
    /// Force all parts to use bass clef regardless of pitch range.
    /// </summary>
    ForceBass,

    /// <summary>
    /// Force all parts to use alto clef regardless of pitch range.
    /// </summary>
    ForceAlto,

    /// <summary>
    /// Force all parts to use tenor clef regardless of pitch range.
    /// </summary>
    ForceTenor
}

/// <summary>
/// Options for controlling how performance data (IR1) is converted to notation (IR2).
/// </summary>
public record NotationOptions
{
    /// <summary>
    /// Maximum number of dots allowed on a note (e.g., 2 for double-dotted notes).
    /// </summary>
    public int MaxDotsAllowed { get; init; } = 2;

    /// <summary>
    /// When a duration can be represented as either ties or dots, prefer ties.
    /// </summary>
    public bool PreferTiesOverDots { get; init; }

    /// <summary>
    /// Allow tuplets (triplets, quintuplets, etc.) when converting durations.
    /// </summary>
    public bool AllowTuplets { get; init; } = true;

    /// <summary>
    /// Default key signature to use when none is specified.
    /// </summary>
    public KeySignature DefaultKeySignature { get; init; } = KeySignature.C;

    /// <summary>
    /// Determines which clef to use for notation.
    /// Default is Auto, which detects based on pitch range.
    /// </summary>
    public ClefPreference ClefPreference { get; init; } = ClefPreference.Auto;

    /// <summary>
    /// Validates that all options have valid values.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when options are invalid.</exception>
    public void Validate()
    {
        if (MaxDotsAllowed < 0 || MaxDotsAllowed > 3)
        {
            throw new ArgumentException($"MaxDotsAllowed must be between 0 and 3, but was {MaxDotsAllowed}.", nameof(MaxDotsAllowed));
        }
    }
}
