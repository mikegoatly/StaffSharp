namespace StaffSharp.Audio.Tests;

using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.TestHelpers.Builders;
using Xunit;

/// <summary>
/// Base class for pitch detector tests.
/// Provides common assertion helpers for pitch detection results.
/// </summary>
public abstract class PitchDetectorTestBase
{
    protected const int DefaultSampleRate = 44100;

    /// <summary>
    /// Asserts that pitch detection result matches expected frequency within tolerance.
    /// </summary>
    protected static void AssertPitchFrequency(
        PitchDetectionResult result,
        double expectedFrequency,
        double toleranceHz = 5.0)
    {
        Assert.True(result.IsPitched, $"Expected pitch to be detected at {expectedFrequency} Hz, but no pitch was detected");
        var error = Math.Abs(result.FrequencyHz - expectedFrequency);
        Assert.True(
            error <= toleranceHz,
            $"Frequency error {error:F2} Hz exceeds tolerance {toleranceHz} Hz. Expected: {expectedFrequency} Hz, Got: {result.FrequencyHz:F2} Hz");
    }

    /// <summary>
    /// Asserts that pitch detection result matches expected frequency within percentage tolerance.
    /// </summary>
    protected static void AssertPitchFrequencyPercent(
        PitchDetectionResult result,
        double expectedFrequency,
        double tolerancePercent = 2.0)
    {
        Assert.True(result.IsPitched, $"Expected pitch to be detected at {expectedFrequency} Hz, but no pitch was detected");
        var error = Math.Abs(result.FrequencyHz - expectedFrequency);
        var errorPercent = (error / expectedFrequency) * 100;
        Assert.True(
            errorPercent <= tolerancePercent,
            $"Frequency error {errorPercent:F2}% exceeds tolerance {tolerancePercent}%. Expected: {expectedFrequency} Hz, Got: {result.FrequencyHz:F2} Hz");
    }

    /// <summary>
    /// Asserts that the confidence is within the expected range.
    /// </summary>
    protected static void AssertConfidence(
        PitchDetectionResult result,
        float minConfidence,
        float maxConfidence = 1.0f)
    {
        Assert.InRange(result.Confidence, minConfidence, maxConfidence);
    }

    /// <summary>
    /// Asserts that no pitch was detected.
    /// </summary>
    protected static void AssertNoPitch(PitchDetectionResult result)
    {
        Assert.False(result.IsPitched, $"Expected no pitch, but detected {result.FrequencyHz:F2} Hz with confidence {result.Confidence:F2}");
    }

    /// <summary>
    /// Asserts that either no pitch was detected OR confidence is very low.
    /// </summary>
    protected static void AssertNoPitchOrLowConfidence(
        PitchDetectionResult result,
        float maxConfidence = 0.3f)
    {
        if (result.IsPitched)
        {
            Assert.InRange(result.Confidence, 0f, maxConfidence);
        }
    }
}
