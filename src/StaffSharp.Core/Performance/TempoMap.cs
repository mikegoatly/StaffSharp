using StaffSharp.Notation;

namespace StaffSharp.Performance;

/// <summary>
/// Maps between real time (seconds) and musical time (beats).
/// Handles tempo changes and time signature changes throughout a piece.
/// </summary>
public sealed class TempoMap
{
    private readonly List<TempoChange> _tempoChanges;
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
        _tempoChanges = tempoChanges.OrderBy(tc => tc.TimeInBeats).ToList();
        _timeSignatures = timeSignatures.OrderBy(ts => ts.TimeInBeats).ToList();

        if (_tempoChanges.Count == 0)
        {
            throw new ArgumentException(
                "TempoMap must have at least one tempo change",
                nameof(tempoChanges));
        }

        if (_timeSignatures.Count == 0)
        {
            throw new ArgumentException(
                "TempoMap must have at least one time signature",
                nameof(timeSignatures));
        }

        if (_tempoChanges[0].TimeInBeats != Rational.Zero)
        {
            throw new ArgumentException(
                "First tempo change must be at beat 0",
                nameof(tempoChanges));
        }

        if (_timeSignatures[0].TimeInBeats != Rational.Zero)
        {
            throw new ArgumentException(
                "First time signature must be at beat 0",
                nameof(timeSignatures));
        }
    }

    /// <summary>
    /// Gets all tempo changes in the piece, sorted by time.
    /// </summary>
    public IReadOnlyList<TempoChange> TempoChanges => _tempoChanges;

    /// <summary>
    /// Gets all time signature changes in the piece, sorted by time.
    /// </summary>
    public IReadOnlyList<TimeSignatureChange> TimeSignatures => _timeSignatures;

    /// <summary>
    /// Converts musical time (beats) to real time (seconds).
    /// </summary>
    /// <param name="beats">Musical time in beats from the start of the piece.</param>
    /// <returns>Real time in seconds.</returns>
    public double BeatsToSeconds(Rational beats)
    {
        double seconds = 0.0;
        Rational currentBeat = Rational.Zero;

        for (int i = 0; i < _tempoChanges.Count; i++)
        {
            var change = _tempoChanges[i];
            var nextChange = i + 1 < _tempoChanges.Count
                ? _tempoChanges[i + 1]
                : null;

            var segmentStart = change.TimeInBeats;
            var segmentEnd = nextChange?.TimeInBeats ?? beats;

            if (beats <= segmentStart)
            {
                break;
            }

            // Calculate how many beats in this tempo segment
            var beatsInSegment = beats < segmentEnd
                ? beats - segmentStart
                : segmentEnd - segmentStart;

            // Convert beats to seconds: seconds = beats / (BPM / 60)
            var secondsInSegment = (beatsInSegment.ToDouble() / change.BeatsPerMinute) * 60.0;
            seconds += secondsInSegment;

            currentBeat = segmentEnd;
            if (beats <= segmentEnd)
            {
                break;
            }
        }

        return seconds;
    }

    /// <summary>
    /// Converts real time (seconds) to musical time (beats).
    /// </summary>
    /// <param name="seconds">Real time in seconds from the start of the piece.</param>
    /// <returns>Musical time in beats.</returns>
    public Rational SecondsToBeats(double seconds)
    {
        double currentSeconds = 0.0;
        Rational currentBeats = Rational.Zero;

        for (int i = 0; i < _tempoChanges.Count; i++)
        {
            var change = _tempoChanges[i];
            var nextChange = i + 1 < _tempoChanges.Count
                ? _tempoChanges[i + 1]
                : null;

            // Calculate how many seconds this tempo segment lasts
            double segmentSeconds;
            if (nextChange != null)
            {
                var beatsInSegment = nextChange.TimeInBeats - change.TimeInBeats;
                segmentSeconds = (beatsInSegment.ToDouble() / change.BeatsPerMinute) * 60.0;
            }
            else
            {
                // Last tempo segment extends to the requested time
                segmentSeconds = seconds - currentSeconds;
            }

            if (currentSeconds + segmentSeconds >= seconds)
            {
                // The requested time is within this segment
                var remainingSeconds = seconds - currentSeconds;
                var beatsPerSecond = change.BeatsPerMinute / 60.0;
                var remainingBeats = remainingSeconds * beatsPerSecond;

                return currentBeats + Rational.FromDouble(remainingBeats);
            }

            // Move to next segment
            currentSeconds += segmentSeconds;
            if (nextChange != null)
            {
                currentBeats = nextChange.TimeInBeats;
            }
        }

        // Should not reach here, but return currentBeats as fallback
        return currentBeats;
    }

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
                var measuresInSection = (int)((beatsInPreviousSection / beatsPerMeasure).ToDouble());

                measureNumber += measuresInSection;
                currentBeat = timeSig.TimeInBeats;
            }

            currentTimeSig = timeSig;
        }

        // Calculate position in current time signature
        var beatsFromLastTimeSig = beats - currentBeat;
        var beatsPerMeasureNow = currentTimeSig.TimeSignature.BeatsPerMeasure;
        var additionalMeasures = (int)((beatsFromLastTimeSig / beatsPerMeasureNow).ToDouble());

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
        // Find the most recent tempo change before or at this beat
        TempoChange currentTempo = _tempoChanges[0];

        foreach (var change in _tempoChanges)
        {
            if (change.TimeInBeats > beats)
            {
                break;
            }
            currentTempo = change;
        }

        return currentTempo.BeatsPerMinute;
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
}
