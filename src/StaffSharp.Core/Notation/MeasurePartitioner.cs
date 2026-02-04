using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Core.Notation;

/// <summary>
/// Partitions performance events into measures, splitting notes at barlines with ties and inserting rests.
/// </summary>
public sealed class MeasurePartitioner
{
    private readonly TempoMap _tempoMap;

    public MeasurePartitioner(TempoMap tempoMap)
    {
        _tempoMap = tempoMap ?? throw new ArgumentNullException(nameof(tempoMap));
    }

    /// <summary>
    /// Partitions events by voice into measures, adding ties and rests as needed.
    /// </summary>
    /// <param name="voiceAssignments">Events grouped by voice number.</param>
    /// <returns>Dictionary mapping voice number to list of measures.</returns>
    public IReadOnlyDictionary<int, List<Measure>> PartitionIntoMeasures(
        IReadOnlyDictionary<int, List<VoiceAssignment>> voiceAssignments)
    {
        ArgumentNullException.ThrowIfNull(voiceAssignments);

        var result = new Dictionary<int, List<Measure>>();

        foreach (var (voiceNumber, assignments) in voiceAssignments)
        {
            result[voiceNumber] = PartitionVoiceIntoMeasures(assignments);
        }

        return result;
    }

    private List<Measure> PartitionVoiceIntoMeasures(List<VoiceAssignment> assignments)
    {
        if (assignments.Count == 0)
        {
            return [];
        }

        var measures = new Dictionary<int, List<INotationEvent>>();
        var currentBeat = Rational.Zero;

        // Sort and filter valid assignments
        var validAssignments = assignments.FilterValid().SortByOnset();
        if (validAssignments.Count == 0)
        {
            return [];
        }

        // Get all temporal boundaries (note starts and ends)
        var boundaries = validAssignments.GetTemporalBoundaries();

        // Process each temporal segment
        for (int i = 0; i < boundaries.Count - 1; i++)
        {
            var segmentStart = boundaries[i];
            var segmentEnd = boundaries[i + 1];
            var segmentDuration = segmentEnd - segmentStart;

            // Skip zero-duration segments
            if (segmentDuration == Rational.Zero)
            {
                continue;
            }

            // Add rest if there's a gap before this segment
            if (segmentStart > currentBeat)
            {
                AddRestsForGap(measures, currentBeat, segmentStart);
            }

            // Get notes active during this segment
            var activeNotes = validAssignments.GetActiveNotesAt(segmentStart);

            if (activeNotes.Count > 0)
            {
                // Check if all active notes started at this segment (true chord)
                var allStartHere = activeNotes.All(n => n.Event.OnsetBeats == segmentStart);

                Rational duration;
                if (allStartHere && activeNotes.Count > 1)
                {
                    // Notes started simultaneously - check if durations are similar
                    var minDuration = activeNotes.Min(n => n.Event.DurationBeats);
                    var maxDuration = activeNotes.Max(n => n.Event.DurationBeats);

                    // Allow 25% tolerance for ML detection artifacts
                    const double durationSimilarityThreshold = 1.25;
                    var durationRatio = maxDuration.ToDouble() / minDuration.ToDouble();

                    if (durationRatio <= durationSimilarityThreshold)
                    {
                        // Durations are similar (within tolerance) - treat as true chord
                        // Use minimum duration to handle ML artifacts where chord notes
                        // have slightly different detected durations (e.g., 1.0 vs 1.25 beats)
                        duration = minDuration;
                    }
                    else
                    {
                        // Durations differ significantly - treat as polyphonic overlap
                        // (e.g., sustained bass chord with shorter melody notes)
                        duration = segmentDuration;
                    }
                }
                else
                {
                    // Temporal overlap or single note: use segment duration
                    duration = segmentDuration;
                }

                AddNoteOrChordWithMeasureSplits(measures, activeNotes, segmentStart, duration);
                currentBeat = segmentEnd;
            }

            // If segment has no active notes, don't update currentBeat
            // so the next segment will detect the gap and add a rest
        }

        // Convert dictionary to sorted list of measures
        return [.. measures
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => CreateMeasure(kvp.Key, kvp.Value))];
    }

    private void AddRestsForGap(Dictionary<int, List<INotationEvent>> measures, Rational startBeat, Rational endBeat)
    {
        var currentBeat = startBeat;

        while (currentBeat < endBeat)
        {
            var measureLocation = _tempoMap.GetMeasureAt(currentBeat);
            var measureNumber = measureLocation.MeasureNumber;
            var measureEndBeat = GetMeasureEndBeat(measureNumber);

            var restDuration = endBeat - currentBeat < measureEndBeat - currentBeat
                ? endBeat - currentBeat
                : measureEndBeat - currentBeat;
            var symbolicDuration = restDuration.FromRational();

            if (!measures.TryGetValue(measureNumber, out var measureEvents))
            {
                measureEvents = [];
                measures[measureNumber] = measureEvents;
            }

            measureEvents.Add(new Rest(symbolicDuration));
            currentBeat += restDuration;
        }
    }

    /// <summary>
    /// Gets the starting beat of a given measure number.
    /// </summary>
    /// <param name="measureNumber">The 1-based measure number.</param>
    /// <returns>The beat position at the start of the measure.</returns>
    private Rational GetMeasureStartBeat(int measureNumber)
    {
        // Find the beat where this measure starts by iterating forward
        Rational beat = Rational.Zero;
        int currentMeasure = 1;

        // Iterate until we reach the target measure
        while (currentMeasure < measureNumber)
        {
            var currentTimeSig = _tempoMap.GetTimeSignatureAt(beat);
            beat += currentTimeSig.BeatsPerMeasure;
            currentMeasure++;
        }

        return beat;
    }

    /// <summary>
    /// Gets the ending beat of a given measure number.
    /// </summary>
    /// <param name="measureNumber">The 1-based measure number.</param>
    /// <returns>The beat position at the end of the measure.</returns>
    private Rational GetMeasureEndBeat(int measureNumber)
    {
        // Find where the measure starts
        var measureStartBeat = GetMeasureStartBeat(measureNumber);

        // Get the time signature for this measure and return the end beat
        var measureTimeSig = _tempoMap.GetTimeSignatureAt(measureStartBeat);
        return measureStartBeat + measureTimeSig.BeatsPerMeasure;
    }

    private static Measure CreateMeasure(int measureNumber, List<INotationEvent> events)
    {
        // Calculate the start beat of this measure to get the correct time signature
        //var measureStartBeat = GetMeasureStartBeat(measureNumber);
        //var timeSignature = _tempoMap.GetTimeSignatureAt(measureStartBeat);

        return new Measure(
            number: measureNumber,
            events: events,
            timeSignature: null, // TODO: Only set if changed from previous measure
            lyrics: null
        );
    }

    /// <summary>
    /// Adds a note or chord with the given duration, splitting across measure boundaries if needed.
    /// This method takes a Rational duration and converts each segment to SymbolicDuration,
    /// preserving the full duration across splits.
    /// </summary>
    private void AddNoteOrChordWithMeasureSplits(
        Dictionary<int, List<INotationEvent>> measures,
        List<VoiceAssignment> noteGroup,
        Rational onsetBeats,
        Rational durationBeats)
    {
        var currentBeat = onsetBeats;
        var remainingDuration = durationBeats;
        INotationEvent? previousEvent = null;

        while (remainingDuration > Rational.Zero)
        {
            var measureLocation = _tempoMap.GetMeasureAt(currentBeat);
            var measureNumber = measureLocation.MeasureNumber;
            var measureEndBeat = GetMeasureEndBeat(measureNumber);

            var segmentDuration = remainingDuration < measureEndBeat - currentBeat
                ? remainingDuration
                : measureEndBeat - currentBeat;
            var symbolicDuration = segmentDuration.FromRational();

            // Determine tie marker
            TieMarker? tieMarker = null;
            if (previousEvent != null && remainingDuration > segmentDuration)
            {
                tieMarker = new TieMarker(TieMarkerType.Both); // Middle of a tie chain
            }
            else if (previousEvent != null)
            {
                tieMarker = new TieMarker(TieMarkerType.Stop); // Last note in tie chain
            }
            else if (remainingDuration > segmentDuration)
            {
                tieMarker = new TieMarker(TieMarkerType.Start); // First note in tie chain
            }

            // Create the segment event (note or chord)
            INotationEvent segmentEvent;
            if (noteGroup.Count == 1)
            {
                var note = noteGroup[0].Event;
                segmentEvent = new NotationNote(
                    note.Pitch.ToPitch(),
                    symbolicDuration,
                    note.Velocity,
                    tieMarker,
                    GraceNote: null,
                    Decorations: null
                );
            }
            else
            {
                var pitches = noteGroup
                    .Select(n => n.Event.Pitch.ToPitch())
                    .OrderBy(p => p.ToMidiNote().MidiNumber)
                    .ToList();

                var avgVelocity = new Velocity(
                    (float)noteGroup.Average(n => n.Event.Velocity.Value)
                );

                segmentEvent = new Chord(
                    pitches,
                    symbolicDuration,
                    avgVelocity,
                    tieMarker,
                    graceNote: null,
                    decorations: null
                );
            }

            if (!measures.TryGetValue(measureNumber, out var measureEvents))
            {
                measureEvents = [];
                measures[measureNumber] = measureEvents;
            }

            measureEvents.Add(segmentEvent);

            previousEvent = segmentEvent;
            currentBeat += segmentDuration;
            remainingDuration -= segmentDuration;
        }
    }
}
