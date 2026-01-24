using StaffSharp.Notation;

namespace StaffSharp.Performance;

/// <summary>
/// Maps between real time (seconds) and musical time (beats).
/// Handles tempo changes and time signature changes throughout a piece.
/// </summary>
public sealed class TempoMap
{
    private readonly List<TempoMapSegment> _segments;
    private readonly List<TimeSignatureChange> _timeSignatures;

    /// <summary>
    /// Creates a new tempo map with the specified tempo and time signature changes.
    /// </summary>
    /// <param name="tempoChanges">All tempo changes in the piece, must include at least one at beat 0.</param>
    /// <param name="timeSignatures">All time signature changes in the piece, must include at least one at beat 0.</param>
    public TempoMap(
        IEnumerable<TempoChange> tempoChanges,
        IEnumerable<TimeSignatureChange> timeSignatures)
    {
        var sortedTempos = tempoChanges.OrderBy(tc => tc.TimeInBeats).ToList();

        _timeSignatures = timeSignatures.OrderBy(ts => ts.TimeInBeats).ToList();

        ValidateInputs(sortedTempos, _timeSignatures);

        // Pre-calculate the Real Time (seconds) for every tempo change.
        // This effectively "renders" the time map once, so lookups are instant.
        _segments = new List<TempoMapSegment>(sortedTempos.Count);

        double currentSeconds = 0.0;

        for (int i = 0; i < sortedTempos.Count; i++)
        {
            var current = sortedTempos[i];

            // Calculate duration of THIS segment (until the next one starts)
            if (i > 0)
            {
                var prev = sortedTempos[i - 1];
                var beatsInPrevSegment = current.TimeInBeats - prev.TimeInBeats;
                var secondsInPrevSegment = (beatsInPrevSegment.ToDouble() / prev.BeatsPerMinute) * 60.0;
                currentSeconds += secondsInPrevSegment;
            }

            _segments.Add(new TempoMapSegment(
                StartBeat: current.TimeInBeats,
                StartTime: currentSeconds,
                Bpm: current.BeatsPerMinute
            ));
        }

    }

    /// <summary>
    /// Gets the beat position for a specific time in seconds.
    /// (Previously SecondsToBeats)
    /// </summary>
    public double GetBeatAtTime(double seconds)
    {
        var segment = FindSegmentAtTime(seconds);

        var timeOffset = seconds - segment.StartTime;
        var beatOffset = timeOffset * (segment.Bpm / 60.0);

        return segment.StartBeat.ToDouble() + beatOffset;
    }

    /// <summary>
    /// Gets the time in seconds for a specific beat position.
    /// (Previously BeatsToSeconds)
    /// </summary>
    public double GetTimeAtBeat(double beats)
    {
        var segment = FindSegmentAtBeat(beats);

        var beatOffset = beats - segment.StartBeat.ToDouble();
        var timeOffset = beatOffset * (60.0 / segment.Bpm);

        return segment.StartTime + timeOffset;
    }

    /// <summary>
    /// Gets the Tempo (BPM) effective at the given time in seconds.
    /// </summary>
    public double GetTempoAtTime(double seconds)
    {
        return FindSegmentAtTime(seconds).Bpm;
    }

    // Legacy support wrappers if you still use them elsewhere
    public Rational SecondsToBeats(double seconds) => Rational.FromDouble(GetBeatAtTime(seconds));
    public double BeatsToSeconds(Rational beats) => GetTimeAtBeat(beats.ToDouble());

    public IReadOnlyList<TimeSignatureChange> TimeSignatures => _timeSignatures;

    /// <summary>
    /// Gets the measure number and beat within that measure for a given beat position.
    /// </summary>
    /// <param name="beats">Musical time in beats from the start of the piece.</param>
    /// <returns>The measure location (measure number and beat within measure).</returns>
    public MeasureLocation GetMeasureAt(Rational beats)
    {
        int measureNumber = 1;
        Rational currentBeat = Rational.Zero;
        TimeSignatureChange currentTimeSig = _timeSignatures[0];

        foreach (var timeSig in _timeSignatures)
        {
            if (beats < timeSig.TimeInBeats)
            {
                break;
            }

            // Calculate how many measures elapsed in the previous time signature
            if (timeSig.TimeInBeats > currentBeat)
            {
                var beatsInPreviousSection = timeSig.TimeInBeats - currentBeat;
                var beatsPerMeasure = currentTimeSig.TimeSignature.BeatsPerMeasure;
                var measuresInSection = (int)(beatsInPreviousSection / beatsPerMeasure).ToDouble();

                measureNumber += measuresInSection;
                currentBeat = timeSig.TimeInBeats;
            }

            currentTimeSig = timeSig;
        }

        // Calculate position in current time signature
        var beatsFromLastTimeSig = beats - currentBeat;
        var beatsPerMeasureNow = currentTimeSig.TimeSignature.BeatsPerMeasure;
        var additionalMeasures = (int)(beatsFromLastTimeSig / beatsPerMeasureNow).ToDouble();

        measureNumber += additionalMeasures;

        var beatInMeasure = beatsFromLastTimeSig -
            (Rational.Create(additionalMeasures, 1) * beatsPerMeasureNow);

        return new MeasureLocation(measureNumber, beatInMeasure);
    }

    /// <summary>
    /// Gets the tempo (BPM) at a specific musical time.
    /// </summary>
    /// <param name="beats">Musical time in beats.</param>
    /// <returns>The tempo in beats per minute at that time.</returns>
    public double GetTempoAt(Rational beats)
    {
        return FindSegmentAtBeat(beats.ToDouble()).Bpm;
    }

    /// <summary>
    /// Gets the time signature at a specific musical time.
    /// </summary>
    /// <param name="beats">Musical time in beats.</param>
    /// <returns>The time signature at that time.</returns>
    public TimeSignature GetTimeSignatureAt(Rational beats)
    {
        // Find the most recent time signature change before or at this beat
        TimeSignatureChange currentTimeSig = _timeSignatures[0];

        foreach (var change in _timeSignatures)
        {
            if (change.TimeInBeats > beats)
            {
                break;
            }
            currentTimeSig = change;
        }

        return currentTimeSig.TimeSignature;
    }

    private TempoMapSegment FindSegmentAtTime(double seconds)
    {
        // Binary search could be used here for optimization, 
        for (int i = _segments.Count - 1; i >= 0; i--)
        {
            if (seconds >= _segments[i].StartTime)
            {
                return _segments[i];
            }
        }

        return _segments[0];
    }

    private TempoMapSegment FindSegmentAtBeat(double beats)
    {
        for (int i = _segments.Count - 1; i >= 0; i--)
        {
            if (beats >= _segments[i].StartBeat.ToDouble())
            {
                return _segments[i];
            }
        }

        return _segments[0];
    }

    private sealed record TempoMapSegment(Rational StartBeat, double StartTime, double Bpm);

    private static void ValidateInputs(List<TempoChange> tempos, List<TimeSignatureChange> sigs)
    {
        if (tempos.Count == 0 || sigs.Count == 0)
        {
            throw new ArgumentException("Must have at least one tempo and time signature.");
        }

        if (tempos[0].TimeInBeats != Rational.Zero)
        {
            throw new ArgumentException("First tempo must be at beat 0.");
        }

        if (sigs[0].TimeInBeats != Rational.Zero)
        {
            throw new ArgumentException("First time signature must be at beat 0.");
        }
    }
}
