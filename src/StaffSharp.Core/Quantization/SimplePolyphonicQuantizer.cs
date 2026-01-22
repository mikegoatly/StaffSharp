using StaffSharp.Performance;

namespace StaffSharp.Quantization;

/// <summary>
/// Simple polyphonic quantizer for note events with known durations.
/// Snaps both onsets and offsets to rhythmic grid, preserving polyphony.
/// Assumes single tempo and time signature.
/// </summary>
public sealed class SimplePolyphonicQuantizer : IPolyphonicQuantizer
{
    private readonly Rational _quantizationGrid;
    private readonly Rational _minNoteDuration;

    public SimplePolyphonicQuantizer(QuantizationOptions? options = null)
    {
        options ??= new QuantizationOptions();
        options.Validate();

        _quantizationGrid = options.QuantizationGrid;
        _minNoteDuration = options.MinNoteDuration;
    }

    public (IReadOnlyList<QuantizedNoteEvent> Notes, TempoMap TempoMap) Quantize(
        IReadOnlyList<NoteEvent> notes,
        TempoMap tempoMap)
    {
        ArgumentNullException.ThrowIfNull(notes);
        ArgumentNullException.ThrowIfNull(tempoMap);

        if (notes.Count == 0)
        {
            return (Array.Empty<QuantizedNoteEvent>(), tempoMap);
        }

        if (tempoMap.TempoChanges.Count == 0)
        {
            throw new ArgumentException("TempoMap must have at least one tempo");
        }

        // Assume single tempo for the simple quantizer
        var bpm = tempoMap.TempoChanges[0].BeatsPerMinute;
        var secondsPerBeat = 60.0 / bpm;

        var quantizedNotes = new List<QuantizedNoteEvent>(notes.Count);

        // Calculate subdivision for metadata (denominator of grid)
        var subdivision = _quantizationGrid.Denominator;

        foreach (var note in notes)
        {
            // Step 1: Convert onset and offset to beat positions
            var onsetBeats = note.Onset.TotalSeconds / secondsPerBeat;
            var offsetBeats = note.Offset.TotalSeconds / secondsPerBeat;
            var durationBeats = offsetBeats - onsetBeats;

            // Step 2: Quantize onset to grid
            var quantizedOnsetBeats = QuantizeToBeat(onsetBeats, _quantizationGrid);

            // Step 3: Quantize duration to nearest valid note value
            var quantizedDurationBeats = QuantizeDuration(durationBeats, _minNoteDuration);

            // Step 4: Calculate quantization errors
            var quantizedOnsetSeconds = quantizedOnsetBeats.ToDouble() * secondsPerBeat;
            var onsetError = TimeSpan.FromSeconds(quantizedOnsetSeconds - note.Onset.TotalSeconds);

            var quantizedDurationSeconds = quantizedDurationBeats.ToDouble() * secondsPerBeat;
            var durationError = TimeSpan.FromSeconds(quantizedDurationSeconds - note.Duration.TotalSeconds);

            // Step 5: Create quantization metadata
            var metadata = new QuantizationMetadata(
                Subdivision: subdivision,
                TempoAtOnset: bpm,
                OnsetError: onsetError,
                DurationError: durationError
            );

            // Step 6: Create quantized note event
            quantizedNotes.Add(new QuantizedNoteEvent(
                rawEvent: note,
                onsetBeats: quantizedOnsetBeats,
                durationBeats: quantizedDurationBeats,
                quantizationMetadata: metadata
            ));
        }

        return (quantizedNotes, tempoMap);
    }

    /// <summary>
    /// Quantizes a beat position to the nearest grid point.
    /// </summary>
    private static Rational QuantizeToBeat(double beatPosition, Rational grid)
    {
        // Round to nearest grid point
        var gridSize = (double)grid.Numerator / grid.Denominator;
        var gridMultiplier = (int)Math.Round(beatPosition / gridSize);
        return Rational.Create(gridMultiplier * grid.Numerator, grid.Denominator);
    }

    /// <summary>
    /// Quantizes a duration to a valid musical note value.
    /// Tries to find the closest common duration (whole, half, quarter, etc.)
    /// </summary>
    private static Rational QuantizeDuration(double durationBeats, Rational minDuration)
    {
        // Common note durations in beats (assuming 4/4 time where quarter = 1 beat)
        var validDurations = new[]
        {
            Rational.Create(4, 1),   // Whole note
            Rational.Create(3, 1),   // Dotted half
            Rational.Create(2, 1),   // Half note
            Rational.Create(3, 2),   // Dotted quarter
            Rational.Create(1, 1),   // Quarter note
            Rational.Create(3, 4),   // Dotted eighth
            Rational.Create(1, 2),   // Eighth note
            Rational.Create(3, 8),   // Dotted sixteenth
            Rational.Create(1, 4),   // Sixteenth note
            Rational.Create(1, 8),   // Thirty-second note
        };

        // Find closest valid duration
        Rational closest = validDurations[0];
        double smallestDiff = Math.Abs(closest.ToDouble() - durationBeats);

        foreach (var duration in validDurations)
        {
            var diff = Math.Abs(duration.ToDouble() - durationBeats);
            if (diff < smallestDiff)
            {
                smallestDiff = diff;
                closest = duration;
            }
        }

        // Ensure minimum duration
        if (closest < minDuration)
        {
            closest = minDuration;
        }

        return closest;
    }
}
