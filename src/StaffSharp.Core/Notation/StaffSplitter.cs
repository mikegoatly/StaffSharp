using StaffSharp.Performance;

namespace StaffSharp.Core.Notation;

/// <summary>
/// Handles splitting performance data into multiple staves for grand staff notation.
/// </summary>
internal static class StaffSplitter
{
    /// <summary>
    /// Determines if grand staff is needed based on pitch range analysis.
    /// </summary>
    public static bool ShouldUseGrandStaff(
        IReadOnlyList<IPerformanceEvent> events,
        NotationOptions options)
    {
        // Grand staff is only considered for Auto preference
        // Force* preferences should never create grand staff
        if (options.ClefPreference != ClefPreference.Auto)
        {
            return false;
        }

        if (events.Count == 0) 
        {
            return false;
        }

        var (max, min) = events
            .Select(ev => ev.Pitch.MidiNumber)
            .Aggregate(
                (Max: int.MinValue, Min: int.MaxValue),
                (acc, pitch) => (Math.Max(acc.Max, pitch), Math.Min(acc.Min, pitch))
            );

        var range = max - min;
        return range > options.GrandStaffRangeThreshold;
    }

    /// <summary>
    /// Splits voice assignments into treble and bass groups based on pitch.
    /// </summary>
    public static (List<VoiceAssignment> Treble, List<VoiceAssignment> Bass)
        SplitVoiceAssignments(
            IReadOnlyList<VoiceAssignment> assignments,
            int splitPoint)
    {
        var groups = assignments
            .ToLookup(a => a.Event.Pitch.Value >= splitPoint);

        return (
            Treble: [.. groups[true]],
            Bass: [.. groups[false]]
        );
    }

    /// <summary>
    /// Renumbers voices within a group sequentially starting from 1.
    /// Creates a mapping from original voice numbers to new sequential numbers.
    /// </summary>
    public static List<VoiceAssignment> RenumberVoices(List<VoiceAssignment> assignments)
    {
        if (assignments.Count == 0)
        {
            return assignments;
        }

        // Find all unique voice numbers in original assignments
        var originalVoiceNumbers = assignments
            .Select(a => a.VoiceNumber)
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        // Create mapping: original voice number -> new voice number (1, 2, 3...)
        var voiceMapping = new Dictionary<int, int>();
        for (int i = 0; i < originalVoiceNumbers.Count; i++)
        {
            voiceMapping[originalVoiceNumbers[i]] = i + 1;
        }

        // Apply mapping to create new assignments
        var renumbered = new List<VoiceAssignment>(assignments.Count);
        foreach (var assignment in assignments)
        {
            var newVoiceNumber = voiceMapping[assignment.VoiceNumber];
            renumbered.Add(new VoiceAssignment(assignment.Event, newVoiceNumber));
        }

        return renumbered;
    }
}
