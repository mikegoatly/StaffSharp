namespace StaffSharp.Notation;

/// <summary>
/// Represents a slur marker on a note or chord.
/// Slurs indicate notes should be played smoothly connected (legato).
/// ABC notation: (ABC) or (A(B)C) for nested slurs.
/// </summary>
/// <param name="Number">Slur number for distinguishing overlapping slurs (e.g., in nested slurs).</param>
/// <param name="Type">Whether this marks the start or stop of a slur.</param>
/// <param name="IsDotted">True if this is a dotted slur (indicates slight separation while maintaining phrasing).</param>
public readonly record struct SlurMarker(int Number, SlurMarkerType Type, bool IsDotted = false);

/// <summary>
/// Type of slur marker.
/// </summary>
public enum SlurMarkerType
{
    /// <summary>This note starts a slur.</summary>
    Start,

    /// <summary>This note ends a slur.</summary>
    Stop
}
