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
    private readonly Rational _onsetAlignmentTolerance;

    public PolyphonicQuantizer(QuantizationOptions? options = null)
    {
        options ??= new QuantizationOptions();
        options.Validate();

        _quantizationGrid = options.QuantizationGrid;
        _minNoteDuration = options.MinNoteDuration;
        _onsetAlignmentTolerance = options.OnsetAlignmentTolerance;
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

        // Align onsets of overlapping notes within tolerance
        if (_onsetAlignmentTolerance > Rational.Zero)
        {
            var groups = GroupOverlappingNotes(quantizedNotes, _onsetAlignmentTolerance);

            foreach (var group in groups)
            {
                // Find earliest onset in group
                var targetOnset = group.Min(n => n.OnsetBeats);

                // Update all notes in group to align to earliest onset
                for (int i = 0; i < group.Count; i++)
                {
                    var note = group[i];
                    var onsetDelta = note.OnsetBeats - targetOnset;

                    if (onsetDelta > Rational.Zero)
                    {
                        // Adjust onset and maintain original offset by extending duration
                        var adjustedNote = new QuantizedNoteEvent(
                            rawEvent: note.RawEvent,
                            onsetBeats: targetOnset,
                            durationBeats: note.DurationBeats + onsetDelta,
                            quantizationMetadata: note.QuantizationMetadata,
                            voiceHint: note.VoiceHint,
                            articulation: note.Articulation
                        );

                        // Replace in the main list
                        int originalIndex = quantizedNotes.IndexOf(note);
                        if (originalIndex >= 0)
                        {
                            quantizedNotes[originalIndex] = adjustedNote;
                        }

                        // Update in the group for subsequent iterations
                        group[i] = adjustedNote;
                    }
                }
            }
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

    /// <summary>
    /// Groups notes that start within tolerance and overlap in time.
    /// This is used to align chord notes that may have slightly different onset times.
    /// Only returns groups with overlapping notes - notes that do not overlap with any others are not grouped.
    /// </summary>
    /// <param name="notes">The quantized notes to group.</param>
    /// <param name="tolerance">The maximum onset difference (in beats) for notes to be grouped.</param>
    /// <returns>List of note groups where each group contains notes that should be aligned.</returns>
    private static IEnumerable<List<QuantizedNoteEvent>> GroupOverlappingNotes(
        List<QuantizedNoteEvent> notes,
        Rational tolerance)
    {
        var assigned = new bool[notes.Count];

        var group = new List<QuantizedNoteEvent>();

        for (int i = 0; i < notes.Count; i++)
        {
            if (assigned[i])
            {
                continue;
            }

            assigned[i] = true;

            // Start new group with note i
            group.Add(notes[i]);

            // Find all notes within tolerance that overlap with any note in group
            bool addedAny;
            do
            {
                addedAny = false;
                // Starting from i+1 to avoid re-checking previous notes which are guaranteed to be assigned
                for (int j = i + 1; j < notes.Count; j++)
                {
                    if (assigned[j])
                    {
                        continue;
                    }

                    // Check if j overlaps with any note in group and is within tolerance
                    foreach (var groupNote in group)
                    {
                        var onsetDiff = notes[j].OnsetBeats - groupNote.OnsetBeats;
                        var absDiff = Rational.Abs(onsetDiff);
                        var overlaps = notes[j].OnsetBeats < groupNote.OffsetBeats &&
                                       notes[j].OffsetBeats > groupNote.OnsetBeats;

                        if (absDiff <= tolerance && overlaps)
                        {
                            group.Add(notes[j]);
                            assigned[j] = true;
                            addedAny = true;
                            break;
                        }
                    }
                }
            } while (addedAny);

            // Only yield groups with more than one note
            if (group.Count > 1)
            {
                yield return group.ToList();
            }

            // Clear group for next iteration
            group.Clear();
        }
    }
}
