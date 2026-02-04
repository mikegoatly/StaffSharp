namespace StaffSharp.Core.Notation;

/// <summary>
/// Extension methods for working with collections of VoiceAssignment objects.
/// Provides common operations for grouping, segmentation, and temporal analysis.
/// </summary>
internal static class VoiceAssignmentExtensions
{
    /// <summary>
    /// Groups assignments by their onset time. Assignments must be pre-sorted by onset.
    /// </summary>
    /// <returns>Enumerable of (onset, notes) tuples for each unique onset time.</returns>
    public static IEnumerable<(Rational Onset, List<VoiceAssignment> Notes)> GroupByOnset(
        this List<VoiceAssignment> assignments)
    {
        var i = 0;
        var length = assignments.Count;

        while (i < length)
        {
            var onset = assignments[i].Event.OnsetBeats;

            // Collect all notes with identical onset
            int j = i + 1;
            while (j < length && assignments[j].Event.OnsetBeats == onset)
            {
                j++;
            }

            yield return (onset, assignments.GetRange(i, j - i));
            i = j;
        }
    }

    /// <summary>
    /// Gets all temporal boundaries where the set of active notes changes.
    /// For notes starting simultaneously with similar durations (true chords), uses minimum offset to avoid over-segmentation.
    /// For notes with significantly different durations, adds all individual offsets for proper polyphonic segmentation.
    /// Returns sorted list of beat positions including all note onsets and offsets.
    /// </summary>
    public static List<Rational> GetTemporalBoundaries(this List<VoiceAssignment> assignments)
    {
        var boundaries = new HashSet<Rational>();

        // Group notes by onset to detect simultaneous starts
        var onsetGroups = assignments.GroupByOnset().ToList();

        foreach (var (onset, notes) in onsetGroups)
        {
            boundaries.Add(onset);

            if (notes.Count > 1)
            {
                // Multiple notes starting together - check if durations are similar
                var minDuration = notes.Min(n => n.Event.DurationBeats);
                var maxDuration = notes.Max(n => n.Event.DurationBeats);

                const double durationSimilarityThreshold = 1.25; // 25% tolerance
                var durationRatio = maxDuration.ToDouble() / minDuration.ToDouble();

                if (durationRatio <= durationSimilarityThreshold)
                {
                    // Durations are similar (true chord) - use minimum offset to avoid over-segmentation
                    // from ML detection artifacts where chord notes have slightly different durations
                    var minOffset = notes.Min(n => n.Event.OffsetBeats);
                    boundaries.Add(minOffset);
                }
                else
                {
                    // Durations differ significantly (polyphonic) - add all individual offsets
                    // to properly segment sustained notes vs shorter melody (e.g., left hand + right hand)
                    foreach (var note in notes)
                    {
                        boundaries.Add(note.Event.OffsetBeats);
                    }
                }
            }
            else
            {
                // Single note - use its actual offset
                boundaries.Add(notes[0].Event.OffsetBeats);
            }
        }

        return boundaries.OrderBy(b => b).ToList();
    }

    /// <summary>
    /// Gets all notes that are active (sounding) at a specific beat position,
    /// excluding "straggler" notes that are part of chords that logically ended.
    /// A note is active if its onset &lt;= beat &lt; offset, and it's not a straggler.
    /// </summary>
    public static List<VoiceAssignment> GetActiveNotesAt(
        this List<VoiceAssignment> assignments,
        Rational beat)
    {
        // Build map of onset -> min offset for TRUE chord groups (similar durations only)
        // Notes with significantly different durations are polyphonic, not chords with stragglers
        const double durationSimilarityThreshold = 1.25; // 25% tolerance
        var chordMinOffsets = assignments
            .GroupByOnset()
            .Where(g =>
            {
                if (g.Notes.Count <= 1) return false;

                // Check if durations are similar (true chord)
                var minDuration = g.Notes.Min(n => n.Event.DurationBeats);
                var maxDuration = g.Notes.Max(n => n.Event.DurationBeats);
                var durationRatio = maxDuration.ToDouble() / minDuration.ToDouble();

                return durationRatio <= durationSimilarityThreshold;
            })
            .ToDictionary(g => g.Onset, g => g.Notes.Min(n => n.Event.OffsetBeats));

        return assignments
            .Where(a =>
            {
                // Note must be active at this beat
                if (a.Event.OnsetBeats > beat || a.Event.OffsetBeats <= beat)
                    return false;

                // If note is part of a chord that logically ended, exclude it
                if (chordMinOffsets.TryGetValue(a.Event.OnsetBeats, out var minOffset))
                {
                    // This note is part of a chord - only include if beat is before minOffset
                    return beat < minOffset;
                }

                // Single note or chord hasn't ended yet
                return true;
            })
            .ToList();
    }

    /// <summary>
    /// Filters out notes with zero duration or invalid pitch.
    /// </summary>
    public static IEnumerable<VoiceAssignment> FilterValid(this IEnumerable<VoiceAssignment> assignments)
    {
        return assignments
            .Where(a => a.Event.DurationBeats != Rational.Zero && a.Event.Pitch.MidiNumber >= 0);
    }

    /// <summary>
    /// Sorts assignments by onset time.
    /// </summary>
    public static List<VoiceAssignment> SortByOnset(this IEnumerable<VoiceAssignment> assignments)
    {
        return assignments.OrderBy(a => a.Event.OnsetBeats).ToList();
    }
}
