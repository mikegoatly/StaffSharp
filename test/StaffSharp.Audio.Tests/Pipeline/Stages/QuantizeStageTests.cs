using StaffSharp;
using StaffSharp.Audio.Analysis.Quantization;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Tests.Pipeline.Stages;

/// <summary>
/// Unit tests for QuantizeStage.
/// </summary>
public sealed class QuantizeStageTests
{
    [Fact]
    public async Task ProcessAsync_ValidInput_QuantizesNotes()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5 };
        var pitches = new int[] { 60, 64, 67, 72 };
        var expectedQuantized = new List<QuantizedNoteEvent>
        {
            TestDataFactory.CreateQuantizedNoteEvent(60, 0.0, 0.0),
            TestDataFactory.CreateQuantizedNoteEvent(64, 0.5, 0.0),
            TestDataFactory.CreateQuantizedNoteEvent(67, 1.0, 0.0),
            TestDataFactory.CreateQuantizedNoteEvent(72, 1.5, 0.0)
        };
        var quantizer = new MockQuantizer(expectedQuantized);
        var context = CreateContext(onsets, pitches, tempoMap);
        var stage = new QuantizeStage(quantizer);

        // Act
        var result = await stage.ProcessAsync(tempoMap, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.NotNull(context.QuantizedNotes);
        Assert.Same(result, context.QuantizedNotes);
    }

    [Fact]
    public async Task ProcessAsync_NullQuantizer_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new QuantizeStage(null!));
    }

    [Fact]
    public async Task ProcessAsync_OnsetsNotInContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var quantizer = new SimpleQuantizer();
        var context = new AudioPipelineContext(); // No onsets in context
        var stage = new QuantizeStage(quantizer);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stage.ProcessAsync(tempoMap, context));
        Assert.Contains("Onsets not available in context", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_PitchesNotInContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var quantizer = new SimpleQuantizer();
        var context = new AudioPipelineContext();
        context.Onsets = onsets; // No pitches in context
        var stage = new QuantizeStage(quantizer);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stage.ProcessAsync(tempoMap, context));
        Assert.Contains("Pitches not available in context", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_QuantizerReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var pitches = new int[] { 60, 64, 67 };
        var quantizer = new NullReturningQuantizer();
        var context = CreateContext(onsets, pitches, tempoMap);
        var stage = new QuantizeStage(quantizer);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stage.ProcessAsync(tempoMap, context));
        Assert.Contains("Quantization failed", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_EmitsDiagnostics()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var pitches = new int[] { 60, 64, 67 };
        var expectedQuantized = new List<QuantizedNoteEvent>
        {
            TestDataFactory.CreateQuantizedNoteEvent(60, 0.0, 0.0),
            TestDataFactory.CreateQuantizedNoteEvent(64, 0.5, 0.0),
            TestDataFactory.CreateQuantizedNoteEvent(67, 1.0, 0.0)
        };
        var quantizer = new MockQuantizer(expectedQuantized);
        var diagnostics = new MemoryDiagnosticsCollector();
        var context = new AudioPipelineContext(diagnosticsCollector: diagnostics);
        context.Onsets = onsets;
        context.Pitches = pitches;
        context.TempoMap = tempoMap;
        var stage = new QuantizeStage(quantizer);

        // Act
        await stage.ProcessAsync(tempoMap, context);

        // Assert
        var entries = diagnostics.GetEntries().ToList();
        Assert.Contains(entries, e => e.StageName == "Quantize" && e.Key == "QuantizedNoteCount");
        Assert.Contains(entries, e => e.StageName == "Quantize" && e.Key == "QuantizedNotes");
    }

    [Fact]
    public async Task ProcessAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var pitches = new int[] { 60, 64, 67 };
        var quantizer = new SimpleQuantizer();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var context = new AudioPipelineContext(cancellationToken: cts.Token);
        context.Onsets = onsets;
        context.Pitches = pitches;
        context.TempoMap = tempoMap;
        var stage = new QuantizeStage(quantizer);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stage.ProcessAsync(tempoMap, context));
    }

    [Fact]
    public void StageName_ReturnsQuantize()
    {
        // Arrange
        var quantizer = new SimpleQuantizer();
        var stage = new QuantizeStage(quantizer);

        // Act
        var name = stage.StageName;

        // Assert
        Assert.Equal("Quantize", name);
    }

    [Fact]
    public async Task ProcessAsync_SetsContextQuantizedNotesProperty()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var onsets = new double[] { 0.0, 0.5, 1.0 };
        var pitches = new int[] { 60, 64, 67 };
        var expectedQuantized = new List<QuantizedNoteEvent>
        {
            TestDataFactory.CreateQuantizedNoteEvent(60, 0.0, 0.0),
            TestDataFactory.CreateQuantizedNoteEvent(64, 0.5, 0.0),
            TestDataFactory.CreateQuantizedNoteEvent(67, 1.0, 0.0)
        };
        var quantizer = new MockQuantizer(expectedQuantized);
        var context = CreateContext(onsets, pitches, tempoMap);
        var stage = new QuantizeStage(quantizer);

        Assert.Null(context.QuantizedNotes);

        // Act
        var result = await stage.ProcessAsync(tempoMap, context);

        // Assert
        Assert.NotNull(context.QuantizedNotes);
        Assert.Same(result, context.QuantizedNotes);
    }

    [Fact]
    public async Task ProcessAsync_PassesCorrectDataToQuantizer()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var onsets = new double[] { 0.0, 0.5, 1.0, 1.5 };
        var pitches = new int[] { 60, 64, 67, 72 };
        var expectedQuantized = new List<QuantizedNoteEvent>
        {
            TestDataFactory.CreateQuantizedNoteEvent(60, 0.0, 0.0)
        };
        var quantizer = new MockQuantizer(expectedQuantized);
        var context = CreateContext(onsets, pitches, tempoMap);
        var stage = new QuantizeStage(quantizer);

        // Act
        await stage.ProcessAsync(tempoMap, context);

        // Assert
        Assert.True(quantizer.WasCalled);
        Assert.Equal(onsets.Length, quantizer.ReceivedOnsets?.Length ?? 0);
        Assert.Equal(pitches.Length, quantizer.ReceivedPitches?.Length ?? 0);
        Assert.Same(tempoMap, quantizer.ReceivedTempoMap);
    }

    private static TempoMap CreateTempoMap()
    {
        var tempoChanges = new List<TempoChange>
        {
            new TempoChange(Rational.Zero, 120.0)
        };
        var timeSignatures = new List<TimeSignatureChange>
        {
            new TimeSignatureChange(Rational.Zero, TimeSignature.CommonTime)
        };
        return new TempoMap(tempoChanges, timeSignatures);
    }

    private static AudioPipelineContext CreateContext(double[] onsets, int[] pitches, TempoMap tempoMap)
    {
        var context = new AudioPipelineContext();
        context.Onsets = onsets;
        context.Pitches = pitches;
        context.TempoMap = tempoMap;
        return context;
    }

    private sealed class NullReturningQuantizer : IQuantizer
    {
        public IReadOnlyList<QuantizedNoteEvent>? Quantize(
            ReadOnlySpan<double> onsets,
            ReadOnlySpan<int> pitches,
            TempoMap tempoMap) => null;
    }

    private sealed class MockQuantizer : IQuantizer
    {
        private readonly IReadOnlyList<QuantizedNoteEvent> _quantized;

        public bool WasCalled { get; private set; }
        public double[]? ReceivedOnsets { get; private set; }
        public int[]? ReceivedPitches { get; private set; }
        public TempoMap? ReceivedTempoMap { get; private set; }

        public MockQuantizer(IReadOnlyList<QuantizedNoteEvent> quantized)
        {
            _quantized = quantized;
        }

        public IReadOnlyList<QuantizedNoteEvent>? Quantize(
            ReadOnlySpan<double> onsets,
            ReadOnlySpan<int> pitches,
            TempoMap tempoMap)
        {
            WasCalled = true;
            ReceivedOnsets = onsets.ToArray();
            ReceivedPitches = pitches.ToArray();
            ReceivedTempoMap = tempoMap;
            return _quantized;
        }
    }
}
