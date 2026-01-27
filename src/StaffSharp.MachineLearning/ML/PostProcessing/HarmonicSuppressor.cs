namespace StaffSharp.MachineLearning.ML.PostProcessing;

using StaffSharp.MachineLearning.Options;

/// <summary>
/// Filters out harmonic overtones that were incorrectly detected as separate notes.
/// Common in monophonic instrument recordings where the ML model detects both
/// the fundamental frequency and its harmonics as distinct notes.
/// </summary>
public sealed class HarmonicSuppressor
{
    private readonly HarmonicSuppressionOptions _options;

    /// <summary>
    /// Creates a new harmonic suppressor with the specified options.
    /// </summary>
    /// <param name="options">Harmonic suppression configuration options. If null, uses defaults.</param>
    public HarmonicSuppressor(HarmonicSuppressionOptions? options = null)
    {
        _options = options ?? new HarmonicSuppressionOptions();
    }

    /// <summary>
    /// Filters out harmonic overtones from the note events.
    /// When multiple notes start at approximately the same time and have harmonic relationships
    /// keeps only the lowest pitch (fundamental).
    /// </summary>
    /// <param name="noteEvents">The input note events (assumed to be sorted by onset time).</param>
    /// <returns>Filtered note events with harmonics removed.</returns>
    public IReadOnlyList<NoteEvent> SuppressHarmonics(IReadOnlyList<NoteEvent> noteEvents)
    {
        ArgumentNullException.ThrowIfNull(noteEvents);

        if (!_options.SuppressHarmonics || noteEvents.Count <= 1)
        {
            return noteEvents;
        }

        // Ensure sorting, as the logic depends on looking "ahead" in time
        var sortedEvents = noteEvents.OrderBy(n => n.Onset).ToList();
        var keepNote = new bool[sortedEvents.Count];
        Array.Fill(keepNote, true);

        for (int i = 0; i < sortedEvents.Count; i++)
        {
            if (!keepNote[i])
            {
                continue;
            }

            var fundamental = sortedEvents[i];

            for (int j = i + 1; j < sortedEvents.Count; j++)
            {
                var potentialHarmonic = sortedEvents[j];

                // Stop checking if we leave the time window
                var temporalWindow = TimeSpan.FromMilliseconds(_options.TemporalWindowMs);
                if (potentialHarmonic.Onset - fundamental.Onset > temporalWindow)
                {
                    break;
                }

                if (!keepNote[j])
                {
                    continue;
                }

                // 1. Check Pitch Relationship
                // We only care if the potential harmonic is HIGHER than the fundamental
                if (potentialHarmonic.Pitch.MidiNumber <= fundamental.Pitch.MidiNumber)
                {
                    continue;
                }

                int interval = potentialHarmonic.Pitch.MidiNumber - fundamental.Pitch.MidiNumber;

                // 12 (Octave), 19 (Perfect 12th), 24 (2 Octaves)
                // We don't do perfect 5th because played 5ths are too common in chords to risk suppressing
                if (interval == 12 || interval == 19 || interval == 24)
                {
                    // Check Physics Constraints
                    // Constraint A: Velocity
                    // A harmonic artifact is usually weaker than the real note.
                    // If the "harmonic" is LOUDER or similar volume, it was likely played.
                    bool isQuietEnough = potentialHarmonic.Velocity.Value < (fundamental.Velocity.Value * _options.VelocityRatio);

                    // Constraint B: Duration
                    // A harmonic cannot sustain longer than its parent fundamental.
                    // We allow a small error margin (e.g. 100ms) for release detection jitter.
                    bool stopsWithFundamental = potentialHarmonic.Offset <= fundamental.Offset + TimeSpan.FromMilliseconds(100);

                    if (isQuietEnough && stopsWithFundamental)
                    {
                        keepNote[j] = false;
                    }
                }
            }
        }

        if (keepNote.All(k => k))
        {
            return sortedEvents;
        }

        return [.. sortedEvents.Where((_, index) => keepNote[index])];
    }
}
