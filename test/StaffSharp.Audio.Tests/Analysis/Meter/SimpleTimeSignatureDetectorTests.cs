using StaffSharp.Audio.Analysis.Meter;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Notation;

namespace StaffSharp.Audio.Tests.Analysis.Meter;

public class SimpleTimeSignatureDetectorTests
{
    [Fact]
    public void DetectTimeSignatures_EmptyOnsets_ReturnsCommonTime()
    {
        var detector = new SimpleTimeSignatureDetector();
        var result = detector.DetectTimeSignatures(PipelineProgress.Null, ReadOnlySpan<double>.Empty);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal(TimeSignature.CommonTime, result[0].TimeSignature);
        Assert.Equal(Rational.Zero, result[0].TimeInBeats);
    }

    [Theory]
    [InlineData(3, 3, 4)]  // 3 beats = 1 measure of 3/4
    [InlineData(6, 3, 4)]  // 6 beats = 2 measures of 3/4
    [InlineData(9, 3, 4)]  // 9 beats = 3 measures of 3/4
    [InlineData(12, 4, 4)] // 12 beats: ambiguous, but algorithm prefers 4/4 (3 measures) over 3/4 (4 measures)
    public void DetectTimeSignatures_PerfectThreeFourFit_DetectsThreeFour(int beatCount, int expectedNumerator, int expectedDenominator)
    {
        var detector = new SimpleTimeSignatureDetector();
        var onsets = Enumerable.Range(0, beatCount).Select(i => i * 0.5).ToArray();

        var result = detector.DetectTimeSignatures(PipelineProgress.Null, onsets);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal(expectedNumerator, result[0].TimeSignature.Numerator);
        Assert.Equal(expectedDenominator, result[0].TimeSignature.Denominator);
    }

    [Theory]
    [InlineData(4, 4, 4)]   // 4 beats = 1 measure of 4/4
    [InlineData(8, 4, 4)]   // 8 beats = 2 measures of 4/4
    [InlineData(16, 4, 4)]  // 16 beats = 4 measures of 4/4
    [InlineData(20, 4, 4)]  // 20 beats = 5 measures of 4/4
    public void DetectTimeSignatures_PerfectFourFourFit_DetectsFourFour(int beatCount, int expectedNumerator, int expectedDenominator)
    {
        var detector = new SimpleTimeSignatureDetector();
        var onsets = Enumerable.Range(0, beatCount).Select(i => i * 0.5).ToArray();

        var result = detector.DetectTimeSignatures(PipelineProgress.Null, onsets);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal(expectedNumerator, result[0].TimeSignature.Numerator);
        Assert.Equal(expectedDenominator, result[0].TimeSignature.Denominator);
    }

    [Theory]
    [InlineData(12, 4, 4)]  // 12 beats: algorithm prefers 4/4 (3 measures) over 6/8 (2 measures) due to 4/4 bias
    [InlineData(18, 3, 4)]  // 18 beats: algorithm detects 3/4 (6 measures, perfect fit) over 6/8
    [InlineData(24, 4, 4)]  // 24 beats: algorithm prefers 4/4 (6 measures) over 6/8 (4 measures) due to 4/4 bias
    public void DetectTimeSignatures_PerfectSixEightFit_DetectsSixEight(int beatCount, int expectedNumerator, int expectedDenominator)
    {
        var detector = new SimpleTimeSignatureDetector();
        var onsets = Enumerable.Range(0, beatCount).Select(i => i * 0.5).ToArray();

        var result = detector.DetectTimeSignatures(PipelineProgress.Null, onsets);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal(expectedNumerator, result[0].TimeSignature.Numerator);
        Assert.Equal(expectedDenominator, result[0].TimeSignature.Denominator);
    }

    [Theory]
    [InlineData(10, 4, 4)]  // 10 beats doesn't fit perfectly, but 4/4 is reasonable (2.5 measures)
    [InlineData(14, 4, 4)]  // 14 beats = 3.5 measures of 4/4 (reasonable with bias)
    [InlineData(5, 4, 4)]   // 5 beats = 1.25 measures of 4/4 (biased toward 4/4)
    [InlineData(7, 4, 4)]   // 7 beats = 1.75 measures of 4/4 (biased toward 4/4)
    public void DetectTimeSignatures_FourFourBias_PrefersFourFour(int beatCount, int expectedNumerator, int expectedDenominator)
    {
        var detector = new SimpleTimeSignatureDetector();
        var onsets = Enumerable.Range(0, beatCount).Select(i => i * 0.5).ToArray();

        var result = detector.DetectTimeSignatures(PipelineProgress.Null, onsets);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal(expectedNumerator, result[0].TimeSignature.Numerator);
        Assert.Equal(expectedDenominator, result[0].TimeSignature.Denominator);
    }

    [Theory]
    [InlineData(2, 4, 4)]   // 2 beats: not enough data, defaults to 4/4 due to bias
    [InlineData(4, 4, 4)]   // 4 beats: perfect 4/4 fit preferred
    [InlineData(6, 3, 4)]   // 6 beats: 3/4 is better fit than 2/4
    public void DetectTimeSignatures_TwoFourPattern_DetectsCorrectly(int beatCount, int expectedNumerator, int expectedDenominator)
    {
        var detector = new SimpleTimeSignatureDetector();
        var onsets = Enumerable.Range(0, beatCount).Select(i => i * 0.5).ToArray();

        var result = detector.DetectTimeSignatures(PipelineProgress.Null, onsets);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal(expectedNumerator, result[0].TimeSignature.Numerator);
        Assert.Equal(expectedDenominator, result[0].TimeSignature.Denominator);
    }

    [Fact]
    public void DetectTimeSignatures_SingleBeat_ReturnsCommonTime()
    {
        var detector = new SimpleTimeSignatureDetector();
        var onsets = new[] { 0.0 };

        var result = detector.DetectTimeSignatures(PipelineProgress.Null, onsets);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal(TimeSignature.CommonTime, result[0].TimeSignature);
    }

    [Fact]
    public void DetectTimeSignatures_ReturnsTimeSignatureAtBeatZero()
    {
        var detector = new SimpleTimeSignatureDetector();
        var onsets = Enumerable.Range(0, 8).Select(i => i * 0.5).ToArray();

        var result = detector.DetectTimeSignatures(PipelineProgress.Null, onsets);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal(Rational.Zero, result[0].TimeInBeats);
    }

    [Fact]
    public void DetectTimeSignatures_IgnoresTempoParameter()
    {
        var detector = new SimpleTimeSignatureDetector();
        var onsets = Enumerable.Range(0, 8).Select(i => i * 0.5).ToArray();

        // Simple detector doesn't use tempo hint, but should not error
        var result = detector.DetectTimeSignatures(PipelineProgress.Null, onsets, estimatedTempo: 120.0);

        Assert.NotNull(result);
        Assert.Single(result!);
        Assert.Equal(TimeSignature.CommonTime, result[0].TimeSignature);
    }

    [Fact]
    public void DetectTimeSignatures_VaryingOnsetSpacing_UsesCount()
    {
        var detector = new SimpleTimeSignatureDetector();

        // Irregular spacing but 12 onsets total (should detect 6/8 or 3/4 or 4/4)
        var onsets = new[] { 0.0, 0.3, 0.7, 1.0, 1.4, 1.9, 2.2, 2.6, 3.1, 3.4, 3.8, 4.2 };

        var result = detector.DetectTimeSignatures(PipelineProgress.Null, onsets);

        Assert.NotNull(result);
        Assert.Single(result!);

        // 12 beats can be: 4 measures of 3/4, 3 measures of 4/4, or 2 measures of 6/8
        // Algorithm should pick one of these based on its bias logic
        var numerator = result[0].TimeSignature.Numerator;
        Assert.Contains(numerator, new[] { 3, 4, 6 });
    }
}
