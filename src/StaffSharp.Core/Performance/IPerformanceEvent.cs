namespace StaffSharp.Performance;

/// <summary>
/// Base interface for all events in a performance timeline.
/// All events have a musical time onset (in beats from start of piece).
/// </summary>
public interface IPerformanceEvent
{
    /// <summary>
    /// Musical time onset from the start of the piece, measured in beats.
    /// Uses Rational arithmetic for exact representation (no floating-point errors).
    /// </summary>
    Rational OnsetBeats { get; }
}
