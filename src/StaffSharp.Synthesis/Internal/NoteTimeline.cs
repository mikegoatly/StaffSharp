using StaffSharp.Notation;

namespace StaffSharp.Synthesis.Internal;

/// <summary>
/// Builds and manages a timeline of note events from a score.
/// </summary>
internal sealed class NoteTimeline
{
    private readonly List<SynthNote> _events = [];

    public IReadOnlyList<SynthNote> Events => _events;

    /// <summary>
    /// Builds a timeline from a score by extracting all notes from all parts, staves, voices, and measures.
    /// </summary>
    public static NoteTimeline FromScore(NotationScore score)
    {
        var timeline = new NoteTimeline();

        // Calculate seconds per beat based on tempo
        double beatsPerMinute = score.Metadata.Tempo;
        double secondsPerBeat = 60.0 / beatsPerMinute;

        // Process all parts (instruments)
        // staves in the part (e.g., grand staff for piano has 2 staves)
        // voices in the staff
        foreach (var voice in score.Parts.SelectMany(p => p.Staves.SelectMany(s => s.Voices)))
        {
            double currentTime = 0.0;

            // Process all events in all measures in the voice
            foreach (var evt in voice.Measures.SelectMany(m => m.Events))
            {
                if (evt is NotationNote note)
                {
                    double durationSeconds = CalculateDurationSeconds(note.Duration, secondsPerBeat);
                    double onsetSeconds = currentTime;
                    double offsetSeconds = currentTime + durationSeconds;

                    var midiNote = note.Pitch.ToMidiNote();
                    float velocity = note.Velocity.Value;

                    timeline._events.Add(new SynthNote(
                        midiNote,
                        velocity,
                        onsetSeconds,
                        offsetSeconds));

                    currentTime += durationSeconds;
                }
                else if (evt is Rest rest)
                {
                    // Advance time but don't add a note
                    double durationSeconds = CalculateDurationSeconds(rest.Duration, secondsPerBeat);
                    currentTime += durationSeconds;
                }
                else if (evt is Chord chord)
                {
                    // For chords, all notes start at the same time
                    double durationSeconds = CalculateDurationSeconds(chord.Duration, secondsPerBeat);
                    double onsetSeconds = currentTime;
                    double offsetSeconds = currentTime + durationSeconds;

                    float velocity = chord.Velocity.Value;

                    foreach (var pitch in chord.Pitches)
                    {
                        var midiNote = pitch.ToMidiNote();

                        timeline._events.Add(new SynthNote(
                            midiNote,
                            velocity,
                            onsetSeconds,
                            offsetSeconds));
                    }

                    currentTime += durationSeconds;
                }
            }
        }

        // Sort by onset time for easier processing
        timeline._events.Sort((a, b) => a.OnsetSeconds.CompareTo(b.OnsetSeconds));

        return timeline;
    }

    /// <summary>
    /// Calculates the duration in seconds for a given symbolic duration.
    /// </summary>
    private static double CalculateDurationSeconds(SymbolicDuration duration, double secondsPerBeat)
    {
        // Convert to beats (quarter notes)
        var beats = duration.ToBeats();

        // Convert beats to seconds
        return beats.ToDouble() * secondsPerBeat;
    }

    /// <summary>
    /// Gets the total duration of the timeline (latest note offset).
    /// </summary>
    public double GetTotalDuration()
    {
        if (_events.Count == 0)
        {
            return 0.0;
        }

        return _events.Max(e => e.OffsetSeconds);
    }
}
