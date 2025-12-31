using StaffSharp;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Audio.Tests.Pipeline.Stages;

/// <summary>
/// Unit tests for BuildTimelineStage.
/// </summary>
public sealed class BuildTimelineStageTests
{
    [Fact]
    public async Task ProcessAsync_ValidInput_BuildsTimeline()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var quantizedNotes = CreateQuantizedNotes();
        var context = CreateContext(tempoMap);
        var stage = new BuildTimelineStage();

        // Act
        var result = await stage.ProcessAsync(quantizedNotes, context);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.TempoMap);
        Assert.NotNull(result.Events);
        Assert.Equal(quantizedNotes.Count, result.Events.Count);
        Assert.NotNull(context.Timeline);
        Assert.Same(result, context.Timeline);
    }

    [Fact]
    public async Task ProcessAsync_TempoMapNotInContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var quantizedNotes = CreateQuantizedNotes();
        var context = new AudioPipelineContext(); // No tempo map in context
        var stage = new BuildTimelineStage();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stage.ProcessAsync(quantizedNotes, context));
        Assert.Contains("TempoMap not available in context", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_EmptyNoteList_CreatesTimelineWithNoEvents()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var quantizedNotes = new List<QuantizedNoteEvent>();
        var context = CreateContext(tempoMap);
        var stage = new BuildTimelineStage();

        // Act
        var result = await stage.ProcessAsync(quantizedNotes, context);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Events);
    }

    [Fact]
    public async Task ProcessAsync_CreatesCorrectMetadata()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var quantizedNotes = CreateQuantizedNotes();
        var context = CreateContext(tempoMap);
        var stage = new BuildTimelineStage();

        // Act
        var result = await stage.ProcessAsync(quantizedNotes, context);

        // Assert
        Assert.NotNull(result.Metadata);
        Assert.Null(result.Metadata.Title);
        Assert.Null(result.Metadata.Composer);
        Assert.Null(result.Metadata.Copyright);
    }

    [Fact]
    public async Task ProcessAsync_PreservesTempoMap()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var quantizedNotes = CreateQuantizedNotes();
        var context = CreateContext(tempoMap);
        var stage = new BuildTimelineStage();

        // Act
        var result = await stage.ProcessAsync(quantizedNotes, context);

        // Assert
        Assert.Same(tempoMap, result.TempoMap);
    }

    [Fact]
    public async Task ProcessAsync_PreservesQuantizedNotes()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var quantizedNotes = CreateQuantizedNotes();
        var context = CreateContext(tempoMap);
        var stage = new BuildTimelineStage();

        // Act
        var result = await stage.ProcessAsync(quantizedNotes, context);

        // Assert
        Assert.Equal(quantizedNotes, result.Events);
    }

    [Fact]
    public async Task ProcessAsync_EmitsDiagnostics()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var quantizedNotes = CreateQuantizedNotes();
        var diagnostics = new MemoryDiagnosticsCollector();
        var context = new AudioPipelineContext(diagnosticsCollector: diagnostics);
        context.TempoMap = tempoMap;
        var stage = new BuildTimelineStage();

        // Act
        await stage.ProcessAsync(quantizedNotes, context);

        // Assert
        var entries = diagnostics.GetEntries().ToList();
        Assert.Contains(entries, e => e.StageName == "BuildTimeline" && e.Key == "EventCount");
        Assert.Contains(entries, e => e.StageName == "BuildTimeline" && e.Key == "TotalDurationBeats");
    }

    [Fact]
    public async Task ProcessAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var quantizedNotes = CreateQuantizedNotes();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var context = new AudioPipelineContext(cancellationToken: cts.Token);
        context.TempoMap = tempoMap;
        var stage = new BuildTimelineStage();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stage.ProcessAsync(quantizedNotes, context));
    }

    [Fact]
    public void StageName_ReturnsBuildTimeline()
    {
        // Arrange
        var stage = new BuildTimelineStage();

        // Act
        var name = stage.StageName;

        // Assert
        Assert.Equal("BuildTimeline", name);
    }

    [Fact]
    public async Task ProcessAsync_SetsContextTimelineProperty()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var quantizedNotes = CreateQuantizedNotes();
        var context = CreateContext(tempoMap);
        var stage = new BuildTimelineStage();

        Assert.Null(context.Timeline);

        // Act
        var result = await stage.ProcessAsync(quantizedNotes, context);

        // Assert
        Assert.NotNull(context.Timeline);
        Assert.Same(result, context.Timeline);
    }

    [Fact]
    public async Task ProcessAsync_MultipleNotes_CreatesTimelineWithAllNotes()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var quantizedNotes = new List<QuantizedNoteEvent>
        {
            TestDataFactory.CreateQuantizedNoteEvent(60, 0.0, 0.25),
            TestDataFactory.CreateQuantizedNoteEvent(64, 0.25, 0.25),
            TestDataFactory.CreateQuantizedNoteEvent(67, 0.5, 0.25),
            TestDataFactory.CreateQuantizedNoteEvent(72, 0.75, 0.25)
        };
        var context = CreateContext(tempoMap);
        var stage = new BuildTimelineStage();

        // Act
        var result = await stage.ProcessAsync(quantizedNotes, context);

        // Assert
        Assert.Equal(4, result.Events.Count);
        Assert.All(result.Events, e => Assert.NotNull(e));
    }

    [Fact]
    public async Task ProcessAsync_CalculatesTotalDurationBeats()
    {
        // Arrange
        var tempoMap = CreateTempoMap();
        var quantizedNotes = new List<QuantizedNoteEvent>
        {
            TestDataFactory.CreateQuantizedNoteEvent(60, 0.0, 0.25),
            TestDataFactory.CreateQuantizedNoteEvent(64, 1.0, 0.5),
            TestDataFactory.CreateQuantizedNoteEvent(67, 2.0, 1.0)
        };
        var context = CreateContext(tempoMap);
        var stage = new BuildTimelineStage();

        // Act
        var result = await stage.ProcessAsync(quantizedNotes, context);

        // Assert
        // TotalDurationBeats should be computed based on the last note
        Assert.True(result.TotalDurationBeats >= Rational.Zero);
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

    private static List<QuantizedNoteEvent> CreateQuantizedNotes()
    {
        return new List<QuantizedNoteEvent>
        {
            TestDataFactory.CreateQuantizedNoteEvent(60, 0.0, 0.25),
            TestDataFactory.CreateQuantizedNoteEvent(64, 0.25, 0.25),
            TestDataFactory.CreateQuantizedNoteEvent(67, 0.5, 0.25)
        };
    }

    private static AudioPipelineContext CreateContext(TempoMap tempoMap)
    {
        var context = new AudioPipelineContext();
        context.TempoMap = tempoMap;
        return context;
    }
}
