using StaffSharp;
using StaffSharp.Audio.Analysis.Meter;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Tests.Pipeline.Stages;

/// <summary>
/// Unit tests for DetectTimeSignatureStage.
/// </summary>
public sealed class DetectTimeSignatureStageTests
{
    [Fact]
    public async Task ProcessAsync_WithNullDetector_ReturnsDefaultFourFour()
    {
        // Arrange
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5 };
        var context = new AudioPipelineContext();
        var stage = new DetectTimeSignatureStage(detector: null);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(Rational.Zero, result[0].TimeInBeats);
        Assert.Equal(TimeSignature.CommonTime, result[0].TimeSignature);
        Assert.NotNull(context.TimeSignatures);
    }

    [Fact]
    public async Task ProcessAsync_DetectorReturnsNull_ReturnsDefaultFourFour()
    {
        // Arrange
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5 };
        var detector = new NullReturningTimeSignatureDetector();
        var context = new AudioPipelineContext();
        var stage = new DetectTimeSignatureStage(detector);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(TimeSignature.CommonTime, result[0].TimeSignature);
    }

    [Fact]
    public async Task ProcessAsync_DetectorReturnsEmpty_ReturnsDefaultFourFour()
    {
        // Arrange
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5 };
        var detector = new EmptyReturningTimeSignatureDetector();
        var context = new AudioPipelineContext();
        var stage = new DetectTimeSignatureStage(detector);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(TimeSignature.CommonTime, result[0].TimeSignature);
    }

    [Fact]
    public async Task ProcessAsync_ValidDetector_ReturnsDetectedTimeSignatures()
    {
        // Arrange
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5, 2.0, 2.5 };
        var expectedTimeSignatures = new List<TimeSignatureChange>
        {
            new TimeSignatureChange(Rational.Zero, new TimeSignature(3, 4))
        };
        var detector = new MockTimeSignatureDetector(expectedTimeSignatures);
        var context = new AudioPipelineContext();
        var stage = new DetectTimeSignatureStage(detector);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(3, result[0].TimeSignature.Numerator);
        Assert.Equal(4, result[0].TimeSignature.Denominator);
    }

    [Fact]
    public async Task ProcessAsync_EmitsDiagnostics_WithNullDetector()
    {
        // Arrange
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var diagnostics = new MemoryDiagnosticsCollector();
        var context = new AudioPipelineContext(diagnosticsCollector: diagnostics);
        var stage = new DetectTimeSignatureStage(detector: null);

        // Act
        await stage.ProcessAsync(onsets, context);

        // Assert
        var entries = diagnostics.GetEntries().ToList();
        Assert.Contains(entries, e => e.StageName == "DetectTimeSignature" && e.Key == "DetectorUsed");
        Assert.Contains(entries, e => e.StageName == "DetectTimeSignature" && e.Key == "TimeSignatureCount");
        Assert.Contains(entries, e => e.StageName == "DetectTimeSignature" && e.Key == "TimeSignatures");

        var detectorUsedEntry = entries.First(e => e.Key == "DetectorUsed");
        Assert.Equal("Default (4/4)", detectorUsedEntry.Value);
    }

    [Fact]
    public async Task ProcessAsync_EmitsDiagnostics_WithValidDetector()
    {
        // Arrange
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var expectedTimeSignatures = new List<TimeSignatureChange>
        {
            new TimeSignatureChange(Rational.Zero, new TimeSignature(3, 4))
        };
        var detector = new MockTimeSignatureDetector(expectedTimeSignatures);
        var diagnostics = new MemoryDiagnosticsCollector();
        var context = new AudioPipelineContext(diagnosticsCollector: diagnostics);
        var stage = new DetectTimeSignatureStage(detector);

        // Act
        await stage.ProcessAsync(onsets, context);

        // Assert
        var entries = diagnostics.GetEntries().ToList();
        Assert.Contains(entries, e => e.StageName == "DetectTimeSignature" && e.Key == "DetectorUsed");
        Assert.Contains(entries, e => e.StageName == "DetectTimeSignature" && e.Key == "TimeSignatureCount");
        Assert.Contains(entries, e => e.StageName == "DetectTimeSignature" && e.Key == "TimeSignatures");

        var detectorUsedEntry = entries.First(e => e.Key == "DetectorUsed");
        Assert.Equal("MockTimeSignatureDetector", detectorUsedEntry.Value);
    }

    [Fact]
    public async Task ProcessAsync_EmitsDiagnostics_WhenDetectorFails()
    {
        // Arrange
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var detector = new NullReturningTimeSignatureDetector();
        var diagnostics = new MemoryDiagnosticsCollector();
        var context = new AudioPipelineContext(diagnosticsCollector: diagnostics);
        var stage = new DetectTimeSignatureStage(detector);

        // Act
        await stage.ProcessAsync(onsets, context);

        // Assert
        var entries = diagnostics.GetEntries().ToList();
        var detectorUsedEntry = entries.First(e => e.Key == "DetectorUsed");
        Assert.Equal("Failed - Fallback to 4/4", detectorUsedEntry.Value);
    }

    [Fact]
    public async Task ProcessAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var context = new AudioPipelineContext(cancellationToken: cts.Token);
        var stage = new DetectTimeSignatureStage(detector: null);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stage.ProcessAsync(onsets, context));
    }

    [Fact]
    public void StageName_ReturnsDetectTimeSignature()
    {
        // Arrange
        var stage = new DetectTimeSignatureStage(detector: null);

        // Act
        var name = stage.StageName;

        // Assert
        Assert.Equal("DetectTimeSignature", name);
    }

    [Fact]
    public async Task ProcessAsync_SetsContextTimeSignaturesProperty()
    {
        // Arrange
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var context = new AudioPipelineContext();
        var stage = new DetectTimeSignatureStage(detector: null);

        Assert.Null(context.TimeSignatures);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.NotNull(context.TimeSignatures);
        Assert.Same(result, context.TimeSignatures);
    }

    private sealed class NullReturningTimeSignatureDetector : ITimeSignatureDetector
    {
        public IReadOnlyList<TimeSignatureChange>? DetectTimeSignatures(ReadOnlySpan<double> onsets, double? estimatedTempo = null) => null;
    }

    private sealed class EmptyReturningTimeSignatureDetector : ITimeSignatureDetector
    {
        public IReadOnlyList<TimeSignatureChange>? DetectTimeSignatures(ReadOnlySpan<double> onsets, double? estimatedTempo = null) =>
            new List<TimeSignatureChange>();
    }

    private sealed class MockTimeSignatureDetector : ITimeSignatureDetector
    {
        private readonly IReadOnlyList<TimeSignatureChange> _timeSignatures;

        public MockTimeSignatureDetector(IReadOnlyList<TimeSignatureChange> timeSignatures)
        {
            _timeSignatures = timeSignatures;
        }

        public IReadOnlyList<TimeSignatureChange>? DetectTimeSignatures(ReadOnlySpan<double> onsets, double? estimatedTempo = null) =>
            _timeSignatures;
    }
}
