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
        if (options.ClefPreference != ClefPreference.AutoGrandStaff)
        {
            return false;
        }

        var pitches = ExtractPitches(events);
        if (pitches.Count == 0)
        {
            return false;
        }

        var range = pitches.Max() - pitches.Min();
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
        var treble = new List<VoiceAssignment>();
        var bass = new List<VoiceAssignment>();

        foreach (var assignment in assignments)
        {
            if (assignment.Event.Pitch.Value >= splitPoint)
            {
                // High notes go to treble staff
                treble.Add(assignment);
            }
            else
            {
                // Low notes go to bass staff
                bass.Add(assignment);
            }
        }

        return (treble, bass);
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

    /// <summary>
    /// Extracts all MIDI pitch numbers from performance events.
    /// </summary>
    private static List<int> ExtractPitches(IEnumerable<IPerformanceEvent> events)
    {
        return events
            .Select(ev => ev.Pitch.MidiNumber)
            .ToList();
    }
}
