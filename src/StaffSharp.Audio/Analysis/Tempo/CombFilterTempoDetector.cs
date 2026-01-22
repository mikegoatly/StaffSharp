using StaffSharp.Audio.Pipeline;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Analysis.Tempo;

/// <summary>
/// A robust tempo detector using Comb Filter banks and All-Pairs Inter-Onset Intervals.
///
/// Algorithm:
/// 1. Calculates All-Pairs IOIs (Inter-Onset Intervals) to find periodicity across rests and syncopation.
/// 2. Clusters intervals using sorted-neighbor grouping (avoiding histogram binning artifacts).
/// 3. Scores candidates using a Resonator/Comb Filter with Phase Alignment.
/// 4. Applies perceptual weighting to favor standard human tempos.
/// </summary>
public sealed class CombFilterTempoDetector : ITempoDetector
{
    private readonly TempoDetectionOptions _options;

    /// <summary>
    /// Minimum valid interval derived from maximum BPM.
    /// </summary>
    private double MinValidInterval => 60.0 / _options.MaxBpm;

    /// <summary>
    /// Maximum valid interval derived from minimum BPM.
    /// </summary>
    private double MaxValidInterval => 60.0 / _options.MinBpm;

    public CombFilterTempoDetector(TempoDetectionOptions? options = null)
    {
        _options = options ?? new TempoDetectionOptions();
        _options.Validate();
    }

    public IReadOnlyList<TempoChange> DetectTempo(
        PipelineProgress progress,
        ReadOnlySpan<double> onsetTimes)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (onsetTimes.Length < 2)
        {
            throw new ArgumentException(
                "At least two onset times are required to detect tempo.",
                nameof(onsetTimes));
        }

        // STEP 1: Compute All-Pairs Intervals
        // This looks at distances between non-adjacent notes to handle syncopation.
        var rawIntervals = ComputeAllPairsIntervals(onsetTimes, _options.PairwiseWindow);

        if (rawIntervals.Count == 0)
        {
            throw new InvalidOperationException(
                "No valid inter-onset intervals found within the specified tempo range.");
        }

        progress.EmitDiagnostics("Raw intervals found", rawIntervals.Count);

        // STEP 2: Cluster Intervals (Sorted Neighbor approach)
        // Finds the "Center of Mass" for common durations without histogram bin edges.
        var clusters = ClusterIntervals(rawIntervals);
        progress.EmitDiagnostics("Dominant clusters", clusters.Count);

        if (clusters.Count == 0)
        {
            throw new InvalidOperationException("No interval clusters found.");
        }

        // STEP 3: Comb Filter Scoring (Grid Fitting)
        // Tests candidates against the onsets to find Phase and best fit.
        var bestResult = FindBestTempo(onsetTimes, clusters);

        progress.EmitDiagnostics("Detected BPM", bestResult.Bpm);
        progress.EmitDiagnostics("Phase offset (s)", bestResult.Phase);
        progress.EmitDiagnostics("Confidence score", bestResult.Score);

        return [new TempoChange(Rational.Zero, bestResult.Bpm)];
    }

    /// <summary>
    /// Computes intervals between every note and its next 'windowSize' neighbors.
    /// This captures periodicities that span syncopated notes or rests.
    /// </summary>
    private List<double> ComputeAllPairsIntervals(ReadOnlySpan<double> onsets, int windowSize)
    {
        var intervals = new List<double>(onsets.Length * windowSize);

        for (int i = 0; i < onsets.Length; i++)
        {
            // Look ahead up to 'windowSize' neighbors
            int maxJ = Math.Min(i + windowSize, onsets.Length);
            for (int j = i + 1; j < maxJ; j++)
            {
                double diff = onsets[j] - onsets[i];

                if (diff >= MinValidInterval && diff <= MaxValidInterval)
                {
                    intervals.Add(diff);
                }
            }
        }
        return intervals;
    }

    /// <summary>
    /// Groups intervals using 1D single-linkage clustering on sorted data.
    /// Consecutive values within tolerance are grouped together.
    /// Solves the "Histogram Bin Split" issue where 120.0 and 120.1 BPM
    /// would land in different bins.
    /// </summary>
    private List<(double Interval, double Strength)> ClusterIntervals(List<double> intervals)
    {
        if (intervals.Count == 0)
        {
            return [];
        }

        // Sort is crucial for O(N) linear clustering
        intervals.Sort();

        var clusters = new List<(double Interval, double Strength)>();

        double currentSum = intervals[0];
        int currentCount = 1;

        // Single-linkage clustering: compare each value to the previous value
        for (int i = 1; i < intervals.Count; i++)
        {
            double val = intervals[i];
            double prevVal = intervals[i - 1];

            // If within tolerance of the previous value, add to current cluster
            if (val - prevVal < _options.ClusterToleranceSeconds)
            {
                currentSum += val;
                currentCount++;
            }
            else
            {
                // Finalize previous cluster
                clusters.Add((currentSum / currentCount, (double)currentCount));

                // Start new cluster
                currentSum = val;
                currentCount = 1;
            }
        }

        // Add final cluster
        clusters.Add((currentSum / currentCount, (double)currentCount));

        // Return top 10 strongest clusters
        return clusters
            .OrderByDescending(c => c.Strength)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Orchestrates the Comb Filter scoring and Perceptual Weighting.
    /// Tests different harmonic interpretations (0.5x, 1x, 2x) to resolve
    /// whether the dominant interval is a half-beat, full beat, or two beats.
    /// </summary>
    private (double Bpm, double Phase, double Score) FindBestTempo(
        ReadOnlySpan<double> onsets,
        List<(double Interval, double Strength)> clusters)
    {
        double bestScore = -1.0;
        double bestBpm = 120.0;
        double bestPhase = 0.0;

        // Check harmonics: The cluster might be 1/2 beat or 2 beats.
        double[] multipliers = [1.0, 0.5, 2.0];

        // Compute max strength once for cluster boost calculation
        double maxClusterStrength = clusters[0].Strength;

        foreach (var cluster in clusters)
        {
            foreach (var mult in multipliers)
            {
                double interval = cluster.Interval * mult;
                double bpm = 60.0 / interval;

                // Range check
                if (bpm < _options.MinBpm || bpm > _options.MaxBpm)
                {
                    continue;
                }

                // A. Comb Filter Score (How well does the grid fit?)
                (double fitScore, double phase) = ScoreCandidate(onsets, interval);

                // B. Perceptual Weighting (Log-Gaussian bias toward ~110 BPM)
                double weight = GetPerceptualWeight(bpm);
                double weightedScore = fitScore * weight;

                // C. Cluster Boost
                // Prefer intervals that appeared frequently in the raw data
                double clusterBoost = 1.0 + (cluster.Strength / maxClusterStrength * 0.1);
                weightedScore *= clusterBoost;

                if (weightedScore > bestScore)
                {
                    bestScore = weightedScore;
                    bestBpm = bpm;
                    bestPhase = phase;
                }
            }
        }

        return (bestBpm, bestPhase, bestScore);
    }

    /// <summary>
    /// Generates a theoretical grid at 'interval' and scans phases to find best alignment.
    /// Returns the match score and the optimal phase offset.
    /// </summary>
    private (double Score, double Phase) ScoreCandidate(ReadOnlySpan<double> onsets, double interval)
    {
        // Adaptive tolerance: scales with tempo (5% of the beat duration)
        // Faster songs get tighter tolerance, slower songs get looser tolerance
        double tolerance = interval * _options.ToleranceRatio;

        // Scan resolution: Check 20 phase positions per interval
        double stepSize = interval / 20.0;

        double maxMatchedEnergy = 0.0;
        double bestPhase = 0.0;

        // Phase Search: slide the grid to find where it "locks" to the onsets
        for (double phase = 0; phase < interval; phase += stepSize)
        {
            double currentMatchedEnergy = 0.0;

            foreach (var onset in onsets)
            {
                // Calculate distance to nearest grid line
                // Grid lines are at: phase + (N * interval) for integer N
                double distFromPhase = onset - phase;
                double cycles = distFromPhase / interval;
                double nearestCycle = Math.Round(cycles);

                // Absolute timing error in seconds
                double error = Math.Abs(cycles - nearestCycle) * interval;

                // Gaussian Scoring: High score if error is close to 0
                // exp(-x^2 / 2*sigma^2)
                double weight = Math.Exp(-(error * error) / (2 * tolerance * tolerance));

                currentMatchedEnergy += weight;
            }

            if (currentMatchedEnergy > maxMatchedEnergy)
            {
                maxMatchedEnergy = currentMatchedEnergy;
                bestPhase = phase;
            }
        }

        // Normalize score by total onsets (range: 0.0 to 1.0)
        return (maxMatchedEnergy / onsets.Length, bestPhase);
    }

    /// <summary>
    /// Calculates Log-Gaussian weight to favor "human" tempos around 110 BPM.
    /// This helps resolve the "octave error" where the algorithm might be ambiguous
    /// between, e.g., 60 BPM and 120 BPM.
    ///
    /// The weight is clamped to [0.3, 1.0] to avoid completely eliminating
    /// valid tempos that are far from the target but have strong comb filter scores.
    /// </summary>
    private double GetPerceptualWeight(double bpm)
    {
        if (bpm <= 0)
        {
            return 0.0;
        }

        // Work in log2 space for perceptual scaling
        double logBpm = Math.Log2(bpm);
        double logTarget = Math.Log2(_options.TargetBpm);

        // Convert linear BPM width to log-space FWHM
        // FWHM spans from (Target - Width/2) to (Target + Width/2)
        double upperBound = _options.TargetBpm + _options.WidthBpm / 2.0;
        double lowerBound = Math.Max(1.0, _options.TargetBpm - _options.WidthBpm / 2.0);
        double logFWHM = Math.Log2(upperBound / lowerBound);

        // Convert FWHM to sigma: sigma = FWHM / 2.355
        double sigma = logFWHM / 2.355;

        double exponent = -Math.Pow(logBpm - logTarget, 2) / (2 * sigma * sigma);
        double weight = Math.Exp(exponent);

        // Clamp weight to [0.7, 1.0] so it acts as a gentle bias, not a hard constraint
        // This allows the comb filter score to dominate when there's a clear fit
        // The narrow range (1.4x) prevents perceptual bias from overriding strong fits
        return Math.Max(0.7, weight);
    }
}
