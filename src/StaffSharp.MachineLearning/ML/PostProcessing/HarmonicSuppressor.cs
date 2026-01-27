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

        var temporalWindow = _options.TemporalWindow;

        for (int i = 0; i < sortedEvents.Count; i++)
        {
            if (!keepNote[i])
            {
                continue;
            }

            var note1 = sortedEvents[i];

            // Check both forward (harmonics after fundamental) and backward (harmonics before fundamental)
            for (int j = 0; j < sortedEvents.Count; j++)
            {
                if (i == j || !keepNote[j])
                {
                    continue;
                }

                var note2 = sortedEvents[j];

                // Check if notes are within temporal window (either direction)
                var timeDiff = (note2.Onset - note1.Onset).Duration();
                if (timeDiff > temporalWindow)
                {
                    continue;
                }

                // Determine which is fundamental (lower pitch) and which is harmonic (higher pitch)
                NoteEvent fundamental;
                NoteEvent harmonic;
                int harmonicIndex;

                var note1Pitch = note1.Pitch.MidiNumber;
                var note2Pitch = note2.Pitch.MidiNumber;
                if (note1Pitch < note2Pitch)
                {
                    fundamental = note1;
                    harmonic = note2;
                    harmonicIndex = j;
                }
                else if (note2Pitch < note1Pitch)
                {
                    fundamental = note2;
                    harmonic = note1;
                    harmonicIndex = i;
                }
                else
                {
                    continue; // Same pitch, not a harmonic relationship
                }

                int interval = harmonic.Pitch.MidiNumber - fundamental.Pitch.MidiNumber;

                // 12 (Octave), 19 (Perfect 12th), 24 (2 Octaves)
                if (interval == 12 || interval == 19 || interval == 24)
                {
                    // Check Physics Constraints
                    // Constraint A: Velocity
                    bool isQuietEnough = harmonic.Velocity.Value < (fundamental.Velocity.Value * _options.VelocityRatio);

                    // Constraint B: Duration
                    bool stopsWithFundamental = harmonic.Offset <= fundamental.Offset + TimeSpan.FromMilliseconds(100);

                    if (isQuietEnough && stopsWithFundamental)
                    {
                        keepNote[harmonicIndex] = false;
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
