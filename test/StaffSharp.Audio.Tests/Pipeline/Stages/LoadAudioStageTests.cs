using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Audio.Tests.Pipeline.Stages;

/// <summary>
/// Unit tests for LoadAudioStage.
/// </summary>
public sealed class LoadAudioStageTests
{
    private const int SampleRate = 44100;

    [Fact]
    public async Task ProcessAsync_ValidWavStream_LoadsAudio()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        using var stream = WavStreamBuilder.FromSamples(samples, SampleRate);
        var context = new AudioPipelineContext();
        var stage = new LoadAudioStage();

        // Act
        var result = await stage.ProcessAsync(stream, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SampleRate, result.SampleRate);
        Assert.Equal(1, result.Channels);
        Assert.True(result.SampleCount > 0);
        Assert.NotNull(context.Audio);
        Assert.Same(result, context.Audio);
    }

    [Fact]
    public async Task ProcessAsync_MonoAudio_LoadsCorrectly()
    {
        // Arrange
        var samples = AudioSignalBuilder.Silence(duration: 0.1, sampleRate: SampleRate);
        using var stream = WavStreamBuilder.FromSamples(samples, SampleRate, channels: 1);
        var context = new AudioPipelineContext();
        var stage = new LoadAudioStage();

        // Act
        var result = await stage.ProcessAsync(stream, context);

        // Assert
        Assert.Equal(1, result.Channels);
    }

    [Fact]
    public async Task ProcessAsync_EmitsDiagnostics()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        using var stream = WavStreamBuilder.FromSamples(samples, SampleRate);
        var diagnostics = new MemoryDiagnosticsCollector();
        var context = new AudioPipelineContext(diagnosticsCollector: diagnostics);
        var stage = new LoadAudioStage();

        // Act
        await stage.ProcessAsync(stream, context);

        // Assert
        var entries = diagnostics.GetEntries().ToList();
        Assert.Contains(entries, e => e.StageName == "LoadAudio" && e.Key == "SampleRate");
        Assert.Contains(entries, e => e.StageName == "LoadAudio" && e.Key == "Channels");
        Assert.Contains(entries, e => e.StageName == "LoadAudio" && e.Key == "DurationSeconds");
        Assert.Contains(entries, e => e.StageName == "LoadAudio" && e.Key == "SampleCount");
    }

    [Fact]
    public async Task ProcessAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        using var stream = WavStreamBuilder.FromSamples(samples, SampleRate);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var context = new AudioPipelineContext(cancellationToken: cts.Token);
        var stage = new LoadAudioStage();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stage.ProcessAsync(stream, context));
    }

    [Fact]
    public void StageName_ReturnsLoadAudio()
    {
        // Arrange
        var stage = new LoadAudioStage();

        // Act
        var name = stage.StageName;

        // Assert
        Assert.Equal("LoadAudio", name);
    }

    [Fact]
    public async Task ProcessAsync_DifferentSampleRates_LoadsCorrectly()
    {
        // Arrange
        var sampleRates = new[] { 22050, 44100, 48000 };
        var stage = new LoadAudioStage();

        foreach (var sr in sampleRates)
        {
            var samples = AudioSignalBuilder.Sine(440.0, duration: 0.1, sampleRate: sr);
            using var stream = WavStreamBuilder.FromSamples(samples, sr);
            var context = new AudioPipelineContext();

            // Act
            var result = await stage.ProcessAsync(stream, context);

            // Assert
            Assert.Equal(sr, result.SampleRate);
        }
    }

    [Fact]
    public async Task ProcessAsync_SetsContextAudioProperty()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.1, sampleRate: SampleRate);
        using var stream = WavStreamBuilder.FromSamples(samples, SampleRate);
        var context = new AudioPipelineContext();
        var stage = new LoadAudioStage();

        Assert.Null(context.Audio);

        // Act
        var result = await stage.ProcessAsync(stream, context);

        // Assert
        Assert.NotNull(context.Audio);
        Assert.Same(result, context.Audio);
    }
}
