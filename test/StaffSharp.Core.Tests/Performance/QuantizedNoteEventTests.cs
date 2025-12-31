using StaffSharp.Performance;

namespace StaffSharp.Tests.Performance;

public class QuantizedNoteEventTests
{
    [Fact]
    public void QuantizedNoteEvent_PreservesRawEvent()
    {
        var rawEvent = new NoteEvent(
            Pitch: MidiNote.Create(60),
            Onset: TimeSpan.FromSeconds(1.0),
            Duration: TimeSpan.FromSeconds(0.5),
            Velocity: Velocity.Create(0.8f));

        var quantizationMetadata = new QuantizationMetadata(
            Subdivision: 16,
            TempoAtOnset: 120,
            OnsetError: TimeSpan.FromMilliseconds(10),
            DurationError: TimeSpan.FromMilliseconds(5));

        var quantized = new QuantizedNoteEvent(
            rawEvent: rawEvent,
            onsetBeats: Rational.Create(2, 1),
            durationBeats: Rational.Create(1, 1),
            quantizationMetadata: quantizationMetadata);

        Assert.Equal(rawEvent, quantized.RawEvent);
        Assert.Equal(TimeSpan.FromSeconds(1.0), quantized.RawEvent.Onset);
    }

    [Fact]
    public void QuantizedNoteEvent_CalculatesOffsetCorrectly()
    {
        var rawEvent = new NoteEvent(
            Pitch: MidiNote.Create(60),
            Onset: TimeSpan.FromSeconds(0),
            Duration: TimeSpan.FromSeconds(0.5),
            Velocity: Velocity.Create(0.8f));

        var quantizationMetadata = new QuantizationMetadata(
            Subdivision: 16,
            TempoAtOnset: 120,
            OnsetError: TimeSpan.Zero,
            DurationError: TimeSpan.Zero);

        var quantized = new QuantizedNoteEvent(
            rawEvent: rawEvent,
            onsetBeats: Rational.Create(2, 1),
            durationBeats: Rational.Create(3, 1),
            quantizationMetadata: quantizationMetadata);

        // Offset = 2 + 3 = 5
        Assert.Equal(Rational.Create(5, 1), quantized.OffsetBeats);
    }

    [Fact]
    public void QuantizedNoteEvent_StoresQuantizationMetadata()
    {
        var rawEvent = new NoteEvent(
            Pitch: MidiNote.Create(60),
            Onset: TimeSpan.FromSeconds(0),
            Duration: TimeSpan.FromSeconds(0.5),
            Velocity: Velocity.Create(0.8f));

        var quantizationMetadata = new QuantizationMetadata(
            Subdivision: 16,
            TempoAtOnset: 120,
            OnsetError: TimeSpan.FromMilliseconds(15),
            DurationError: TimeSpan.FromMilliseconds(8));

        var quantized = new QuantizedNoteEvent(
            rawEvent: rawEvent,
            onsetBeats: Rational.Create(2, 1),
            durationBeats: Rational.Create(1, 1),
            quantizationMetadata: quantizationMetadata);

        Assert.Equal(16, quantized.QuantizationMetadata.Subdivision);
        Assert.Equal(120, quantized.QuantizationMetadata.TempoAtOnset);
        Assert.Equal(TimeSpan.FromMilliseconds(15), quantized.QuantizationMetadata.OnsetError);
        Assert.Equal(TimeSpan.FromMilliseconds(8), quantized.QuantizationMetadata.DurationError);
    }

    [Fact]
    public void QuantizedNoteEvent_SupportsVoiceHint()
    {
        var rawEvent = new NoteEvent(
            Pitch: MidiNote.Create(60),
            Onset: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(0.5),
            Velocity: Velocity.Create(0.8f));

        var quantizationMetadata = new QuantizationMetadata(16, 120, TimeSpan.Zero, TimeSpan.Zero);

        var quantized = new QuantizedNoteEvent(
            rawEvent: rawEvent,
            onsetBeats: Rational.Create(2, 1),
            durationBeats: Rational.Create(1, 1),
            quantizationMetadata: quantizationMetadata,
            voiceHint: 2);

        Assert.Equal(2, quantized.VoiceHint);
    }

    [Fact]
    public void QuantizedNoteEvent_SupportsArticulationFlags()
    {
        var rawEvent = new NoteEvent(
            Pitch: MidiNote.Create(60),
            Onset: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(0.1),  // Short duration
            Velocity: Velocity.Create(0.95f));      // High velocity

        var quantizationMetadata = new QuantizationMetadata(16, 120, TimeSpan.Zero, TimeSpan.Zero);

        var quantized = new QuantizedNoteEvent(
            rawEvent: rawEvent,
            onsetBeats: Rational.Create(2, 1),
            durationBeats: Rational.Create(1, 4),
            quantizationMetadata: quantizationMetadata,
            articulation: ArticulationFlags.Staccato | ArticulationFlags.Accent);

        Assert.True(quantized.Articulation.HasFlag(ArticulationFlags.Staccato));
        Assert.True(quantized.Articulation.HasFlag(ArticulationFlags.Accent));
        Assert.False(quantized.Articulation.HasFlag(ArticulationFlags.Legato));
    }
}
