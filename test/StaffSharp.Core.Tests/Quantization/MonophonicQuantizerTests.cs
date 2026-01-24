using StaffSharp.Notation;
using StaffSharp.Performance;
using StaffSharp.Quantization;

namespace StaffSharp.Core.Tests.Quantization;

public class MonophonicQuantizerTests
{
    [Fact]
    public void Constructor_InvalidQuantizationGrid_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new MonophonicQuantizer(new QuantizationOptions { QuantizationGrid = Rational.Zero }));
        Assert.Throws<ArgumentException>(() =>
            new MonophonicQuantizer(new QuantizationOptions { QuantizationGrid = Rational.Create(-1, 4) }));
    }

    [Fact]
    public void Constructor_InvalidDefaultDuration_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new MonophonicQuantizer(new QuantizationOptions { DefaultLastNoteDuration = Rational.Zero }));
        Assert.Throws<ArgumentException>(() =>
            new MonophonicQuantizer(new QuantizationOptions { DefaultLastNoteDuration = Rational.Create(-1, 1) }));
    }

    [Fact]
    public void Constructor_InvalidMinDuration_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new MonophonicQuantizer(new QuantizationOptions { MinNoteDuration = Rational.Zero }));
        Assert.Throws<ArgumentException>(() =>
            new MonophonicQuantizer(new QuantizationOptions { MinNoteDuration = Rational.Create(-1, 8) }));
    }

    [Fact]
    public void Quantize_EmptyOnsets_ReturnsEmptyList()
    {
        var quantizer = new MonophonicQuantizer();
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        var (notes, returnedTempoMap) = quantizer.Quantize([], [], tempoMap);

        Assert.Empty(notes);
        Assert.Same(tempoMap, returnedTempoMap);
    }

    [Fact]
    public void Quantize_MismatchedLengths_ThrowsException()
    {
        var quantizer = new MonophonicQuantizer();
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);
        var onsets = new[] { 0.0, 0.5, 1.0 };
        var pitches = new[] { 60, 62 }; // Mismatched length

        Assert.Throws<ArgumentException>(() =>
            quantizer.Quantize(
                onsets,
                pitches,
                tempoMap));
    }

    [Fact]
    public void Quantize_SingleNote_CreatesNoteWithDefaultDuration()
    {
        var quantizer = new MonophonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 4), // 16th notes
            DefaultLastNoteDuration = Rational.Create(1, 1) // Quarter note
        });

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);
        var onsets = new[] { 0.0 }; // At beat 0
        var pitches = new[] { 60 }; // Middle C

        var (notes, _) = quantizer.Quantize(
            onsets,
            pitches,
            tempoMap);

        Assert.Single(notes);

        var note = notes[0];
        Assert.Equal(Rational.Zero, note.OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), note.DurationBeats); // Default duration
        Assert.Equal(60, note.RawEvent.Pitch.Value);
        Assert.Equal(0.5f, note.RawEvent.Velocity.Value); // Default velocity
        Assert.Null(note.VoiceHint); // Monophonic
        Assert.Equal(ArticulationFlags.None, note.Articulation);
        Assert.Equal(4, note.QuantizationMetadata.Subdivision); // 1/4 grid denominator
        Assert.Equal(120.0, note.QuantizationMetadata.TempoAtOnset);
    }

    [Fact]
    public void Quantize_PerfectQuarterNotes_QuantizesCorrectly()
    {
        var quantizer = new MonophonicQuantizer(new QuantizationOptions { QuantizationGrid = Rational.Create(1, 4) });
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: 1 beat = 0.5 seconds
        // Perfect quarter notes: 0.0, 0.5, 1.0, 1.5
        var onsets = new[] { 0.0, 0.5, 1.0, 1.5 };
        var pitches = new[] { 60, 62, 64, 65 };

        var (notes, _) = quantizer.Quantize(
            onsets,
            pitches,
            tempoMap);

        Assert.Equal(4, notes.Count);

        // First note: beat 0, duration 1 beat
        Assert.Equal(Rational.Zero, notes[0].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), notes[0].DurationBeats);
        Assert.Equal(60, notes[0].RawEvent.Pitch.Value);

        // Second note: beat 1, duration 1 beat
        Assert.Equal(Rational.Create(1, 1), notes[1].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), notes[1].DurationBeats);
        Assert.Equal(62, notes[1].RawEvent.Pitch.Value);

        // Third note: beat 2, duration 1 beat
        Assert.Equal(Rational.Create(2, 1), notes[2].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), notes[2].DurationBeats);
        Assert.Equal(64, notes[2].RawEvent.Pitch.Value);

        // Fourth note: beat 3, default duration
        Assert.Equal(Rational.Create(3, 1), notes[3].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), notes[3].DurationBeats); // Default
        Assert.Equal(65, notes[3].RawEvent.Pitch.Value);
    }

    [Fact]
    public void Quantize_SixteenthNotes_QuantizesCorrectly()
    {
        var quantizer = new MonophonicQuantizer(new QuantizationOptions { QuantizationGrid = Rational.Create(1, 4) }); // 16th note grid
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: 1 beat = 0.5 seconds, 16th note = 0.125 seconds
        var onsets = new[] { 0.0, 0.125, 0.25, 0.375 };
        var pitches = new[] { 60, 62, 64, 65 };

        var (notes, _) = quantizer.Quantize(
            onsets,
            pitches,
            tempoMap);

        Assert.Equal(4, notes.Count);

        // Each note should be 1/4 beat apart (16th notes)
        Assert.Equal(Rational.Zero, notes[0].OnsetBeats);
        Assert.Equal(Rational.Create(1, 4), notes[0].DurationBeats);

        Assert.Equal(Rational.Create(1, 4), notes[1].OnsetBeats);
        Assert.Equal(Rational.Create(1, 4), notes[1].DurationBeats);

        Assert.Equal(Rational.Create(1, 2), notes[2].OnsetBeats);
        Assert.Equal(Rational.Create(1, 4), notes[2].DurationBeats);

        Assert.Equal(Rational.Create(3, 4), notes[3].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), notes[3].DurationBeats); // Default
    }

    [Fact]
    public void Quantize_SlightlyOffGrid_SnapsToGrid()
    {
        var quantizer = new MonophonicQuantizer(new QuantizationOptions { QuantizationGrid = Rational.Create(1, 2) }); // 8th note grid
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: 1 beat = 0.5 seconds
        // Slightly off 8th note grid: should snap to 0, 0.5, 1.0
        var onsets = new[] { 0.03, 0.52, 0.98 }; // Close to 0, 0.5, 1.0
        var pitches = new[] { 60, 62, 64 };

        var (notes, _) = quantizer.Quantize(
            onsets,
            pitches,
            tempoMap);

        Assert.Equal(3, notes.Count);

        // Should snap to nearest grid points
        Assert.Equal(Rational.Zero, notes[0].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), notes[1].OnsetBeats);
        Assert.Equal(Rational.Create(2, 1), notes[2].OnsetBeats);
    }

    [Fact]
    public void Quantize_VeryShortNote_EnforcesMinimumDuration()
    {
        var quantizer = new MonophonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 4),
            MinNoteDuration = Rational.Create(1, 8) // Min duration: 1/8 beat
        });

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // Two notes very close together (would result in < min duration)
        var onsets = new[] { 0.0, 0.05 }; // ~0.1 beat apart at 120 BPM
        var pitches = new[] { 60, 62 };

        var (notes, _) = quantizer.Quantize(
            onsets,
            pitches,
            tempoMap);

        Assert.Equal(2, notes.Count);

        // First note should have minimum duration, not calculated short duration
        Assert.True(notes[0].DurationBeats >= Rational.Create(1, 8));
    }

    [Fact]
    public void Quantize_DifferentTempo_ConvertsCorrectly()
    {
        var quantizer = new MonophonicQuantizer(new QuantizationOptions { QuantizationGrid = Rational.Create(1, 4) });
        var tempoMap = CreateTempoMap(60.0, TimeSignature.CommonTime); // Slower: 60 BPM

        // At 60 BPM: 1 beat = 1.0 second
        var onsets = new[] { 0.0, 1.0, 2.0 }; // Perfect beats
        var pitches = new[] { 60, 62, 64 };

        var (notes, _) = quantizer.Quantize(
            onsets,
            pitches,
            tempoMap);

        Assert.Equal(3, notes.Count);

        Assert.Equal(Rational.Zero, notes[0].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), notes[0].DurationBeats);

        Assert.Equal(Rational.Create(1, 1), notes[1].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), notes[1].DurationBeats);

        Assert.Equal(Rational.Create(2, 1), notes[2].OnsetBeats);
    }

    [Fact]
    public void Quantize_PreservesRawEventTiming()
    {
        var quantizer = new MonophonicQuantizer(new QuantizationOptions { QuantizationGrid = Rational.Create(1, 4) });
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // Slightly off-grid note
        var onsets = new[] { 0.03 }; // 0.03 seconds (slightly late)
        var pitches = new[] { 60 };

        var (notes, _) = quantizer.Quantize(
            onsets,
            pitches,
            tempoMap);

        Assert.Single(notes);

        var note = notes[0];

        // Quantized to beat 0
        Assert.Equal(Rational.Zero, note.OnsetBeats);

        // But raw event preserves original time
        Assert.Equal(TimeSpan.FromSeconds(0.03), note.RawEvent.Onset);

        // Quantization error should be recorded
        Assert.NotEqual(TimeSpan.Zero, note.QuantizationMetadata.OnsetError);
    }

    private static TempoMap CreateTempoMap(double bpm, TimeSignature timeSignature)
    {
        var tempoChanges = new[] { new TempoChange(Rational.Zero, bpm) };
        var timeSignatures = new[] { new TimeSignatureChange(Rational.Zero, timeSignature) };
        return new TempoMap(tempoChanges, timeSignatures);
    }
}
