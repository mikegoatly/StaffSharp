using StaffSharp;
using StaffSharp.Audio.Analysis.Quantization;
using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Tests.Analysis.Quantization;

public class SimpleQuantizerTests
{
    [Fact]
    public void Constructor_InvalidQuantizationGrid_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new SimpleQuantizer(quantizationGrid: Rational.Zero));
        Assert.Throws<ArgumentException>(() =>
            new SimpleQuantizer(quantizationGrid: Rational.Create(-1, 4)));
    }

    [Fact]
    public void Constructor_InvalidDefaultDuration_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new SimpleQuantizer(defaultLastNoteDuration: Rational.Zero));
        Assert.Throws<ArgumentException>(() =>
            new SimpleQuantizer(defaultLastNoteDuration: Rational.Create(-1, 1)));
    }

    [Fact]
    public void Constructor_InvalidMinDuration_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new SimpleQuantizer(minNoteDuration: Rational.Zero));
        Assert.Throws<ArgumentException>(() =>
            new SimpleQuantizer(minNoteDuration: Rational.Create(-1, 8)));
    }

    [Fact]
    public void Quantize_EmptyOnsets_ReturnsNull()
    {
        var quantizer = new SimpleQuantizer();
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        var result = quantizer.Quantize(
            ReadOnlySpan<double>.Empty,
            ReadOnlySpan<int>.Empty,
            tempoMap);

        Assert.Null(result);
    }

    [Fact]
    public void Quantize_MismatchedLengths_ThrowsException()
    {
        var quantizer = new SimpleQuantizer();
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);
        var onsets = new[] { 0.0, 0.5, 1.0 };
        var pitches = new[] { 60, 62 }; // Mismatched length

        Assert.Throws<ArgumentException>(() =>
            quantizer.Quantize(onsets, pitches, tempoMap));
    }

    [Fact]
    public void Quantize_SingleNote_CreatesNoteWithDefaultDuration()
    {
        var quantizer = new SimpleQuantizer(
            quantizationGrid: Rational.Create(1, 4), // 16th notes
            defaultLastNoteDuration: Rational.Create(1, 1)); // Quarter note

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);
        var onsets = new[] { 0.0 }; // At beat 0
        var pitches = new[] { 60 }; // Middle C

        var result = quantizer.Quantize(onsets, pitches, tempoMap);

        Assert.NotNull(result);
        Assert.Single(result!);

        var note = result[0];
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
        var quantizer = new SimpleQuantizer(quantizationGrid: Rational.Create(1, 4));
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: 1 beat = 0.5 seconds
        // Perfect quarter notes: 0.0, 0.5, 1.0, 1.5
        var onsets = new[] { 0.0, 0.5, 1.0, 1.5 };
        var pitches = new[] { 60, 62, 64, 65 };

        var result = quantizer.Quantize(onsets, pitches, tempoMap);

        Assert.NotNull(result);
        Assert.Equal(4, result!.Count);

        // First note: beat 0, duration 1 beat
        Assert.Equal(Rational.Zero, result[0].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), result[0].DurationBeats);
        Assert.Equal(60, result[0].RawEvent.Pitch.Value);

        // Second note: beat 1, duration 1 beat
        Assert.Equal(Rational.Create(1, 1), result[1].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), result[1].DurationBeats);
        Assert.Equal(62, result[1].RawEvent.Pitch.Value);

        // Third note: beat 2, duration 1 beat
        Assert.Equal(Rational.Create(2, 1), result[2].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), result[2].DurationBeats);
        Assert.Equal(64, result[2].RawEvent.Pitch.Value);

        // Fourth note: beat 3, default duration
        Assert.Equal(Rational.Create(3, 1), result[3].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), result[3].DurationBeats); // Default
        Assert.Equal(65, result[3].RawEvent.Pitch.Value);
    }

    [Fact]
    public void Quantize_SixteenthNotes_QuantizesCorrectly()
    {
        var quantizer = new SimpleQuantizer(quantizationGrid: Rational.Create(1, 4)); // 16th note grid
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: 1 beat = 0.5 seconds, 16th note = 0.125 seconds
        var onsets = new[] { 0.0, 0.125, 0.25, 0.375 };
        var pitches = new[] { 60, 62, 64, 65 };

        var result = quantizer.Quantize(onsets, pitches, tempoMap);

        Assert.NotNull(result);
        Assert.Equal(4, result!.Count);

        // Each note should be 1/4 beat apart (16th notes)
        Assert.Equal(Rational.Zero, result[0].OnsetBeats);
        Assert.Equal(Rational.Create(1, 4), result[0].DurationBeats);

        Assert.Equal(Rational.Create(1, 4), result[1].OnsetBeats);
        Assert.Equal(Rational.Create(1, 4), result[1].DurationBeats);

        Assert.Equal(Rational.Create(1, 2), result[2].OnsetBeats);
        Assert.Equal(Rational.Create(1, 4), result[2].DurationBeats);

        Assert.Equal(Rational.Create(3, 4), result[3].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), result[3].DurationBeats); // Default
    }

    [Fact]
    public void Quantize_SlightlyOffGrid_SnapsToGrid()
    {
        var quantizer = new SimpleQuantizer(quantizationGrid: Rational.Create(1, 2)); // 8th note grid
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: 1 beat = 0.5 seconds
        // Slightly off 8th note grid: should snap to 0, 0.5, 1.0
        var onsets = new[] { 0.03, 0.52, 0.98 }; // Close to 0, 0.5, 1.0
        var pitches = new[] { 60, 62, 64 };

        var result = quantizer.Quantize(onsets, pitches, tempoMap);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);

        // Should snap to nearest grid points
        Assert.Equal(Rational.Zero, result[0].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), result[1].OnsetBeats);
        Assert.Equal(Rational.Create(2, 1), result[2].OnsetBeats);
    }

    [Fact]
    public void Quantize_VeryShortNote_EnforcesMinimumDuration()
    {
        var quantizer = new SimpleQuantizer(
            quantizationGrid: Rational.Create(1, 4),
            minNoteDuration: Rational.Create(1, 8)); // Min duration: 1/8 beat

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // Two notes very close together (would result in < min duration)
        var onsets = new[] { 0.0, 0.05 }; // ~0.1 beat apart at 120 BPM
        var pitches = new[] { 60, 62 };

        var result = quantizer.Quantize(onsets, pitches, tempoMap);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);

        // First note should have minimum duration, not calculated short duration
        Assert.True(result[0].DurationBeats >= Rational.Create(1, 8));
    }

    [Fact]
    public void Quantize_DifferentTempo_ConvertsCorrectly()
    {
        var quantizer = new SimpleQuantizer(quantizationGrid: Rational.Create(1, 4));
        var tempoMap = CreateTempoMap(60.0, TimeSignature.CommonTime); // Slower: 60 BPM

        // At 60 BPM: 1 beat = 1.0 second
        var onsets = new[] { 0.0, 1.0, 2.0 }; // Perfect beats
        var pitches = new[] { 60, 62, 64 };

        var result = quantizer.Quantize(onsets, pitches, tempoMap);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Count);

        Assert.Equal(Rational.Zero, result[0].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), result[0].DurationBeats);

        Assert.Equal(Rational.Create(1, 1), result[1].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), result[1].DurationBeats);

        Assert.Equal(Rational.Create(2, 1), result[2].OnsetBeats);
    }

    [Fact]
    public void Quantize_EighthNoteGrid_QuantizesCorrectly()
    {
        var quantizer = new SimpleQuantizer(quantizationGrid: Rational.Create(1, 2)); // 8th note grid
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: 8th note = 0.25 seconds
        var onsets = new[] { 0.0, 0.25, 0.5, 0.75 };
        var pitches = new[] { 60, 62, 64, 65 };

        var result = quantizer.Quantize(onsets, pitches, tempoMap);

        Assert.NotNull(result);
        Assert.Equal(4, result!.Count);

        // Each should be 1/2 beat (8th note) apart
        Assert.Equal(Rational.Zero, result[0].OnsetBeats);
        Assert.Equal(Rational.Create(1, 2), result[0].DurationBeats);

        Assert.Equal(Rational.Create(1, 2), result[1].OnsetBeats);
        Assert.Equal(Rational.Create(1, 2), result[1].DurationBeats);

        Assert.Equal(Rational.Create(1, 1), result[2].OnsetBeats);
        Assert.Equal(Rational.Create(1, 2), result[2].DurationBeats);

        Assert.Equal(Rational.Create(3, 2), result[3].OnsetBeats);
    }

    [Fact]
    public void Quantize_MixedRhythm_HandlesCorrectly()
    {
        var quantizer = new SimpleQuantizer(quantizationGrid: Rational.Create(1, 4));
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // Mixed rhythm: quarter, 2 eighths, quarter
        // At 120 BPM: quarter = 0.5s, eighth = 0.25s
        var onsets = new[] { 0.0, 0.5, 0.75, 1.25 };
        var pitches = new[] { 60, 62, 64, 65 };

        var result = quantizer.Quantize(onsets, pitches, tempoMap);

        Assert.NotNull(result);
        Assert.Equal(4, result!.Count);

        // Note 1: beat 0, duration 1 beat (to beat 1)
        Assert.Equal(Rational.Zero, result[0].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), result[0].DurationBeats);

        // Note 2: beat 1, duration 1/2 beat (to beat 1.5)
        Assert.Equal(Rational.Create(1, 1), result[1].OnsetBeats);
        Assert.Equal(Rational.Create(1, 2), result[1].DurationBeats);

        // Note 3: beat 1.5, duration 1 beat (to beat 2.5)
        Assert.Equal(Rational.Create(3, 2), result[2].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), result[2].DurationBeats);

        // Note 4: beat 2.5, default duration
        Assert.Equal(Rational.Create(5, 2), result[3].OnsetBeats);
    }

    [Fact]
    public void Quantize_CustomDefaultLastNoteDuration_AppliesCorrectly()
    {
        var quantizer = new SimpleQuantizer(
            quantizationGrid: Rational.Create(1, 4),
            defaultLastNoteDuration: Rational.Create(2, 1)); // Half note for last note

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);
        var onsets = new[] { 0.0, 0.5 };
        var pitches = new[] { 60, 62 };

        var result = quantizer.Quantize(onsets, pitches, tempoMap);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);

        // Last note should use custom default duration
        Assert.Equal(Rational.Create(2, 1), result[1].DurationBeats);
    }

    [Fact]
    public void Quantize_PreservesRawEventTiming()
    {
        var quantizer = new SimpleQuantizer(quantizationGrid: Rational.Create(1, 4));
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // Slightly off-grid note
        var onsets = new[] { 0.03 }; // 0.03 seconds (slightly late)
        var pitches = new[] { 60 };

        var result = quantizer.Quantize(onsets, pitches, tempoMap);

        Assert.NotNull(result);
        Assert.Single(result!);

        var note = result[0];

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
