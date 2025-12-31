namespace StaffSharp.Audio.Tests;

using Xunit;

/// <summary>
/// Base class for onset detector tests.
/// Provides common assertion helpers for onset detection results.
/// </summary>
public abstract class OnsetDetectorTestBase
{
    protected const int DefaultSampleRate = 44100;

    /// <summary>
    /// Asserts that an onset was detected near the expected time.
    /// </summary>
    protected static void AssertOnsetNear(double[] onsets, double expectedTime, double toleranceSeconds = 0.05)
    {
        var hasOnsetNearby = onsets.Any(onset => Math.Abs(onset - expectedTime) < toleranceSeconds);
        Assert.True(hasOnsetNearby,
            $"No onset detected near {expectedTime:F3}s (tolerance: ±{toleranceSeconds:F3}s). Detected onsets: {string.Join(", ", onsets.Select(o => $"{o:F3}s"))}");
    }

    /// <summary>
    /// Asserts that all expected onsets were detected.
    /// </summary>
    protected static void AssertAllOnsetsDetected(double[] onsets, double[] expectedTimes, double toleranceSeconds = 0.1)
    {
        foreach (var expectedTime in expectedTimes)
        {
            AssertOnsetNear(onsets, expectedTime, toleranceSeconds);
        }
    }

    /// <summary>
    /// Asserts that at least a minimum number of onsets were detected.
    /// </summary>
    protected static void AssertMinimumOnsets(double[] onsets, int minimumCount)
    {
        Assert.True(onsets.Length >= minimumCount,
            $"Expected at least {minimumCount} onsets, but only {onsets.Length} were detected");
    }

    /// <summary>
    /// Asserts that no onsets are closer together than the minimum interval.
    /// </summary>
    protected static void AssertMinimumInterval(double[] onsets, double minInterval, double tolerancePercent = 0.1)
    {
        for (int i = 1; i < onsets.Length; i++)
        {
            var gap = onsets[i] - onsets[i - 1];
            var threshold = minInterval * (1.0 - tolerancePercent); // Allow some tolerance
            Assert.True(gap >= threshold,
                $"Onsets at {onsets[i-1]:F3}s and {onsets[i]:F3}s are too close ({gap:F3}s < {minInterval}s with {tolerancePercent*100}% tolerance)");
        }
    }

    /// <summary>
    /// Asserts that no onsets were detected (or very few).
    /// </summary>
    protected static void AssertFewOrNoOnsets(double[] onsets, int maxAllowed = 0)
    {
        Assert.True(onsets.Length <= maxAllowed,
            $"Expected at most {maxAllowed} onsets, but {onsets.Length} were detected");
    }
}
