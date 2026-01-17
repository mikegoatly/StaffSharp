using StaffSharp.Audio.Pipeline;

namespace StaffSharp.Audio.Analysis.Pitch;

/// <summary>
/// pYIN (Probabilistic YIN) pitch detection algorithm.
/// An extension of YIN that provides multiple pitch candidates with probabilities
/// and explicit voicing detection, significantly reducing octave errors.
/// Based on Mauch & Dixon (2014) and librosa implementation.
/// </summary>
public sealed class PyinPitchDetector : IPitchDetector
{
    private readonly float _threshold;
    private readonly double _minFrequency;
    private readonly double _maxFrequency;
    private readonly double _boltzmannTemperature;
    private readonly double _betaDist1;
    private readonly double _betaDist2;
    private readonly float _candidateThreshold;

    public PyinPitchDetector(PitchDetectionOptions? options = null)
    {
        options ??= new PitchDetectionOptions();
        options.Validate();

        _minFrequency = options.MinFrequency;
        _maxFrequency = options.MaxFrequency;
        _threshold = options.Threshold;
        _boltzmannTemperature = options.BoltzmannTemperature;
        _betaDist1 = options.BetaDist1;
        _betaDist2 = options.BetaDist2;
        _candidateThreshold = options.CandidateThreshold;
    }

    public PitchDetectionResult DetectPitch(PipelineProgress progress, ReadOnlySpan<float> buffer, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (buffer.Length < 2)
        {
            return default;
        }

        var minPeriod = (int)Math.Max(1, sampleRate / _maxFrequency);
        var maxPeriod = (int)Math.Min(buffer.Length / 2, sampleRate / _minFrequency);

        if (minPeriod >= maxPeriod)
        {
            return default;
        }

        // Step 1: Compute difference function
        var differenceFunction = ComputeDifferenceFunction(buffer, maxPeriod);

        // Step 2: Compute cumulative mean normalized difference (CMND)
        var cmndf = ComputeCumulativeMeanNormalizedDifference(differenceFunction);

        // Step 3: Find all local minima below threshold (not just first)
        var localMinima = FindAllLocalMinima(cmndf, minPeriod, _threshold);

        if (localMinima.Count == 0)
        {
            return default;
        }

        // Step 4: Apply parabolic interpolation to refine periods and sort by period (ascending)
        // Shorter periods (higher frequencies) should be preferred
        var refinedPeriods = localMinima
            .Select(period => (period: ParabolicInterpolation(cmndf, period), cmndfValue: cmndf[period]))
            .OrderBy(x => x.period) // CRITICAL: Sort by period ascending (shortest first)
            .ToList();

        // Step 5: Convert periods to frequencies
        var candidates = refinedPeriods
            .Select(x => (frequency: sampleRate / x.period, cmndfValue: x.cmndfValue))
            .ToList();

        // Step 6: Compute Boltzmann distribution over candidates
        var probabilities = ComputeBoltzmannDistribution(
            candidates.Select(c => c.cmndfValue).ToList(),
            _boltzmannTemperature);

        // Step 7: Create pitch candidates and prune low-probability ones
        var pitchCandidates = candidates
            .Zip(probabilities, (c, p) => new PitchCandidate(c.frequency, (float)p))
            .Where(c => c.Probability >= _candidateThreshold)
            .OrderByDescending(c => c.Probability)
            .ToList();

        if (pitchCandidates.Count == 0)
        {
            return default;
        }

        // Step 8: Compute voicing probability using beta distribution
        var globalMin = localMinima.Min(period => cmndf[period]);
        var voicingProbability = ComputeVoicingProbability(globalMin, _betaDist1, _betaDist2);

        return new PitchDetectionResult(pitchCandidates, voicingProbability);
    }

    /// <summary>
    /// Computes the difference function: d[tau] = sum((x[i] - x[i+tau])^2)
    /// </summary>
    private static float[] ComputeDifferenceFunction(ReadOnlySpan<float> buffer, int maxPeriod)
    {
        var diff = new float[maxPeriod + 1];
        diff[0] = 0; // Tau = 0 is always 0

        for (int tau = 1; tau <= maxPeriod; tau++)
        {
            float sum = 0;
            for (int i = 0; i < buffer.Length - tau; i++)
            {
                var delta = buffer[i] - buffer[i + tau];
                sum += delta * delta;
            }
            diff[tau] = sum;
        }

        return diff;
    }

    /// <summary>
    /// Computes cumulative mean normalized difference:
    /// cmndf[tau] = d[tau] / ((1/tau) * sum(d[1..tau]))
    /// This is the key YIN innovation that reduces harmonic confusion.
    /// </summary>
    private static float[] ComputeCumulativeMeanNormalizedDifference(float[] diff)
    {
        var cmndf = new float[diff.Length];
        cmndf[0] = 1.0f; // By definition

        float cumulativeSum = 0;
        for (int tau = 1; tau < diff.Length; tau++)
        {
            cumulativeSum += diff[tau];
            cmndf[tau] = diff[tau] / (cumulativeSum / tau);
        }

        return cmndf;
    }

    /// <summary>
    /// Finds all local minima in CMNDF that are below the threshold.
    /// Unlike YIN which returns the first minimum, pYIN finds all of them
    /// to create multiple pitch candidates.
    /// </summary>
    private static List<int> FindAllLocalMinima(float[] cmndf, int minPeriod, float threshold)
    {
        var minima = new List<int>();

        for (int tau = minPeriod; tau < cmndf.Length - 1; tau++)
        {
            // Check if this is a local minimum and below threshold
            if (cmndf[tau] < threshold &&
                cmndf[tau] <= cmndf[tau + 1] &&
                (tau == minPeriod || cmndf[tau] <= cmndf[tau - 1]))
            {
                minima.Add(tau);
            }
        }

        return minima;
    }

    /// <summary>
    /// Performs parabolic interpolation around the detected period
    /// for sub-sample frequency accuracy.
    /// </summary>
    private static double ParabolicInterpolation(float[] cmndf, int period)
    {
        if (period == 0 || period >= cmndf.Length - 1)
        {
            return period;
        }

        var alpha = cmndf[period - 1];
        var beta = cmndf[period];
        var gamma = cmndf[period + 1];

        // Parabola vertex formula: x = 0.5 * (alpha - gamma) / (alpha - 2*beta + gamma)
        var denominator = alpha - 2 * beta + gamma;
        if (Math.Abs(denominator) < 1e-10)
        {
            return period;
        }

        var offset = 0.5 * (alpha - gamma) / denominator;
        return period + offset;
    }

    /// <summary>
    /// Computes Boltzmann distribution over pitch candidates.
    /// 
    /// Uses the Boltzmann (Truncated Discrete Exponential) distribution PMF:
    /// f(k) = (1 - exp(-λ)) * exp(-λ*k) / (1 - exp(-λ*N))
    /// 
    /// where k is the candidate position (0-based), λ is the temperature parameter,
    /// and N is the total number of candidates.
    /// 
    /// This creates an exponential preference for earlier candidates (shorter periods/higher frequencies),
    /// which is critical for avoiding subharmonic errors.
    /// 
    /// From librosa: "Smaller periods are weighted more."
    /// </summary>
    /// <param name="cmndfValues">CMNDF values for each candidate (sorted by period ascending).</param>
    /// <param name="lambda">Lambda parameter controlling decay rate.</param>
    /// <returns>Normalized probabilities summing to 1.0.</returns>
    private static List<double> ComputeBoltzmannDistribution(List<float> cmndfValues, double lambda)
    {
        int n = cmndfValues.Count;

        if (n == 0)
        {
            return [];
        }

        if (n == 1)
        {
            return [1.0];
        }

        // Boltzmann PMF: f(k) = (1 - exp(-λ)) * exp(-λ*k) / (1 - exp(-λ*N))
        var numerator = 1.0 - Math.Exp(-lambda);
        var denominator = 1.0 - Math.Exp(-lambda * n);

        if (Math.Abs(denominator) < 1e-10)
        {
            // Degenerate case: uniform distribution
            return Enumerable.Repeat(1.0 / n, n).ToList();
        }

        var probabilities = new List<double>();
        for (int k = 0; k < n; k++)
        {
            // Position-based probability from Boltzmann PMF
            var positionProb = (numerator * Math.Exp(-lambda * k)) / denominator;

            // Weight by CMNDF quality: lower CMNDF = better match
            // Use exp(-cmndf) to convert CMNDF to a quality weight
            var qualityWeight = Math.Exp(-cmndfValues[k]);

            probabilities.Add(positionProb * qualityWeight);
        }

        // Normalize to sum to 1.0
        var sum = probabilities.Sum();
        if (sum > 0)
        {
            probabilities = probabilities.Select(p => p / sum).ToList();
        }

        return probabilities;
    }

    /// <summary>
    /// Computes voicing probability using a beta distribution prior.
    /// This helps distinguish between pitched sounds (high voicing probability)
    /// and unpitched/noisy sounds (low voicing probability).
    /// </summary>
    /// <param name="globalMin">The global minimum CMNDF value.</param>
    /// <param name="alpha">Beta distribution alpha parameter.</param>
    /// <param name="beta">Beta distribution beta parameter.</param>
    /// <returns>Voicing probability [0.0, 1.0].</returns>
    private static float ComputeVoicingProbability(float globalMin, double alpha, double beta)
    {
        // The voicing probability is computed using the beta distribution CDF
        // evaluated at (1 - globalMin). Lower CMNDF values indicate higher voicing probability.

        // For simplicity, we use a heuristic approximation based on the beta distribution mean
        // The exact implementation would use the incomplete beta function, but this approximation
        // works well in practice.

        var x = 1.0 - globalMin; // Convert CMNDF to "goodness" measure
        var mean = alpha / (alpha + beta);

        // Sigmoid-like transformation centered at the distribution mean
        var scaledX = (x - mean) * 10; // Scale factor to create sharper transitions
        var voicingProb = 1.0 / (1.0 + Math.Exp(-scaledX));

        return (float)voicingProb;
    }
}
