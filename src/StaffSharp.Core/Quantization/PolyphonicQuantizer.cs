using StaffSharp.Performance;

namespace StaffSharp.Quantization;

/// <summary>
/// Simple polyphonic quantizer for note events with known durations.
/// Snaps both onsets and offsets to rhythmic grid, preserving polyphony.
/// </summary>
public sealed class PolyphonicQuantizer : IPolyphonicQuantizer
{
    private readonly Rational _quantizationGrid;
    private readonly Rational _minNoteDuration;

    public PolyphonicQuantizer(QuantizationOptions? options = null)
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

        if (notes.Count == 0) return (Array.Empty<QuantizedNoteEvent>(), tempoMap);

        var quantizedNotes = new List<QuantizedNoteEvent>(notes.Count);
        var subdivision = _quantizationGrid.Denominator;

        var gridSize = (double)_quantizationGrid.Numerator / _quantizationGrid.Denominator;
        foreach (var note in notes)
        {
            // Convert Time to Beats using the Tempo Map
            double rawOnsetBeats = tempoMap.GetBeatAtTime(note.Onset.TotalSeconds);
            double rawOffsetBeats = tempoMap.GetBeatAtTime(note.Offset.TotalSeconds);

            // Quantize Onset (Snap to nearest grid point)
            Rational targetOnset = SnapToGrid(rawOnsetBeats, _quantizationGrid, gridSize);

            // Quantize Offset (Snap end of note to grid)
            // We calculate where the note SHOULD end based on the grid
            Rational targetOffset = SnapToGrid(rawOffsetBeats, _quantizationGrid, gridSize);

            // Ensure Minimum Duration Logic
            if ((targetOffset - targetOnset) < _minNoteDuration)
            {
                targetOffset = targetOnset + _minNoteDuration;
            }

            // Final Duration Calculation
            Rational finalDuration = targetOffset - targetOnset;

            // Calculate Real-World Time (Seconds) based on new Beat positions
            // This handles tempo changes correctly for the output
            double newOnsetSeconds = tempoMap.GetTimeAtBeat(targetOnset.ToDouble());
            double newDurationSeconds = tempoMap.GetTimeAtBeat(targetOffset.ToDouble()) - newOnsetSeconds;

            var metadata = new QuantizationMetadata(
                Subdivision: subdivision,
                TempoAtOnset: tempoMap.GetTempoAtTime(note.Onset.TotalSeconds),
                OnsetError: TimeSpan.FromSeconds(newOnsetSeconds - note.Onset.TotalSeconds),
                DurationError: TimeSpan.FromSeconds(newDurationSeconds - note.Duration.TotalSeconds)
            );

            quantizedNotes.Add(new QuantizedNoteEvent(
                rawEvent: note,
                onsetBeats: targetOnset,
                durationBeats: finalDuration,
                quantizationMetadata: metadata
            ));
        }

        return (quantizedNotes, tempoMap);
    }

    /// <summary>
    /// Snaps a beat position to the nearest grid point.
    /// </summary>
    /// <param name="value">The beat position to snap.</param>
    /// <param name="grid">The quantization grid size.</param>
    /// <returns>The snapped beat position.</returns>
    private static Rational SnapToGrid(double value, Rational grid, double gridSize)
    {
        int gridIndex = (int)Math.Round(value / gridSize);
        return Rational.Create(gridIndex * grid.Numerator, grid.Denominator);
    }
}
