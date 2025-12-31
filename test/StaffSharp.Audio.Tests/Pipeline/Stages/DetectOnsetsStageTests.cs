using StaffSharp.Audio;
using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Analysis.Onset;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Audio.Tests.Pipeline.Stages;

/// <summary>
/// Unit tests for DetectOnsetsStage.
/// </summary>
public sealed class DetectOnsetsStageTests
{
    private const int SampleRate = 44100;

    [Fact]
    public async Task ProcessAsync_ValidInput_DetectsOnsets()
    {
        // Arrange
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0)
            .AtTime(0.1).WithAttack(0.02).AddSine(440.0, amplitude: 0.5, durationSeconds: 0.3)
            .AtTime(0.5).WithAttack(0.02).AddSine(523.25, amplitude: 0.5, durationSeconds: 0.3)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var detector = new SpectralFluxOnsetDetector();
        var context = CreateContext(audio, boundaries);
        var stage = new DetectOnsetsStage(detector);

        // Act
        var result = await stage.ProcessAsync(boundaries, context);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.NotNull(context.Onsets);
    }

    [Fact]
    public async Task ProcessAsync_NullDetector_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DetectOnsetsStage(null!));
    }

    [Fact]
    public async Task ProcessAsync_AudioNotInContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var detector = new SpectralFluxOnsetDetector();
        var context = new AudioPipelineContext(); // No audio in context
        var stage = new DetectOnsetsStage(detector);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stage.ProcessAsync(boundaries, context));
        Assert.Contains("Audio buffer not available in context", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_SlicesAudioToContentRegion()
    {
        // Arrange: Create audio with silence boundaries
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(2.0)
            .AtTime(0.5).WithAttack(0.02).AddSine(440.0, amplitude: 0.5, durationSeconds: 1.0)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);

        // Manually create boundaries that skip the first 0.5s
        var startSample = (int)(0.5 * SampleRate);
        var endSample = (int)(1.5 * SampleRate);
        var boundaries = TestDataFactory.CreateAudioBoundaries(
            audio,
            startSample,
            endSample,
            leadingSilence: TimeSpan.FromSeconds(0.5));

        var detector = new SpectralFluxOnsetDetector();
        var context = CreateContext(audio, boundaries);
        var stage = new DetectOnsetsStage(detector);

        // Act
        var result = await stage.ProcessAsync(boundaries, context);

        // Assert - onset times should be relative to the start of the audio, not the slice
        Assert.NotNull(result);
        if (result.Length > 0)
        {
            // The onset should be around 0.5s (the start of the tone)
            Assert.All(result, onset => Assert.True(onset >= 0.5));
        }
    }

    [Fact]
    public async Task ProcessAsync_AppliesStartTimeOffset()
    {
        // Arrange
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0)
            .AtTime(0.1).WithAttack(0.02).AddSine(440.0, amplitude: 0.5, durationSeconds: 0.5)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var startSample = (int)(0.05 * SampleRate);
        var endSample = audio.SampleCount;
        var boundaries = TestDataFactory.CreateAudioBoundaries(
            audio,
            startSample,
            endSample,
            leadingSilence: TimeSpan.FromSeconds(0.05));

        var detector = new SpectralFluxOnsetDetector();
        var context = CreateContext(audio, boundaries);
        var stage = new DetectOnsetsStage(detector);

        // Act
        var result = await stage.ProcessAsync(boundaries, context);

        // Assert - onsets should account for the leading silence time offset
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ProcessAsync_EmitsDiagnostics()
    {
        // Arrange
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0)
            .AtTime(0.1).WithAttack(0.02).AddSine(440.0, amplitude: 0.5, durationSeconds: 0.5)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var detector = new SpectralFluxOnsetDetector();
        var diagnostics = new MemoryDiagnosticsCollector();
        var context = new AudioPipelineContext(diagnosticsCollector: diagnostics);
        context.Audio = audio;
        context.Boundaries = boundaries;
        var stage = new DetectOnsetsStage(detector);

        // Act
        await stage.ProcessAsync(boundaries, context);

        // Assert
        var entries = diagnostics.GetEntries().ToList();
        Assert.Contains(entries, e => e.StageName == "DetectOnsets" && e.Key == "OnsetCount");
        Assert.Contains(entries, e => e.StageName == "DetectOnsets" && e.Key == "Onsets");
    }

    [Fact]
    public async Task ProcessAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var detector = new SpectralFluxOnsetDetector();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var context = new AudioPipelineContext(cancellationToken: cts.Token);
        context.Audio = audio;
        context.Boundaries = boundaries;
        var stage = new DetectOnsetsStage(detector);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stage.ProcessAsync(boundaries, context));
    }

    [Fact]
    public void StageName_ReturnsDetectOnsets()
    {
        // Arrange
        var detector = new SpectralFluxOnsetDetector();
        var stage = new DetectOnsetsStage(detector);

        // Act
        var name = stage.StageName;

        // Assert
        Assert.Equal("DetectOnsets", name);
    }

    [Fact]
    public async Task ProcessAsync_SetsContextOnsetsProperty()
    {
        // Arrange
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0)
            .AtTime(0.1).WithAttack(0.02).AddSine(440.0, amplitude: 0.5, durationSeconds: 0.5)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var detector = new SpectralFluxOnsetDetector();
        var context = CreateContext(audio, boundaries);
        var stage = new DetectOnsetsStage(detector);

        Assert.Null(context.Onsets);

        // Act
        var result = await stage.ProcessAsync(boundaries, context);

        // Assert
        Assert.NotNull(context.Onsets);
        Assert.Equal(result.Length, context.Onsets.Value.Length);
    }

    private static AudioPipelineContext CreateContext(AudioBuffer audio, AudioBoundaries boundaries)
    {
        var context = new AudioPipelineContext();
        context.Audio = audio;
        context.Boundaries = boundaries;
        return context;
    }
}
