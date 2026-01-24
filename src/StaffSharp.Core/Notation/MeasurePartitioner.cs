using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Core.Notation;

/// <summary>
/// Partitions performance events into measures, splitting notes at barlines with ties and inserting rests.
/// </summary>
public sealed class MeasurePartitioner
{
    private readonly TempoMap _tempoMap;
    private readonly NotationOptions _options;

    public MeasurePartitioner(TempoMap tempoMap, NotationOptions options)
    {
        _tempoMap = tempoMap ?? throw new ArgumentNullException(nameof(tempoMap));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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

        foreach (var assignment in assignments.OrderBy(a => a.Event.OnsetBeats))
        {
            var evt = assignment.Event;
            var onsetBeats = evt.OnsetBeats;
            var durationBeats = evt.DurationBeats;

            if (durationBeats == Rational.Zero)
            {
                continue; // Skip zero-duration events
            }

            // Add rest if there's a gap
            if (onsetBeats > currentBeat)
            {
                AddRestsForGap(measures, currentBeat, onsetBeats);
            }

            // Add the note, potentially splitting across measures
            AddNoteWithMeasureSplits(measures, evt, onsetBeats, durationBeats);

            currentBeat = onsetBeats + durationBeats;
        }

        // Convert dictionary to sorted list of measures
        return measures
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => CreateMeasure(kvp.Key, kvp.Value))
            .ToList();
    }

    private void AddRestsForGap(Dictionary<int, List<INotationEvent>> measures, Rational startBeat, Rational endBeat)
    {
        var currentBeat = startBeat;

        while (currentBeat < endBeat)
        {
            var measureLocation = _tempoMap.GetMeasureAt(currentBeat);
            var measureNumber = measureLocation.MeasureNumber;
            var timeSignature = _tempoMap.GetTimeSignatureAt(currentBeat);
            var measureEndBeat = GetMeasureEndBeat(measureNumber, timeSignature);

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

    private void AddNoteWithMeasureSplits(
        Dictionary<int, List<INotationEvent>> measures,
        IPerformanceEvent performanceEvent,
        Rational onsetBeats,
        Rational durationBeats)
    {
        // Check if this is an unpitched event (rest)
        // MIDI note -1 is the sentinel value for unpitched/rest
        if (performanceEvent.Pitch.MidiNumber < 0)
        {
            AddRestWithMeasureSplits(measures, onsetBeats, durationBeats);
            return;
        }
        
        var currentBeat = onsetBeats;
        var remainingDuration = durationBeats;
        NotationNote? previousNote = null;

        while (remainingDuration > Rational.Zero)
        {
            var measureLocation = _tempoMap.GetMeasureAt(currentBeat);
            var measureNumber = measureLocation.MeasureNumber;
            var timeSignature = _tempoMap.GetTimeSignatureAt(currentBeat);
            var measureEndBeat = GetMeasureEndBeat(measureNumber, timeSignature);

            var segmentDuration = remainingDuration < measureEndBeat - currentBeat 
                ? remainingDuration 
                : measureEndBeat - currentBeat;
            var symbolicDuration = segmentDuration.FromRational();

            // Determine tie marker
            TieMarker? tieMarker = null;
            if (previousNote != null && remainingDuration > segmentDuration)
            {
                tieMarker = new TieMarker(TieMarkerType.Both); // Middle of a tie chain
            }
            else if (previousNote != null)
            {
                tieMarker = new TieMarker(TieMarkerType.Stop); // Last note in tie chain
            }
            else if (remainingDuration > segmentDuration)
            {
                tieMarker = new TieMarker(TieMarkerType.Start); // First note in tie chain
            }

            var pitch = performanceEvent.Pitch.ToPitch();
            var velocity = performanceEvent.Velocity;

            var note = new NotationNote(
                pitch,
                symbolicDuration,
                velocity,
                tieMarker,
                GraceNote: null,
                Decorations: null // IPerformanceEvent base interface doesn't include articulation data
            );

            if (!measures.TryGetValue(measureNumber, out var measureEvents))
            {
                measureEvents = [];
                measures[measureNumber] = measureEvents;
            }

            measureEvents.Add(note);

            previousNote = note;
            currentBeat += segmentDuration;
            remainingDuration -= segmentDuration;
        }
    }
    
    /// <summary>
    /// Adds a rest, potentially splitting across measure boundaries.
    /// </summary>
    private void AddRestWithMeasureSplits(
        Dictionary<int, List<INotationEvent>> measures,
        Rational onsetBeats,
        Rational durationBeats)
    {
        var currentBeat = onsetBeats;
        var remainingDuration = durationBeats;

        while (remainingDuration > Rational.Zero)
        {
            var measureLocation = _tempoMap.GetMeasureAt(currentBeat);
            var measureNumber = measureLocation.MeasureNumber;
            var timeSignature = _tempoMap.GetTimeSignatureAt(currentBeat);
            var measureEndBeat = GetMeasureEndBeat(measureNumber, timeSignature);

            var segmentDuration = remainingDuration < measureEndBeat - currentBeat 
                ? remainingDuration 
                : measureEndBeat - currentBeat;
            var symbolicDuration = segmentDuration.FromRational();

            if (!measures.TryGetValue(measureNumber, out var measureEvents))
            {
                measureEvents = [];
                measures[measureNumber] = measureEvents;
            }

            measureEvents.Add(new Rest(symbolicDuration));

            currentBeat += segmentDuration;
            remainingDuration -= segmentDuration;
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
    /// <param name="timeSignature">Unused parameter (kept for compatibility).</param>
    /// <returns>The beat position at the end of the measure.</returns>
    private Rational GetMeasureEndBeat(int measureNumber, TimeSignature timeSignature)
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
}
