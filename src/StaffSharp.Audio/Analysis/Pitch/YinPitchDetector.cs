namespace StaffSharp.Audio.Analysis.Pitch;

/// <summary>
/// YIN pitch detection algorithm (de Cheveigné & Kawahara, 2002).
/// Superior harmonic rejection compared to autocorrelation,
/// especially effective for piano, guitar, and voice.
/// </summary>
public sealed class YinPitchDetector : IPitchDetector
{
    private readonly float _threshold;
    private readonly double _minFrequency;
    private readonly double _maxFrequency;

    public YinPitchDetector(PitchDetectionOptions? options = null)
    {
        options ??= new PitchDetectionOptions();
        options.Validate();

        _minFrequency = options.MinFrequency;
        _maxFrequency = options.MaxFrequency;
        _threshold = options.Threshold;
    }

    public PitchDetectionResult DetectPitch(ReadOnlySpan<float> buffer, int sampleRate)
    {
        if (buffer.Length < 2)
            return default;

        var minPeriod = (int)Math.Max(1, sampleRate / _maxFrequency);
        var maxPeriod = (int)Math.Min(buffer.Length / 2, sampleRate / _minFrequency);

        if (minPeriod >= maxPeriod)
            return default;

        // Step 1: Compute difference function
        var differenceFunction = ComputeDifferenceFunction(buffer, maxPeriod);

        // Step 2: Compute cumulative mean normalized difference (CMND)
        var cmndf = ComputeCumulativeMeanNormalizedDifference(differenceFunction);

        // Step 3: Find first minimum below threshold
        var period = FindFirstMinimumBelowThreshold(cmndf, minPeriod, _threshold);

        if (period == -1)
            return default;

        // Step 4: Parabolic interpolation for sub-sample accuracy
        var refinedPeriod = ParabolicInterpolation(cmndf, period);

        // Compute frequency and confidence
        var frequency = sampleRate / refinedPeriod;
        var confidence = 1.0f - cmndf[period];

        return new PitchDetectionResult(frequency, confidence);
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
    /// Finds the first local minimum in CMNDF that is below the threshold.
    /// Returns -1 if no such minimum exists.
    /// </summary>
    private static int FindFirstMinimumBelowThreshold(float[] cmndf, int minPeriod, float threshold)
    {
        for (int tau = minPeriod; tau < cmndf.Length - 1; tau++)
        {
            // Check if this is a local minimum and below threshold
            if (cmndf[tau] < threshold && cmndf[tau] < cmndf[tau + 1])
            {
                // Confirm it's a true minimum (not just a plateau)
                if (tau == minPeriod || cmndf[tau] < cmndf[tau - 1])
                    return tau;
            }
        }

        return -1;
    }

    /// <summary>
    /// Performs parabolic interpolation around the detected period
    /// for sub-sample frequency accuracy.
    /// </summary>
    private static double ParabolicInterpolation(float[] cmndf, int period)
    {
        if (period == 0 || period >= cmndf.Length - 1)
            return period;

        var alpha = cmndf[period - 1];
        var beta = cmndf[period];
        var gamma = cmndf[period + 1];

        // Parabola vertex formula: x = 0.5 * (alpha - gamma) / (alpha - 2*beta + gamma)
        var denominator = alpha - 2 * beta + gamma;
        if (Math.Abs(denominator) < 1e-10)
            return period;

        var offset = 0.5 * (alpha - gamma) / denominator;
        return period + offset;
    }
}
