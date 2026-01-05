using StaffSharp.Performance;

namespace StaffSharp.Audio.Analysis.Quantization;

/// <summary>
/// Simple rhythm quantizer.
/// Snaps onsets to a quantization grid and infers durations from inter-onset intervals.
/// Assumes single tempo and time signature.
/// </summary>
public sealed class SimpleQuantizer : IQuantizer
{
    private readonly Rational _quantizationGrid;
    private readonly Rational _defaultLastNoteDuration;
    private readonly Rational _minNoteDuration;

    public SimpleQuantizer(QuantizationOptions? options = null)
    {
        options ??= new QuantizationOptions();
        options.Validate();

        _quantizationGrid = options.QuantizationGrid;
        _defaultLastNoteDuration = options.DefaultLastNoteDuration;
        _minNoteDuration = options.MinNoteDuration;
    }

    public IReadOnlyList<QuantizedNoteEvent>? Quantize(
        ReadOnlySpan<double> onsetTimes,
        ReadOnlySpan<int> pitches,
        TempoMap tempoMap)
    {
        ArgumentNullException.ThrowIfNull(tempoMap);

        if (onsetTimes.Length == 0)
            return null;

        if (onsetTimes.Length != pitches.Length)
            throw new ArgumentException("Onset times and pitches must have same length");

        if (tempoMap.TempoChanges.Count == 0 || tempoMap.TimeSignatures.Count == 0)
            throw new ArgumentException("TempoMap must have at least one tempo and time signature");

        // Assume single tempo and time signature for the simple quantizer
        var bpm = tempoMap.TempoChanges[0].BeatsPerMinute;
        var secondsPerBeat = 60.0 / bpm;

        var notes = new List<QuantizedNoteEvent>(onsetTimes.Length);

        // Calculate subdivision for metadata (denominator of grid)
        var subdivision = _quantizationGrid.Denominator;

        for (int i = 0; i < onsetTimes.Length; i++)
        {
            // Step 1: Convert onset time to beat position
            var beatPosition = onsetTimes[i] / secondsPerBeat;

            // Step 2: Quantize to grid
            var quantizedBeat = QuantizeToBeat(beatPosition, _quantizationGrid);

            // Step 3: Calculate duration
            Rational quantizedDuration;
            double actualDurationSeconds;
            if (i < onsetTimes.Length - 1)
            {
                // Duration = gap to next onset
                actualDurationSeconds = onsetTimes[i + 1] - onsetTimes[i];
                var nextBeatPosition = onsetTimes[i + 1] / secondsPerBeat;
                var nextQuantizedBeat = QuantizeToBeat(nextBeatPosition, _quantizationGrid);
                quantizedDuration = nextQuantizedBeat - quantizedBeat;

                // Ensure minimum duration
                if (quantizedDuration < _minNoteDuration)
                {
                    quantizedDuration = _minNoteDuration;
                }
            }
            else
            {
                // Last note: use default duration
                quantizedDuration = _defaultLastNoteDuration;
                actualDurationSeconds = quantizedDuration.ToDouble() * secondsPerBeat;
            }

            // Step 4: Calculate quantization errors
            var quantizedOnsetSeconds = quantizedBeat.ToDouble() * secondsPerBeat;
            var onsetError = TimeSpan.FromSeconds(quantizedOnsetSeconds - onsetTimes[i]);

            var quantizedDurationSeconds = quantizedDuration.ToDouble() * secondsPerBeat;
            var durationError = TimeSpan.FromSeconds(quantizedDurationSeconds - actualDurationSeconds);

            // Step 5: Create raw NoteEvent (preserves original audio timing)
            var rawEvent = new NoteEvent(
                Pitch: new MidiNote(pitches[i]),
                Onset: TimeSpan.FromSeconds(onsetTimes[i]),
                Duration: TimeSpan.FromSeconds(actualDurationSeconds),
                Velocity: new Velocity(0.5f) // Default moderate velocity (0.5 = mf) - could be improved with dynamics analysis
            );

            // Step 6: Create quantization metadata
            var metadata = new QuantizationMetadata(
                Subdivision: subdivision,
                TempoAtOnset: bpm,
                OnsetError: onsetError,
                DurationError: durationError
            );

            // Step 7: Create quantized note event
            notes.Add(new QuantizedNoteEvent(
                rawEvent: rawEvent,
                onsetBeats: quantizedBeat,
                durationBeats: quantizedDuration,
                quantizationMetadata: metadata
            ));
        }

        return notes;
    }

    /// <summary>
    /// Quantizes a beat position to the nearest grid point.
    /// </summary>
    private static Rational QuantizeToBeat(double beatPosition, Rational grid)
    {
        // Round to nearest grid point
        var gridSize = (double)grid.Numerator / grid.Denominator;

        // Convert to Rational
        // We need to express quantized as a fraction
        var gridMultiplier = (int)Math.Round(beatPosition / gridSize);
        var result = Rational.Create(gridMultiplier * grid.Numerator, grid.Denominator);

        return result;
    }
}
