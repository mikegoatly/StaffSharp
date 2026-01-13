namespace StaffSharp.MachineLearning.Tests.ML.Models;

using StaffSharp.Audio;
using StaffSharp.MachineLearning.ML.Models;
using StaffSharp.MachineLearning.Options;
using StaffSharp.TestHelpers.Builders;

public sealed class OnnxPolyphonicTranscriberTests
{
    private const string TestModelPath = "TestData/test_onsets_frames.onnx";

    [Fact]
    public void Constructor_WithNullModelPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OnnxPolyphonicTranscriber((string)null!));
    }

    [Fact]
    public void Constructor_WithEmptyModelPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OnnxPolyphonicTranscriber(string.Empty));
    }

    [Fact]
    public void Constructor_WithNonExistentPath_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "path/that/does/not/exist.onnx";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => new OnnxPolyphonicTranscriber(nonExistentPath));
    }

    [Fact(Skip = "Requires test ONNX model - run: python training/scripts/create_test_model.py")]
    public void Constructor_WithValidModelPath_Succeeds()
    {
        // Arrange
        var modelPath = GetTestModelPath();
        SkipIfModelNotAvailable(modelPath);

        // Act
        using var transcriber = new OnnxPolyphonicTranscriber(modelPath);

        // Assert - no exception thrown
        Assert.NotNull(transcriber);
    }

    [Fact(Skip = "Requires test ONNX model - run: python training/scripts/create_test_model.py")]
    public void Constructor_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var modelPath = GetTestModelPath();
        SkipIfModelNotAvailable(modelPath);

        var options = new PolyphonicTranscriptionOptions
        {
            ModelPath = modelPath,
            OnsetThreshold = 0.7f,
            FrameThreshold = 0.6f,
            MinNoteLengthSeconds = 0.1f
        };

        // Act
        using var transcriber = new OnnxPolyphonicTranscriber(options);

        // Assert - no exception thrown
        Assert.NotNull(transcriber);
    }

    [Fact(Skip = "Requires test ONNX model - run: python training/scripts/create_test_model.py")]
    public void Transcribe_WithValidAudio_ReturnsResult()
    {
        // Arrange
        var modelPath = GetTestModelPath();
        SkipIfModelNotAvailable(modelPath);

        using var transcriber = new OnnxPolyphonicTranscriber(modelPath);

        // Create test audio (1 second at 16kHz)
        const int sampleRate = 16000;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(1.0)
            .AddSine(440.0) // A4 note
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);

        // Act
        var result = transcriber.Transcribe(audio);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sampleRate, result.SampleRate);
        Assert.True(result.NumFrames > 0, "Should have at least one frame");
        Assert.Equal(88, result.PianoRoll.GetLength(1)); // 88 piano keys
        Assert.Equal(88, result.OnsetRoll.GetLength(1));
        Assert.Equal(88, result.VelocityRoll.GetLength(1));
    }

    [Fact(Skip = "Requires test ONNX model - run: python training/scripts/create_test_model.py")]
    public void Transcribe_OutputShapesMatchInputDuration()
    {
        // Arrange
        var modelPath = GetTestModelPath();
        SkipIfModelNotAvailable(modelPath);

        using var transcriber = new OnnxPolyphonicTranscriber(modelPath);

        // Create test audio (2 seconds)
        const int sampleRate = 16000;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(2.0)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);

        // Act
        var result = transcriber.Transcribe(audio);

        // Assert
        // Frame rate should be sampleRate / hopSize (16000 / 512 = 31.25 fps)
        var expectedFrameRate = 16000 / 512;
        Assert.Equal(expectedFrameRate, result.FrameRate);

        // Duration should be approximately 2 seconds (within a frame's duration)
        var frameDuration = 1.0 / expectedFrameRate;
        Assert.True(Math.Abs(result.DurationSeconds - 2.0) < frameDuration,
            $"Expected duration ~2.0s, got {result.DurationSeconds:F3}s");
    }

    [Fact(Skip = "Requires test ONNX model - run: python training/scripts/create_test_model.py")]
    public void Transcribe_OutputValuesInValidRange()
    {
        // Arrange
        var modelPath = GetTestModelPath();
        SkipIfModelNotAvailable(modelPath);

        using var transcriber = new OnnxPolyphonicTranscriber(modelPath);

        // Create test audio
        const int sampleRate = 16000;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(1.0)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);

        // Act
        var result = transcriber.Transcribe(audio);

        // Assert - all output values should be in [0, 1] range (probabilities)
        AssertAllValuesInRange(result.OnsetRoll, 0f, 1f, "Onset probabilities");
        AssertAllValuesInRange(result.PianoRoll, 0f, 1f, "Frame probabilities");
        AssertAllValuesInRange(result.VelocityRoll, 0f, 1f, "Velocities");
    }

    [Fact(Skip = "Requires test ONNX model - run: python training/scripts/create_test_model.py")]
    public void Transcribe_WithStereoAudio_ConvertsToMono()
    {
        // Arrange
        var modelPath = GetTestModelPath();
        SkipIfModelNotAvailable(modelPath);

        using var transcriber = new OnnxPolyphonicTranscriber(modelPath);

        // Create stereo audio
        var samples = new float[32000]; // 2 seconds at 16kHz
        for (int i = 0; i < samples.Length; i += 2)
        {
            var t = (float)i / 16000;
            var value = MathF.Sin(2 * MathF.PI * 440 * t);
            samples[i] = value;     // Left channel
            samples[i + 1] = value; // Right channel
        }
        var audio = new AudioBuffer(samples, sampleRate: 16000, channels: 2);

        // Act
        var result = transcriber.Transcribe(audio);

        // Assert - should handle stereo audio without errors
        Assert.NotNull(result);
        Assert.True(result.NumFrames > 0);
    }

    [Fact(Skip = "Requires test ONNX model - run: python training/scripts/create_test_model.py")]
    public void Transcribe_WithDifferentSampleRate_Resamples()
    {
        // Arrange
        var modelPath = GetTestModelPath();
        SkipIfModelNotAvailable(modelPath);

        using var transcriber = new OnnxPolyphonicTranscriber(modelPath);

        // Create audio at 44.1kHz (different from model's expected 16kHz)
        const int sampleRate = 44100;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(1.0)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);

        // Act
        var result = transcriber.Transcribe(audio);

        // Assert - should resample and produce valid output
        Assert.NotNull(result);
        Assert.True(result.NumFrames > 0);
        Assert.Equal(16000, result.SampleRate); // Model uses 16kHz internally
    }

    [Fact(Skip = "Requires test ONNX model - run: python training/scripts/create_test_model.py")]
    public void Transcribe_WithNullAudio_ThrowsArgumentNullException()
    {
        // Arrange
        var modelPath = GetTestModelPath();
        SkipIfModelNotAvailable(modelPath);

        using var transcriber = new OnnxPolyphonicTranscriber(modelPath);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => transcriber.Transcribe(null!));
    }

    [Fact(Skip = "Requires test ONNX model - run: python training/scripts/create_test_model.py")]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var modelPath = GetTestModelPath();
        SkipIfModelNotAvailable(modelPath);

        var transcriber = new OnnxPolyphonicTranscriber(modelPath);

        // Act - dispose multiple times
        transcriber.Dispose();
        transcriber.Dispose();
        transcriber.Dispose();

        // Assert - no exception thrown
    }

    [Fact(Skip = "Requires test ONNX model - run: python training/scripts/create_test_model.py")]
    public void Transcribe_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var modelPath = GetTestModelPath();
        SkipIfModelNotAvailable(modelPath);

        var transcriber = new OnnxPolyphonicTranscriber(modelPath);
        transcriber.Dispose();

        // Create test audio
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(16000)
            .WithDuration(1.0)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, 16000, channels: 1);

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => transcriber.Transcribe(audio));
    }

    #region Helper Methods

    private static string GetTestModelPath()
    {
        // Try multiple possible locations for the test model
        var possiblePaths = new[]
        {
            TestModelPath,
            Path.Combine("..", "..", "..", "..", TestModelPath),
            Path.Combine(Environment.CurrentDirectory, TestModelPath)
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        // Return the default path (tests will be skipped if not found)
        return Path.GetFullPath(TestModelPath);
    }

    private static void SkipIfModelNotAvailable(string modelPath)
    {
        if (!File.Exists(modelPath))
        {
            // Model not available - test will be skipped via Skip attribute
            // This method is here to document the requirement
            Assert.True(File.Exists(modelPath),
                $"Test model not found at: {modelPath}\n" +
                "Run: python training/scripts/create_test_model.py --output test/StaffSharp.MachineLearning.Tests/TestData/test_onsets_frames.onnx");
        }
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

    #endregion
}
