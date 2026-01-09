namespace StaffSharp.Notation;

/// <summary>
/// Represents a tie marker on a note or chord.
/// Ties connect notes of the same pitch, indicating they should be performed as a single sustained note.
/// </summary>
/// <param name="Type">Whether this marks the start or stop of a tie.</param>
public readonly record struct TieMarker(TieMarkerType Type);

/// <summary>
/// Type of tie marker.
/// </summary>
public enum TieMarkerType
{
    /// <summary>This note starts a tie to the next note of the same pitch.</summary>
    Start,

    /// <summary>This note ends a tie from the previous note of the same pitch.</summary>
    Stop,

    /// <summary>This note is both the end of a tie from the previous note AND the start of a tie to the next note (middle of a tie chain).</summary>
    Both
}
