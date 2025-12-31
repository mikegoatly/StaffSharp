using StaffSharp.Performance;

namespace StaffSharp.Core.Notation;

/// <summary>
/// Represents the assignment of a performance event to a specific voice number.
/// </summary>
public sealed record VoiceAssignment(IPerformanceEvent Event, int VoiceNumber);

/// <summary>
/// Assigns performance events to voices for polyphonic notation.
/// </summary>
public interface IVoiceAssigner
{
    /// <summary>
    /// Assigns each performance event to a voice number (1-based).
    /// </summary>
    /// <param name="events">The performance events to assign, sorted by onset time.</param>
    /// <returns>Voice assignments for each event.</returns>
    IReadOnlyList<VoiceAssignment> AssignVoices(IReadOnlyList<IPerformanceEvent> events);
}
