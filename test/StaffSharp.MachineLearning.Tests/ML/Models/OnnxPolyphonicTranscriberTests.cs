namespace StaffSharp.MachineLearning.Tests.ML.Models;

using StaffSharp.Audio;
using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning.ML.Models;
using StaffSharp.TestHelpers.Builders;

public sealed class OnnxPolyphonicTranscriberTests
{
    [Fact]
    public void Constructor_WithNonExistentPath_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "path/that/does/not/exist.onnx";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => new OnnxTranscriber(new() { ModelPath = nonExistentPath }));
    }

    [Fact]
    public async Task Transcribe_WithValidAudio_ReturnsResult()
    {
        // Arrange
        using var transcriber = new OnnxTranscriber();

        // Create test audio (1 second at 16kHz)
        const int sampleRate = 16000;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(1.0)
            .AddSine(440.0) // A4 note
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);

        // Act
        var result = await transcriber.TranscribeAsync(PipelineProgress.Null, audio);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sampleRate, result.SampleRate);
        Assert.True(result.NumFrames > 0, "Should have at least one frame");
        Assert.Equal(88, result.PianoRoll.GetLength(1)); // 88 piano keys
        Assert.Equal(88, result.OnsetRoll.GetLength(1));
        Assert.Equal(88, result.VelocityRoll.GetLength(1));
    }

    [Fact]
    public async Task Transcribe_OutputShapesMatchInputDuration()
    {
        // Arrange
        using var transcriber = new OnnxTranscriber();

        // Create test audio (2 seconds)
        const int sampleRate = 16000;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(2.0)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);

        // Act
        var result = await transcriber.TranscribeAsync(PipelineProgress.Null, audio);

        // Assert
        // Frame rate should be sampleRate / hopSize (16000 / 512 = 31.25 fps)
        var expectedFrameRate = 16000 / 512;
        Assert.Equal(expectedFrameRate, result.FrameRate);

        // Duration should be approximately 2 seconds (within a few frames' duration)
        // Allow for edge effects in STFT processing (last few frames may be trimmed)
        var frameDuration = 1.0 / expectedFrameRate;
        var maxTolerance = frameDuration * 4;  // Allow up to 4 frames difference
        Assert.True(Math.Abs(result.DurationSeconds - 2.0) < maxTolerance,
            $"Expected duration ~2.0s, got {result.DurationSeconds:F3}s");
    }

    [Fact]
    public async Task Transcribe_OutputValuesInValidRange()
    {
        // Arrange
        using var transcriber = new OnnxTranscriber();

        // Create test audio
        const int sampleRate = 16000;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(1.0)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);

        // Act
        var result = await transcriber.TranscribeAsync(PipelineProgress.Null, audio);

        // Assert - all output values should be in [0, 1] range (probabilities)
        AssertAllValuesInRange(result.OnsetRoll, 0f, 1f, "Onset probabilities");
        AssertAllValuesInRange(result.PianoRoll, 0f, 1f, "Frame probabilities");
        AssertAllValuesInRange(result.VelocityRoll, 0f, 1f, "Velocities");
    }

    [Fact]
    public async Task Transcribe_WithDifferentSampleRate_Resamples()
    {
        // Arrange
        using var transcriber = new OnnxTranscriber();

        // Create audio at 44.1kHz (different from model's expected 16kHz)
        const int sampleRate = 44100;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(1.0)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);

        // Act
        var result = await transcriber.TranscribeAsync(PipelineProgress.Null, audio);

        // Assert - should resample and produce valid output
        Assert.NotNull(result);
        Assert.True(result.NumFrames > 0);
        Assert.Equal(16000, result.SampleRate); // Model uses 16kHz internally
    }

    [Fact]
    public async Task Transcribe_WithNullAudio_ThrowsArgumentNullException()
    {
        // Arrange
        using var transcriber = new OnnxTranscriber();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => transcriber.TranscribeAsync(PipelineProgress.Null, null!));
    }

    private static void AssertAllValuesInRange(float[,] array, float min, float max, string name)
    {
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                var value = array[i, j];
                Assert.True(value >= min && value <= max,
                    $"{name} at [{i},{j}] = {value} is outside valid range [{min}, {max}]");
            }
        }
    }
}
