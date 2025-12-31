using StaffSharp;
using StaffSharp.Audio;
using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Tests;

/// <summary>
/// Factory methods for creating test data with correct constructors.
/// </summary>
internal static class TestDataFactory
{
    /// <summary>
    /// Creates an AudioBoundaries instance for testing.
    /// </summary>
    public static AudioBoundaries CreateAudioBoundaries(
        AudioBuffer audio,
        int startSample,
        int endSample,
        TimeSpan? leadingSilence = null,
        TimeSpan? trailingSilence = null)
    {
        return new AudioBoundaries(
            startSample,
            endSample,
            audio.SampleRate,
            leadingSilence ?? TimeSpan.Zero,
            trailingSilence ?? TimeSpan.Zero);
    }

    /// <summary>
    /// Creates a QuantizedNoteEvent for testing.
    /// </summary>
    public static QuantizedNoteEvent CreateQuantizedNoteEvent(
        int midiPitch,
        double onsetBeats,
        double durationBeats,
        double onsetSeconds = 0.0,
        double durationSeconds = 0.0)
    {
        var rawEvent = new NoteEvent(
            Pitch: new MidiNote(midiPitch),
            Onset: TimeSpan.FromSeconds(onsetSeconds),
            Duration: TimeSpan.FromSeconds(durationSeconds),
            Velocity: new Velocity(0.8f));

        var metadata = new QuantizationMetadata(
            Subdivision: 16,
            TempoAtOnset: 120.0,
            OnsetError: TimeSpan.Zero,
            DurationError: TimeSpan.Zero);

        return new QuantizedNoteEvent(
            rawEvent,
            Rational.Create((int)(onsetBeats * 4.0f), 4),
            Rational.Create((int)(durationBeats * 4.0f), 4),
            metadata);
    }
}
