using StaffSharp;
using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Analysis.Quantization;

/// <summary>
/// Simple rhythm quantizer for Phase 1.
/// Snaps onsets to a quantization grid and infers durations from inter-onset intervals.
/// Assumes single tempo and time signature (Phase 1 constraint).
/// </summary>
public sealed class SimpleQuantizer : IQuantizer
{
    private readonly Rational _quantizationGrid;
    private readonly Rational _defaultLastNoteDuration;
    private readonly Rational _minNoteDuration;

    /// <summary>
    /// Creates a new simple quantizer.
    /// </summary>
    /// <param name="quantizationGrid">Quantization grid in beats (e.g., 1/4 for 16th notes in 4/4). Default: 1/4 (16th notes).</param>
    /// <param name="defaultLastNoteDuration">Default duration for the last note in beats. Default: 1 (quarter note).</param>
    /// <param name="minNoteDuration">Minimum note duration in beats. Notes shorter than this are extended. Default: 1/8 (32nd note).</param>
    public SimpleQuantizer(
        Rational? quantizationGrid = null,
        Rational? defaultLastNoteDuration = null,
        Rational? minNoteDuration = null)
    {
        _quantizationGrid = quantizationGrid ?? Rational.Create(1, 4); // 16th notes
        _defaultLastNoteDuration = defaultLastNoteDuration ?? Rational.Create(1, 1); // Quarter note
        _minNoteDuration = minNoteDuration ?? Rational.Create(1, 8); // 32nd note

        if (_quantizationGrid <= Rational.Zero)
            throw new ArgumentException("Quantization grid must be positive", nameof(quantizationGrid));
        if (_defaultLastNoteDuration <= Rational.Zero)
            throw new ArgumentException("Default last note duration must be positive", nameof(defaultLastNoteDuration));
        if (_minNoteDuration <= Rational.Zero)
            throw new ArgumentException("Minimum note duration must be positive", nameof(minNoteDuration));
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

        // Phase 1: Assume single tempo and time signature
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
                    quantizedDuration = _minNoteDuration;
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
                Velocity: new Velocity(0.5f) // Default moderate velocity (0.5 = mf) for Phase 1
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
                quantizationMetadata: metadata,
                voiceHint: null, // Monophonic for Phase 1
                articulation: ArticulationFlags.None // No articulation detection in Phase 1
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
        var quantized = Math.Round(beatPosition / gridSize) * gridSize;

        // Convert to Rational
        // We need to express quantized as a fraction
        // quantized = (round(beatPosition / gridSize)) * gridSize
        // = (round(beatPosition / gridSize)) * (grid.Numerator / grid.Denominator)

        var gridMultiplier = (int)Math.Round(beatPosition / gridSize);
        var result = Rational.Create(gridMultiplier * grid.Numerator, grid.Denominator);

        return result;
    }
}
