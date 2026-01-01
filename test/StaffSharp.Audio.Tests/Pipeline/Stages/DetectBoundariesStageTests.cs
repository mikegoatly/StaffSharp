using StaffSharp.Audio;
using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Audio.Tests.Pipeline.Stages;

/// <summary>
/// Unit tests for DetectBoundariesStage.
/// </summary>
public sealed class DetectBoundariesStageTests
{
    private const int SampleRate = 44100;

    [Fact]
    public async Task ProcessAsync_ValidAudio_DetectsBoundaries()
    {
        // Arrange
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(2.0)
            .AtTime(0.5).AddSine(440.0, amplitude: 0.5, durationSeconds: 1.0)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var detector = new EnergyBasedBoundaryDetector();
        var context = CreateContext(audio);
        var stage = new DetectBoundariesStage(detector);

        // Act
        var result = await stage.ProcessAsync(audio, context);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.StartSample < result.EndSample);
        Assert.NotNull(context.Boundaries);
        Assert.Same(result, context.Boundaries);
    }

    [Fact]
    public async Task ProcessAsync_AudioWithLeadingAndTrailingSilence_DetectsCorrectly()
    {
        // Arrange: 0.5s silence + 1s tone + 0.5s silence
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(2.0)
            .AtTime(0.5).AddSine(440.0, amplitude: 0.5, durationSeconds: 1.0)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var detector = new EnergyBasedBoundaryDetector();
        var context = CreateContext(audio);
        var stage = new DetectBoundariesStage(detector);

        // Act
        var result = await stage.ProcessAsync(audio, context);

        // Assert
        Assert.InRange(result.LeadingSilence.TotalSeconds, 0.3, 0.7);
        Assert.InRange(result.TrailingSilence.TotalSeconds, 0.3, 0.7);
    }

    [Fact]
    public async Task ProcessAsync_NullDetector_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DetectBoundariesStage(null!));
    }

    [Fact]
    public async Task ProcessAsync_DetectorReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        var audio = new AudioBuffer(samples, SampleRate, 1);
        var detector = new NullReturningBoundaryDetector();
        var context = CreateContext(audio);
        var stage = new DetectBoundariesStage(detector);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stage.ProcessAsync(audio, context));
        Assert.Contains("Boundary detection failed", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_EmitsDiagnostics()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        var audio = new AudioBuffer(samples, SampleRate, 1);
        var detector = new EnergyBasedBoundaryDetector();
        var diagnostics = new MemoryDiagnosticsCollector();
        var context = new AudioPipelineContext(diagnosticsCollector: diagnostics);
        context.Audio = audio;
        var stage = new DetectBoundariesStage(detector);

        // Act
        await stage.ProcessAsync(audio, context);

        // Assert
        var entries = diagnostics.GetEntries().ToList();
        Assert.Contains(entries, e => e.StageName == "DetectBoundaries" && e.Key == "StartSample");
        Assert.Contains(entries, e => e.StageName == "DetectBoundaries" && e.Key == "EndSample");
        Assert.Contains(entries, e => e.StageName == "DetectBoundaries" && e.Key == "LeadingSilence");
        Assert.Contains(entries, e => e.StageName == "DetectBoundaries" && e.Key == "TrailingSilence");
        Assert.Contains(entries, e => e.StageName == "DetectBoundaries" && e.Key == "ContentDuration");
    }

    [Fact]
    public async Task ProcessAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        var audio = new AudioBuffer(samples, SampleRate, 1);
        var detector = new EnergyBasedBoundaryDetector();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var context = new AudioPipelineContext(cancellationToken: cts.Token);
        context.Audio = audio;
        var stage = new DetectBoundariesStage(detector);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stage.ProcessAsync(audio, context));
    }

    [Fact]
    public void StageName_ReturnsDetectBoundaries()
    {
        // Arrange
        var detector = new EnergyBasedBoundaryDetector();
        var stage = new DetectBoundariesStage(detector);

        // Act
        var name = stage.StageName;

        // Assert
        Assert.Equal("DetectBoundaries", name);
    }

    [Fact]
    public async Task ProcessAsync_SetsContextBoundariesProperty()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        var audio = new AudioBuffer(samples, SampleRate, 1);
        var detector = new EnergyBasedBoundaryDetector();
        var context = CreateContext(audio);
        var stage = new DetectBoundariesStage(detector);

        Assert.Null(context.Boundaries);

        // Act
        var result = await stage.ProcessAsync(audio, context);

        // Assert
        Assert.NotNull(context.Boundaries);
        Assert.Same(result, context.Boundaries);
    }

    private static AudioPipelineContext CreateContext(AudioBuffer audio)
    {
        var context = new AudioPipelineContext();
        context.Audio = audio;
        return context;
    }

    private sealed class NullReturningBoundaryDetector : IAudioBoundaryDetector
    {
        public AudioBoundaries? DetectBoundaries(AudioBuffer audio) => null;
    }
}
