using StaffSharp;
using StaffSharp.Audio;
using StaffSharp.Audio.Analysis.Boundaries;
using StaffSharp.Audio.Analysis.Pitch;
using StaffSharp.Audio.Pipeline;
using StaffSharp.Audio.Pipeline.Stages;
using StaffSharp.TestHelpers.Builders;

namespace StaffSharp.Audio.Tests.Pipeline.Stages;

/// <summary>
/// Unit tests for DetectPitchesStage.
/// </summary>
public sealed class DetectPitchesStageTests
{
    private const int SampleRate = 44100;

    [Fact]
    public async Task ProcessAsync_ValidInput_DetectsPitches()
    {
        // Arrange
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0)
            .AtTime(0.1).WithAttack(0.02).AddSine(440.0, amplitude: 0.5, durationSeconds: 0.3)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var onsets = new double[] { 0.1 };
        var detector = new YinPitchDetector();
        var context = CreateContext(audio, boundaries, onsets);
        var stage = new DetectPitchesStage(detector);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(onsets.Length, result.Length);
        Assert.NotNull(context.Pitches);
    }

    [Fact]
    public async Task ProcessAsync_NullDetector_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DetectPitchesStage(null!));
    }

    [Fact]
    public async Task ProcessAsync_AudioNotInContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var onsets = new double[] { 0.1 };
        var detector = new YinPitchDetector();
        var context = new AudioPipelineContext(); // No audio in context
        var stage = new DetectPitchesStage(detector);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stage.ProcessAsync(onsets, context));
        Assert.Contains("Audio buffer not available in context", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_BoundariesNotInContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        var audio = new AudioBuffer(samples, SampleRate, 1);
        var onsets = new double[] { 0.1 };
        var detector = new YinPitchDetector();
        var context = new AudioPipelineContext();
        context.Audio = audio; // No boundaries in context
        var stage = new DetectPitchesStage(detector);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await stage.ProcessAsync(onsets, context));
        Assert.Contains("Audio boundaries not available in context", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_PitchedNote_DetectsPitch()
    {
        // Arrange: A4 (440 Hz) should be detected as MIDI note 69
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0)
            .AtTime(0.1).AddHarmonics(440.0, harmonicCount: 5, amplitude: 0.5, durationSeconds: 0.5)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var onsets = new double[] { 0.1 };
        var detector = new YinPitchDetector();
        var context = CreateContext(audio, boundaries, onsets);
        var stage = new DetectPitchesStage(detector);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.Single(result);
        var expectedMidi = MidiNote.FromFrequency(440.0).MidiNumber;
        Assert.InRange(result[0], expectedMidi - 1, expectedMidi + 1); // Allow ±1 semitone tolerance
    }

    [Fact]
    public async Task ProcessAsync_UnpitchedSound_ReturnsUnpitchedSentinel()
    {
        // Arrange: Pure noise is unpitched
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0)
            .AtTime(0.1).AddNoiseAt(0.1, 0.3, amplitude: 0.5)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var onsets = new double[] { 0.1 };
        var detector = new YinPitchDetector();
        var context = CreateContext(audio, boundaries, onsets);
        var stage = new DetectPitchesStage(detector);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.Single(result);
        Assert.Equal(DetectPitchesStage.UnpitchedSentinel, result[0]);
    }

    [Fact]
    public async Task ProcessAsync_OnsetOutsideBoundaries_ReturnsUnpitchedSentinel()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, (int)(0.3 * SampleRate)); // Only first 0.3s
        var onsets = new double[] { 0.4 }; // Onset outside boundaries
        var detector = new YinPitchDetector();
        var context = CreateContext(audio, boundaries, onsets);
        var stage = new DetectPitchesStage(detector);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.Single(result);
        Assert.Equal(DetectPitchesStage.UnpitchedSentinel, result[0]);
    }

    [Fact]
    public async Task ProcessAsync_MultipleOnsets_DetectsMultiplePitches()
    {
        // Arrange
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.5)
            .AtTime(0.1).AddHarmonics(261.63, harmonicCount: 5, amplitude: 0.5, durationSeconds: 0.3) // C4
            .AtTime(0.5).AddHarmonics(329.63, harmonicCount: 5, amplitude: 0.5, durationSeconds: 0.3) // E4
            .AtTime(0.9).AddHarmonics(392.00, harmonicCount: 5, amplitude: 0.5, durationSeconds: 0.3) // G4
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var onsets = new double[] { 0.1, 0.5, 0.9 };
        var detector = new YinPitchDetector();
        var context = CreateContext(audio, boundaries, onsets);
        var stage = new DetectPitchesStage(detector);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.Equal(3, result.Length);
        Assert.All(result, pitch => Assert.NotEqual(DetectPitchesStage.UnpitchedSentinel, pitch));
    }

    [Fact]
    public async Task ProcessAsync_EmitsDiagnostics()
    {
        // Arrange
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0)
            .AtTime(0.1).AddSine(440.0, amplitude: 0.5, durationSeconds: 0.5)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var onsets = new double[] { 0.1 };
        var detector = new YinPitchDetector();
        var diagnostics = new MemoryDiagnosticsCollector();
        var context = new AudioPipelineContext(diagnosticsCollector: diagnostics);
        context.Audio = audio;
        context.Boundaries = boundaries;
        context.Onsets = onsets;
        var stage = new DetectPitchesStage(detector);

        // Act
        await stage.ProcessAsync(onsets, context);

        // Assert
        var entries = diagnostics.GetEntries().ToList();
        Assert.Contains(entries, e => e.StageName == "DetectPitches" && e.Key == "PitchCount");
        Assert.Contains(entries, e => e.StageName == "DetectPitches" && e.Key == "Pitches");
    }

    [Fact]
    public async Task ProcessAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var onsets = new double[] { 0.1 };
        var detector = new YinPitchDetector();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var context = new AudioPipelineContext(cancellationToken: cts.Token);
        context.Audio = audio;
        context.Boundaries = boundaries;
        context.Onsets = onsets;
        var stage = new DetectPitchesStage(detector);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await stage.ProcessAsync(onsets, context));
    }

    [Fact]
    public void StageName_ReturnsDetectPitches()
    {
        // Arrange
        var detector = new YinPitchDetector();
        var stage = new DetectPitchesStage(detector);

        // Act
        var name = stage.StageName;

        // Assert
        Assert.Equal("DetectPitches", name);
    }

    [Fact]
    public async Task ProcessAsync_SetsContextPitchesProperty()
    {
        // Arrange
        var samples = AudioSignalBuilder.Sine(440.0, duration: 0.5, sampleRate: SampleRate);
        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var onsets = new double[] { 0.1 };
        var detector = new YinPitchDetector();
        var context = CreateContext(audio, boundaries, onsets);
        var stage = new DetectPitchesStage(detector);

        Assert.Null(context.Pitches);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.NotNull(context.Pitches);
        Assert.Equal(result.Length, context.Pitches.Value.Length);
    }

    [Fact]
    public async Task ProcessAsync_WithCustomMaxDegreeOfParallelism_UsesSpecifiedValue()
    {
        // Arrange
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(1.0)
            .AtTime(0.1).AddSine(440.0, amplitude: 0.5, durationSeconds: 0.2)
            .AtTime(0.4).AddSine(523.25, amplitude: 0.5, durationSeconds: 0.2)
            .Build();

        var audio = new AudioBuffer(samples, SampleRate, 1);
        var boundaries = TestDataFactory.CreateAudioBoundaries(audio, 0, audio.SampleCount);
        var onsets = new double[] { 0.1, 0.4 };
        var detector = new YinPitchDetector();
        var context = CreateContext(audio, boundaries, onsets);
        var stage = new DetectPitchesStage(detector, maxDegreeOfParallelism: 1);

        // Act
        var result = await stage.ProcessAsync(onsets, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
    }

    private static AudioPipelineContext CreateContext(AudioBuffer audio, AudioBoundaries boundaries, double[] onsets)
    {
        var context = new AudioPipelineContext();
        context.Audio = audio;
        context.Boundaries = boundaries;
        context.Onsets = onsets;
        return context;
    }
}
