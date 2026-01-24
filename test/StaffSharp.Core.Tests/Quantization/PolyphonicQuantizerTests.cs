using StaffSharp.Notation;
using StaffSharp.Performance;
using StaffSharp.Quantization;

namespace StaffSharp.Core.Tests.Quantization;

public class PolyphonicQuantizerTests
{
    [Fact]
    public void Constructor_InvalidQuantizationGrid_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PolyphonicQuantizer(new QuantizationOptions { QuantizationGrid = Rational.Zero }));
        Assert.Throws<ArgumentException>(() =>
            new PolyphonicQuantizer(new QuantizationOptions { QuantizationGrid = Rational.Create(-1, 4) }));
    }

    [Fact]
    public void Constructor_InvalidMinDuration_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new PolyphonicQuantizer(new QuantizationOptions { MinNoteDuration = Rational.Zero }));
        Assert.Throws<ArgumentException>(() =>
            new PolyphonicQuantizer(new QuantizationOptions { MinNoteDuration = Rational.Create(-1, 8) }));
    }

    [Fact]
    public void Quantize_EmptyNotes_ReturnsEmptyList()
    {
        var quantizer = new PolyphonicQuantizer();
        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        var (notes, returnedTempoMap) = quantizer.Quantize(
            Array.Empty<NoteEvent>(),
            tempoMap);

        Assert.Empty(notes);
        Assert.Same(tempoMap, returnedTempoMap);
    }

    [Fact]
    public void Quantize_SingleNote_QuantizesOnsetAndDuration()
    {
        var quantizer = new PolyphonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 4), // 16th notes
            MinNoteDuration = Rational.Create(1, 8)
        });

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: 1 beat = 0.5 seconds
        // Quarter note starting at beat 0
        var notes = new[]
        {
            new NoteEvent(
                Pitch: new MidiNote(60),
                Onset: TimeSpan.FromSeconds(0.0),
                Duration: TimeSpan.FromSeconds(0.5), // 1 beat = quarter note
                Velocity: new Velocity(0.7f))
        };

        var (quantizedNotes, _) = quantizer.Quantize(
            notes,
            tempoMap);

        Assert.Single(quantizedNotes);

        var quantized = quantizedNotes[0];
        Assert.Equal(Rational.Zero, quantized.OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), quantized.DurationBeats); // Snapped to quarter note
        Assert.Equal(60, quantized.RawEvent.Pitch.Value);
        Assert.Equal(0.7f, quantized.RawEvent.Velocity.Value);
    }

    [Fact]
    public void Quantize_MultipleSimultaneousNotes_PreservesPolyphony()
    {
        var quantizer = new PolyphonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 4)
        });

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // Chord: C-E-G starting simultaneously
        var notes = new[]
        {
            new NoteEvent(
                Pitch: new MidiNote(60), // C
                Onset: TimeSpan.FromSeconds(0.0),
                Duration: TimeSpan.FromSeconds(0.5),
                Velocity: new Velocity(0.8f)),
            new NoteEvent(
                Pitch: new MidiNote(64), // E
                Onset: TimeSpan.FromSeconds(0.0),
                Duration: TimeSpan.FromSeconds(0.5),
                Velocity: new Velocity(0.7f)),
            new NoteEvent(
                Pitch: new MidiNote(67), // G
                Onset: TimeSpan.FromSeconds(0.0),
                Duration: TimeSpan.FromSeconds(0.5),
                Velocity: new Velocity(0.75f))
        };

        var (quantizedNotes, _) = quantizer.Quantize(
            notes,
            tempoMap);

        Assert.Equal(3, quantizedNotes.Count);

        // All should start at beat 0
        Assert.All(quantizedNotes, n => Assert.Equal(Rational.Zero, n.OnsetBeats));

        // All should have quarter note duration
        Assert.All(quantizedNotes, n => Assert.Equal(Rational.Create(1, 1), n.DurationBeats));

        // Pitches should be preserved
        Assert.Equal(60, quantizedNotes[0].RawEvent.Pitch.Value);
        Assert.Equal(64, quantizedNotes[1].RawEvent.Pitch.Value);
        Assert.Equal(67, quantizedNotes[2].RawEvent.Pitch.Value);

        // Velocities should be preserved
        Assert.Equal(0.8f, quantizedNotes[0].RawEvent.Velocity.Value);
        Assert.Equal(0.7f, quantizedNotes[1].RawEvent.Velocity.Value);
        Assert.Equal(0.75f, quantizedNotes[2].RawEvent.Velocity.Value);
    }

    [Fact]
    public void Quantize_OverlappingNotes_HandlesCorrectly()
    {
        var quantizer = new PolyphonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 4)
        });

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: 1 beat = 0.5 seconds
        // Note 1: starts at 0, lasts 1 second (2 beats)
        // Note 2: starts at 0.5 seconds (1 beat), lasts 0.5 seconds (1 beat)
        var notes = new[]
        {
            new NoteEvent(
                Pitch: new MidiNote(60),
                Onset: TimeSpan.FromSeconds(0.0),
                Duration: TimeSpan.FromSeconds(1.0), // 2 beats
                Velocity: new Velocity(0.8f)),
            new NoteEvent(
                Pitch: new MidiNote(64),
                Onset: TimeSpan.FromSeconds(0.5), // 1 beat
                Duration: TimeSpan.FromSeconds(0.5), // 1 beat
                Velocity: new Velocity(0.7f))
        };

        var (quantizedNotes, _) = quantizer.Quantize(
            notes,
            tempoMap);

        Assert.Equal(2, quantizedNotes.Count);

        // First note: beat 0, duration 2 beats
        Assert.Equal(Rational.Zero, quantizedNotes[0].OnsetBeats);
        Assert.Equal(Rational.Create(2, 1), quantizedNotes[0].DurationBeats);

        // Second note: beat 1, duration 1 beat
        Assert.Equal(Rational.Create(1, 1), quantizedNotes[1].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), quantizedNotes[1].DurationBeats);
    }

    [Fact]
    public void Quantize_SlightlyOffGrid_SnapsOnsetToGrid()
    {
        var quantizer = new PolyphonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 2) // 8th note grid
        });

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // Slightly off-grid onset
        var notes = new[]
        {
            new NoteEvent(
                Pitch: new MidiNote(60),
                Onset: TimeSpan.FromSeconds(0.03), // Close to 0
                Duration: TimeSpan.FromSeconds(0.48), // Close to 0.5
                Velocity: new Velocity(0.8f))
        };

        var (quantizedNotes, _) = quantizer.Quantize(
            notes,
            tempoMap);

        Assert.Single(quantizedNotes);

        // Should snap to beat 0
        Assert.Equal(Rational.Zero, quantizedNotes[0].OnsetBeats);

        // Duration should snap to nearest valid value (1 beat = quarter note)
        Assert.Equal(Rational.Create(1, 1), quantizedNotes[0].DurationBeats);
    }

    [Fact]
    public void Quantize_VeryShortDuration_EnforcesMinimumDuration()
    {
        var quantizer = new PolyphonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 4),
            MinNoteDuration = Rational.Create(1, 8) // Min duration: 1/8 beat
        });

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // Very short note (would quantize to less than minimum)
        var notes = new[]
        {
            new NoteEvent(
                Pitch: new MidiNote(60),
                Onset: TimeSpan.FromSeconds(0.0),
                Duration: TimeSpan.FromSeconds(0.01), // Very short
                Velocity: new Velocity(0.8f))
        };

        var (quantizedNotes, _) = quantizer.Quantize(
            notes,
            tempoMap);

        Assert.Single(quantizedNotes);

        // Should enforce minimum duration
        Assert.True(quantizedNotes[0].DurationBeats >= Rational.Create(1, 8));
    }

    [Fact]
    public void Quantize_DottedNoteDuration_QuantizesCorrectly()
    {
        var quantizer = new PolyphonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 4)
        });

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: dotted quarter = 0.75 seconds (1.5 beats)
        var notes = new[]
        {
            new NoteEvent(
                Pitch: new MidiNote(60),
                Onset: TimeSpan.FromSeconds(0.0),
                Duration: TimeSpan.FromSeconds(0.75), // 1.5 beats
                Velocity: new Velocity(0.8f))
        };

        var (quantizedNotes, _) = quantizer.Quantize(
            notes,
            tempoMap);

        Assert.Single(quantizedNotes);

        // Should quantize to dotted quarter (3/2 beats)
        Assert.Equal(Rational.Create(3, 2), quantizedNotes[0].DurationBeats);
    }

    [Fact]
    public void Quantize_DifferentTempo_ConvertsCorrectly()
    {
        var quantizer = new PolyphonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 4)
        });

        var tempoMap = CreateTempoMap(60.0, TimeSignature.CommonTime); // 60 BPM

        // At 60 BPM: 1 beat = 1.0 second
        var notes = new[]
        {
            new NoteEvent(
                Pitch: new MidiNote(60),
                Onset: TimeSpan.FromSeconds(0.0),
                Duration: TimeSpan.FromSeconds(1.0), // 1 beat
                Velocity: new Velocity(0.8f))
        };

        var (quantizedNotes, _) = quantizer.Quantize(
            notes,
            tempoMap);

        Assert.Single(quantizedNotes);

        Assert.Equal(Rational.Zero, quantizedNotes[0].OnsetBeats);
        Assert.Equal(Rational.Create(1, 1), quantizedNotes[0].DurationBeats);
    }

    [Fact]
    public void Quantize_PreservesRawEventData()
    {
        var quantizer = new PolyphonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 4)
        });

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // Slightly off-grid note
        var originalOnset = TimeSpan.FromSeconds(0.03);
        var originalDuration = TimeSpan.FromSeconds(0.52);
        var notes = new[]
        {
            new NoteEvent(
                Pitch: new MidiNote(60),
                Onset: originalOnset,
                Duration: originalDuration,
                Velocity: new Velocity(0.8f))
        };

        var (quantizedNotes, _) = quantizer.Quantize(
            notes,
            tempoMap);

        Assert.Single(quantizedNotes);

        var quantized = quantizedNotes[0];

        // Quantized values should be on grid
        Assert.Equal(Rational.Zero, quantized.OnsetBeats);

        // But raw event should preserve original timing
        Assert.Equal(originalOnset, quantized.RawEvent.Onset);
        Assert.Equal(originalDuration, quantized.RawEvent.Duration);

        // Quantization errors should be recorded
        Assert.NotEqual(TimeSpan.Zero, quantized.QuantizationMetadata.OnsetError);
        Assert.NotEqual(TimeSpan.Zero, quantized.QuantizationMetadata.DurationError);
    }

    [Fact]
    public void Quantize_MixedDurations_HandlesCorrectly()
    {
        var quantizer = new PolyphonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 4)
        });

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: 1 beat = 0.5 seconds
        // Mix of whole, half, quarter, eighth notes
        var notes = new[]
        {
            new NoteEvent(new MidiNote(60), TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(2.0), new Velocity(0.8f)), // Whole note (4 beats)
            new NoteEvent(new MidiNote(62), TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(1.0), new Velocity(0.8f)), // Half note (2 beats)
            new NoteEvent(new MidiNote(64), TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(0.5), new Velocity(0.8f)), // Quarter (1 beat)
            new NoteEvent(new MidiNote(65), TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(0.25), new Velocity(0.8f))  // Eighth (0.5 beat)
        };

        var (quantizedNotes, _) = quantizer.Quantize(
            notes,
            tempoMap);

        Assert.Equal(4, quantizedNotes.Count);

        // All start at beat 0
        Assert.All(quantizedNotes, n => Assert.Equal(Rational.Zero, n.OnsetBeats));

        // Verify quantized durations
        Assert.Equal(Rational.Create(4, 1), quantizedNotes[0].DurationBeats); // Whole
        Assert.Equal(Rational.Create(2, 1), quantizedNotes[1].DurationBeats); // Half
        Assert.Equal(Rational.Create(1, 1), quantizedNotes[2].DurationBeats); // Quarter
        Assert.Equal(Rational.Create(1, 2), quantizedNotes[3].DurationBeats); // Eighth
    }

    [Fact]
    public void Quantize_OnsetAndOffsetSnapToSamePoint_EnforcesMinimumDuration()
    {
        // Edge case: when both onset and offset snap to the same grid point,
        // the minimum duration enforcement should prevent zero-duration notes
        var quantizer = new PolyphonicQuantizer(new QuantizationOptions
        {
            QuantizationGrid = Rational.Create(1, 4), // Quarter note grid
            MinNoteDuration = Rational.Create(1, 8)   // Min duration: 1/8 beat
        });

        var tempoMap = CreateTempoMap(120.0, TimeSignature.CommonTime);

        // At 120 BPM: 1 beat = 0.5 seconds
        // Note near beat 1.0 with very short duration
        // Onset at 0.48s (~0.96 beats) snaps to 1.0
        // Offset at 0.49s (~0.98 beats) also snaps to 1.0
        // Result: would be zero duration without min duration enforcement
        var notes = new[]
        {
            new NoteEvent(
                Pitch: new MidiNote(60),
                Onset: TimeSpan.FromSeconds(0.48),  // ~0.96 beats
                Duration: TimeSpan.FromSeconds(0.01), // Very short
                Velocity: new Velocity(0.8f))
        };

        var (quantizedNotes, _) = quantizer.Quantize(
            notes,
            tempoMap);

        Assert.Single(quantizedNotes);

        var quantized = quantizedNotes[0];

        // Onset should snap to beat 1.0
        Assert.Equal(Rational.Create(1, 1), quantized.OnsetBeats);

        // Duration should be enforced to minimum (1/8 beat), not zero
        Assert.Equal(Rational.Create(1, 8), quantized.DurationBeats);

        // Offset should be at beat 1.125 (1 + 1/8)
        var expectedOffset = Rational.Create(9, 8); // 1 + 1/8 = 9/8
        Assert.Equal(expectedOffset, quantized.OnsetBeats + quantized.DurationBeats);
    }

    private static TempoMap CreateTempoMap(double bpm, TimeSignature timeSignature)
    {
        var tempoChanges = new[] { new TempoChange(Rational.Zero, bpm) };
        var timeSignatures = new[] { new TimeSignatureChange(Rational.Zero, timeSignature) };
        return new TempoMap(tempoChanges, timeSignatures);
    }
}
