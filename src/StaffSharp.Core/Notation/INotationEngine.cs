using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Core.Notation;

/// <summary>
/// Converts performance timeline data (IR1) to notation score (IR2).
/// </summary>
public interface INotationEngine
{
    /// <summary>
    /// Converts a performance timeline to a notation score.
    /// </summary>
    /// <param name="timeline">The performance timeline to convert.</param>
    /// <param name="options">Options controlling the conversion behavior.</param>
    /// <returns>A notation score ready for rendering or export.</returns>
    NotationScore Convert(PerformanceTimeline timeline, NotationOptions? options = null);
}
