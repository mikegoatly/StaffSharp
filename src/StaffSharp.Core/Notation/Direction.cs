namespace StaffSharp.Notation;

/// <summary>
/// Represents a musical direction or expression marking at the measure level.
/// Examples: tempo markings, dynamics, rehearsal marks, text directions.
/// </summary>
/// <param name="Type">The type of direction.</param>
/// <param name="Placement">The vertical placement of the direction (above or below the staff).</param>
/// <param name="Content">The text content of the direction (e.g., "Allegro", "mf", "A", "D.C. al Coda").</param>
/// <param name="Bpm">Optional tempo in beats per minute (for tempo markings).</param
public record Direction(
    DirectionType Type,
    Placement Placement,
    string Content,
    int? Bpm = null);