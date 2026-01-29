using StaffSharp.Notation;

namespace StaffSharp.Core.Notation;

/// <summary>
/// Specifies how to determine which clef to use for notation.
/// </summary>
public enum ClefPreference
{
    /// <summary>
    /// Automatically detect the appropriate clef(s) based on pitch range.
    /// For narrow ranges: uses treble (avg pitch >= Middle C) or bass (avg pitch < Middle C).
    /// For wide ranges (> 24 semitones): creates grand staff with treble + bass staves.
    /// This is the recommended default for most use cases.
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
    /// Pitch range threshold (in semitones) for automatically creating grand staff.
    /// When ClefPreference is AutoGrandStaff and the pitch range exceeds this threshold,
    /// a grand staff (treble + bass) will be created instead of a single staff.
    /// Default is 24 semitones (2 octaves).
    /// </summary>
    public int GrandStaffRangeThreshold { get; init; } = 24;

    /// <summary>
    /// MIDI note number to use as split point between treble and bass staves.
    /// Notes with pitch >= this value go to the treble staff, notes below go to the bass staff.
    /// Default is 60 (Middle C / C4).
    /// </summary>
    public int GrandStaffSplitPoint { get; init; } = 60;

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

        if (GrandStaffRangeThreshold < 0 || GrandStaffRangeThreshold > 127)
        {
            throw new ArgumentException($"GrandStaffRangeThreshold must be between 0 and 127 semitones, but was {GrandStaffRangeThreshold}.", nameof(GrandStaffRangeThreshold));
        }

        if (GrandStaffSplitPoint < 0 || GrandStaffSplitPoint > 127)
        {
            throw new ArgumentException($"GrandStaffSplitPoint must be a valid MIDI note number (0-127), but was {GrandStaffSplitPoint}.", nameof(GrandStaffSplitPoint));
        }
    }
}
