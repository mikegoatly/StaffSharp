using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Analysis.Meter;

/// <summary>
/// Simple time signature detector using beat count division.
/// Determines meter by analyzing how cleanly beats divide into measures.
/// Strongly biases toward 4/4 (appropriate for amateur recordings).
/// Returns a single time signature for Phase 1; architecture supports meter changes for Phase 2.
/// </summary>
public sealed class SimpleTimeSignatureDetector : ITimeSignatureDetector
{
    public SimpleTimeSignatureDetector(TimeSignatureDetectionOptions? options = null)
    {
        options ??= new TimeSignatureDetectionOptions();
        options.Validate();
    }

    public IReadOnlyList<TimeSignatureChange>? DetectTimeSignatures(
        ReadOnlySpan<double> onsetTimes,
        double? estimatedTempo = null)
    {
        if (onsetTimes.Length == 0)
        {
            return new List<TimeSignatureChange>
            {
                new TimeSignatureChange(Rational.Zero, TimeSignature.CommonTime)
            };
        }

        // Count beats (onsets)
        var beatCount = onsetTimes.Length;

        // Detect time signature using beat count division
        var detectedMeter = DetectFromBeatCount(beatCount);

        return new List<TimeSignatureChange>
        {
            new TimeSignatureChange(Rational.Zero, detectedMeter)
        };
    }

    /// <summary>
    /// Detects time signature by analyzing how cleanly beats divide into measures.
    /// Based on proven heuristic: try each common meter and pick best fit.
    /// Strongly biases toward 4/4 (appropriate for amateur recordings).
    /// </summary>
    private static TimeSignature DetectFromBeatCount(int beatCount)
    {
        if (beatCount == 0)
        {
            return TimeSignature.CommonTime; // Default fallback
        }

        // Calculate how cleanly beats divide into measures for each time signature
        var measures3_4 = beatCount / 3.0;
        var measures4_4 = beatCount / 4.0;
        var measures6_8 = beatCount / 6.0;
        var measures2_4 = beatCount / 2.0;

        // Calculate error (distance from whole number of measures)
        var error3_4 = Math.Abs(measures3_4 - Math.Round(measures3_4));
        var error4_4 = Math.Abs(measures4_4 - Math.Round(measures4_4));
        var error6_8 = Math.Abs(measures6_8 - Math.Round(measures6_8));
        var error2_4 = Math.Abs(measures2_4 - Math.Round(measures2_4));

        // Require at least 1 measure for edge cases (like 3 beats = 1 measure of 3/4)
        // Prefer at least 2 measures for more confidence
        const double minMeasuresPreferred = 2.0;
        const double minMeasuresAcceptable = 1.0;

        // Strategy: Strongly prefer 4/4, but detect clear patterns for other signatures

        // 1. Check for perfect 4/4 fit first (most common)
        if (error4_4 < 0.05 && measures4_4 >= minMeasuresPreferred)
        {
            return TimeSignature.CommonTime;
        }

        // 2. Check for perfect 3/4 fit
        if (error3_4 < 0.05 && measures3_4 >= minMeasuresAcceptable)
        {
            return new TimeSignature(3, 4);
        }

        // 3. Check for perfect 6/8 fit
        if (error6_8 < 0.05 && measures6_8 >= minMeasuresPreferred)
        {
            return new TimeSignature(6, 8);
        }

        // 4. For most cases, prefer 4/4 if it's reasonable (even with moderate error)
        //    This handles: 4, 8, 16 beats (perfect), 10, 14 beats (acceptable)
        if (measures4_4 >= minMeasuresAcceptable && error4_4 <= 0.5)
        {
            return TimeSignature.CommonTime;
        }

        // 5. If we get here, 4/4 doesn't fit well. Check if 3/4 fits reasonably
        if (error3_4 < error4_4 && measures3_4 >= minMeasuresAcceptable)
        {
            return new TimeSignature(3, 4);
        }

        // 6. Check if 6/8 fits better than 4/4
        if (error6_8 < error4_4 && measures6_8 >= minMeasuresPreferred)
        {
            return new TimeSignature(6, 8);
        }

        // 7. Last resort: check 2/4 (only if significantly better than 4/4)
        if (error2_4 < 0.05 && error2_4 < (error4_4 - 0.3) && measures2_4 >= minMeasuresPreferred)
        {
            return new TimeSignature(2, 4);
        }

        // Default to 4/4 if nothing else fits
        return TimeSignature.CommonTime;
    }
}
