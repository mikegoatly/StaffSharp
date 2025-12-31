using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Analysis.Tempo;

/// <summary>
/// Tempo detector based on inter-onset intervals (IOI).
/// Analyzes the time gaps between note onsets to estimate BPM.
/// Returns a TempoMap with a single tempo at beat 0.
/// </summary>
public sealed class InterOnsetIntervalTempoDetector : ITempoDetector
{
    private readonly double _minBpm;
    private readonly double _maxBpm;
    private readonly Notation.TimeSignature _defaultTimeSignature;

    public InterOnsetIntervalTempoDetector(TempoDetectionOptions? options = null)
    {
        options ??= new TempoDetectionOptions();
        options.Validate();

        _minBpm = options.MinBpm;
        _maxBpm = options.MaxBpm;
        _defaultTimeSignature = options.DefaultTimeSignature ?? TimeSignature.CommonTime; // 4/4
    }

    public TempoMap? DetectTempo(ReadOnlySpan<double> onsetTimes)
    {
        if (onsetTimes.Length < 2)
            return null;

        // Step 1: Compute inter-onset intervals
        var intervals = ComputeIntervals(onsetTimes);

        // Step 2: Filter intervals to valid tempo range
        var validIntervals = FilterByTempoRange(intervals, _minBpm, _maxBpm);

        if (validIntervals.Count == 0)
            return null;

        // Step 3: Find predominant interval using histogram clustering
        var predominantInterval = FindPredominantInterval(validIntervals);

        // Step 4: Convert to BPM
        var bpm = 60.0 / predominantInterval;

        // Step 5: Create TempoMap with single tempo at beat 0
        var tempoChanges = new[] { new TempoChange(Rational.Zero, bpm) };
        var timeSignatures = new[] { new TimeSignatureChange(Rational.Zero, _defaultTimeSignature) };

        return new TempoMap(tempoChanges, timeSignatures);
    }

    /// <summary>
    /// Computes time differences between consecutive onsets.
    /// </summary>
    private static List<double> ComputeIntervals(ReadOnlySpan<double> onsetTimes)
    {
        var intervals = new List<double>(onsetTimes.Length - 1);

        for (int i = 1; i < onsetTimes.Length; i++)
        {
            var interval = onsetTimes[i] - onsetTimes[i - 1];
            if (interval > 0)
                intervals.Add(interval);
        }

        return intervals;
    }

    /// <summary>
    /// Filters intervals to those corresponding to valid tempo range.
    /// </summary>
    private static List<double> FilterByTempoRange(List<double> intervals, double minBpm, double maxBpm)
    {
        var minInterval = 60.0 / maxBpm;
        var maxInterval = 60.0 / minBpm;

        return intervals.Where(i => i >= minInterval && i <= maxInterval).ToList();
    }

    /// <summary>
    /// Finds the predominant interval using median-based clustering.
    /// The median is robust to outliers caused by ornaments, grace notes, etc.
    /// </summary>
    private static double FindPredominantInterval(List<double> intervals)
    {
        if (intervals.Count == 0)
            return 0;

        // Sort intervals
        var sorted = intervals.OrderBy(x => x).ToArray();

        // Use median as the predominant interval
        var median = sorted.Length % 2 == 0
            ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2.0
            : sorted[sorted.Length / 2];

        // Refine by averaging intervals close to median (within 15%)
        var tolerance = median * 0.15;
        var clustered = intervals
            .Where(i => Math.Abs(i - median) <= tolerance)
            .ToArray();

        return clustered.Length > 0 ? clustered.Average() : median;
    }
}
