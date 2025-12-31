using StaffSharp;
using StaffSharp.Audio.Analysis.Tempo;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Tests.Pipeline.Stages;

/// <summary>
/// Unit tests for DetectTempoStage.
/// </summary>
public sealed class DetectTempoStageTests
{
    [Fact]
    public async Task ProcessAsync_ValidInput_DetectsTempo()
    {
        // Arrange
        var timeSignatures = new List<TimeSignatureChange>
        {
            new TimeSignatureChange(Rational.Zero, TimeSignature.CommonTime)
        };
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5, 2.0 };
        var expectedTempo = new TempoMap(
            new List<TempoChange> { new TempoChange(Rational.Zero, 120.0) },
            timeSignatures
        );
        var detector = new MockTempoDetector(expectedTempo);
        var context = CreateContext(onsets);
        var stage = new DetectTempoStage(detector);

        // Act
        var result = await stage.ProcessAsync(timeSignatures, context);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.TempoChanges);
        Assert.NotNull(context.TempoMap);
        Assert.Same(result, context.TempoMap);
    }

    [Fact]
    public async Task ProcessAsync_NullDetector_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DetectTempoStage(null!));
    }

    [Fact]
    public async Task ProcessAsync_OnsetsNotInContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var timeSignatures = new List<TimeSignatureChange>
        {
            new TimeSignatureChange(Rational.Zero, TimeSignature.CommonTime)
        };
        var detector = new InterOnsetIntervalTempoDetector();
        var context = new AudioPipelineContext(); // No onsets in context
        var stage = new DetectTempoStage(detector);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stage.ProcessAsync(timeSignatures, context));
        Assert.Contains("Onsets not available in context", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_DetectorReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var timeSignatures = new List<TimeSignatureChange>
        {
            new TimeSignatureChange(Rational.Zero, TimeSignature.CommonTime)
        };
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var detector = new NullReturningTempoDetector();
        var context = CreateContext(onsets);
        var stage = new DetectTempoStage(detector);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stage.ProcessAsync(timeSignatures, context));
        Assert.Contains("Tempo detection failed", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_EmitsDiagnostics()
    {
        // Arrange
        var timeSignatures = new List<TimeSignatureChange>
        {
            new TimeSignatureChange(Rational.Zero, TimeSignature.CommonTime)
        };
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5 };
        var expectedTempo = new TempoMap(
            new List<TempoChange> { new TempoChange(Rational.Zero, 120.0) },
            timeSignatures
        );
        var detector = new MockTempoDetector(expectedTempo);
        var diagnostics = new MemoryDiagnosticsCollector();
        var context = new AudioPipelineContext(diagnosticsCollector: diagnostics);
        context.Onsets = onsets;
        var stage = new DetectTempoStage(detector);

        // Act
        await stage.ProcessAsync(timeSignatures, context);

        // Assert
        var entries = diagnostics.GetEntries().ToList();
        Assert.Contains(entries, e => e.StageName == "DetectTempo" && e.Key == "TempoChangeCount");
        Assert.Contains(entries, e => e.StageName == "DetectTempo" && e.Key == "TimeSignatureCount");
        Assert.Contains(entries, e => e.StageName == "DetectTempo" && e.Key == "InitialTempo");
    }

    [Fact]
    public async Task ProcessAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var timeSignatures = new List<TimeSignatureChange>
        {
            new TimeSignatureChange(Rational.Zero, TimeSignature.CommonTime)
        };
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var detector = new InterOnsetIntervalTempoDetector();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var context = new AudioPipelineContext(cancellationToken: cts.Token);
        context.Onsets = onsets;
        var stage = new DetectTempoStage(detector);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stage.ProcessAsync(timeSignatures, context));
    }

    [Fact]
    public void StageName_ReturnsDetectTempo()
    {
        // Arrange
        var detector = new InterOnsetIntervalTempoDetector();
        var stage = new DetectTempoStage(detector);

        // Act
        var name = stage.StageName;

        // Assert
        Assert.Equal("DetectTempo", name);
    }

    [Fact]
    public async Task ProcessAsync_SetsContextTempoMapProperty()
    {
        // Arrange
        var timeSignatures = new List<TimeSignatureChange>
        {
            new TimeSignatureChange(Rational.Zero, TimeSignature.CommonTime)
        };
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5 };
        var expectedTempo = new TempoMap(
            new List<TempoChange> { new TempoChange(Rational.Zero, 120.0) },
            timeSignatures
        );
        var detector = new MockTempoDetector(expectedTempo);
        var context = CreateContext(onsets);
        var stage = new DetectTempoStage(detector);

        Assert.Null(context.TempoMap);

        // Act
        var result = await stage.ProcessAsync(timeSignatures, context);

        // Assert
        Assert.NotNull(context.TempoMap);
        Assert.Same(result, context.TempoMap);
    }

    [Fact]
    public async Task ProcessAsync_PassesOnsetsToDetector()
    {
        // Arrange
        var timeSignatures = new List<TimeSignatureChange>
        {
            new TimeSignatureChange(Rational.Zero, TimeSignature.CommonTime)
        };
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5, 2.0 };
        var expectedTempo = new TempoMap(
            new List<TempoChange> { new TempoChange(Rational.Zero, 120.0) },
            timeSignatures
        );
        var detector = new MockTempoDetector(expectedTempo);
        var context = CreateContext(onsets);
        var stage = new DetectTempoStage(detector);

        // Act
        await stage.ProcessAsync(timeSignatures, context);

        // Assert
        Assert.True(detector.WasCalled);
        Assert.Equal(onsets.Length, detector.ReceivedOnsets?.Length ?? 0);
    }

    private static AudioPipelineContext CreateContext(double[] onsets)
    {
        var context = new AudioPipelineContext();
        context.Onsets = onsets;
        return context;
    }

    private sealed class NullReturningTempoDetector : ITempoDetector
    {
        public TempoMap? DetectTempo(ReadOnlySpan<double> onsets) => null;
    }

    private sealed class MockTempoDetector : ITempoDetector
    {
        private readonly TempoMap _tempoMap;

        public bool WasCalled { get; private set; }
        public double[]? ReceivedOnsets { get; private set; }

        public MockTempoDetector(TempoMap tempoMap)
        {
            _tempoMap = tempoMap;
        }

        public TempoMap? DetectTempo(ReadOnlySpan<double> onsets)
        {
            WasCalled = true;
            ReceivedOnsets = onsets.ToArray();
            return _tempoMap;
        }
    }
}
