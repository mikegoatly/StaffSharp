namespace StaffSharp.MachineLearning.Tests.ML.Features;

using StaffSharp.Audio;
using StaffSharp.MachineLearning.ML.Features;
using StaffSharp.MachineLearning.Options;
using StaffSharp.TestHelpers.Builders;

public sealed class MelSpectrogramExtractorTests
{
    [Fact]
    public void Constructor_WithDefaultOptions_Succeeds()
    {
        // Act
        var extractor = new MelSpectrogramExtractor();

        // Assert - no exception thrown
        Assert.NotNull(extractor);
    }

    [Fact]
    public void Constructor_WithCustomOptions_Succeeds()
    {
        // Arrange
        var options = new MelSpectrogramOptions
        {
            SampleRate = 22050,
            FrameSize = 1024,
            HopSize = 256,
            MelBins = 128
        };

        // Act
        var extractor = new MelSpectrogramExtractor(options);

        // Assert - no exception thrown
        Assert.NotNull(extractor);
    }

    [Fact]
    public void ExtractFeatures_WithSineWave_ProducesExpectedShape()
    {
        // Arrange
        const int sampleRate = 16000;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(1.0)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);
        var extractor = new MelSpectrogramExtractor();

        // Act
        var features = extractor.ExtractFeatures(audio);

        // Assert
        Assert.Equal(229, features.GetLength(1)); // Mel bins
        Assert.True(features.GetLength(0) > 0); // Time frames

        // Expected frames = (samples - frameSize) / hopSize + 1
        // (16000 - 2048) / 512 + 1 = 27.25... = 27 frames (integer division)
        var expectedFrames = (audio.SampleCount - 2048) / 512 + 1;
        Assert.Equal(expectedFrames, features.GetLength(0));
    }

    [Fact]
    public void ExtractFeatures_WithDifferentSampleRate_Resamples()
    {
        // Arrange - create audio at 44.1kHz
        const int sampleRate = 44100;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(1.0)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);
        var extractor = new MelSpectrogramExtractor();

        // Act
        var features = extractor.ExtractFeatures(audio);

        // Assert - should still work and produce valid features
        Assert.Equal(229, features.GetLength(1)); // Mel bins
        Assert.True(features.GetLength(0) > 0); // Time frames
    }

    [Fact]
    public void ExtractFeatures_WithStereoAudio_ConvertsToMono()
    {
        // Arrange - create stereo audio
        var samples = new float[32000]; // 2 seconds at 16kHz
        for (int i = 0; i < samples.Length; i += 2)
        {
            var t = (float)i / 16000;
            var value = MathF.Sin(2 * MathF.PI * 440 * t);
            samples[i] = value;     // Left channel
            samples[i + 1] = value; // Right channel
        }
        var audio = new AudioBuffer(samples, sampleRate: 16000, channels: 2);
        var extractor = new MelSpectrogramExtractor();

        // Act
        var features = extractor.ExtractFeatures(audio);

        // Assert - should work without error
        Assert.Equal(229, features.GetLength(1));
        Assert.True(features.GetLength(0) > 0);
    }

    [Fact]
    public void ExtractFeatures_WithShortAudio_ThrowsException()
    {
        // Arrange - create audio shorter than frame size
        const int sampleRate = 16000;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(0.05)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);
        var extractor = new MelSpectrogramExtractor();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => extractor.ExtractFeatures(audio));
    }

    [Fact]
    public void ExtractFeatures_WithSilence_ProducesValidFeatures()
    {
        // Arrange - create silence
        var samples = new float[32000]; // 2 seconds at 16kHz, all zeros
        var audio = new AudioBuffer(samples, sampleRate: 16000, channels: 1);
        var extractor = new MelSpectrogramExtractor();

        // Act
        var features = extractor.ExtractFeatures(audio);

        // Assert
        Assert.Equal(229, features.GetLength(1));
        Assert.True(features.GetLength(0) > 0);

        // For silence, all mel bins should have low values (close to log(1) = 0)
        var maxValue = 0f;
        for (int t = 0; t < features.GetLength(0); t++)
        {
            for (int m = 0; m < features.GetLength(1); m++)
            {
                maxValue = Math.Max(maxValue, features[t, m]);
            }
        }

        // Log(1 + 10000 * 0) = 0, so max should be very small
        Assert.True(maxValue < 1.0f, "Silence should produce very low feature values");
    }

    [Fact]
    public void ExtractFeatures_WithMultipleFrequencies_ProducesDistinctPatterns()
    {
        // Arrange - create two tones at different frequencies
        const int sampleRate = 16000;
        var lowFreqSamples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(1.0)
            .AddSine(100.0)
            .Build();
        var highFreqSamples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(1.0)
            .AddSine(2000.0)
            .Build();
        var lowFreq = new AudioBuffer(lowFreqSamples, sampleRate, channels: 1);
        var highFreq = new AudioBuffer(highFreqSamples, sampleRate, channels: 1);
        var extractor = new MelSpectrogramExtractor();

        // Act
        var lowFeatures = extractor.ExtractFeatures(lowFreq);
        var highFeatures = extractor.ExtractFeatures(highFreq);

        // Assert - features should be different
        // Calculate total energy difference
        var totalDifference = 0.0;
        for (int t = 0; t < Math.Min(lowFeatures.GetLength(0), highFeatures.GetLength(0)); t++)
        {
            for (int m = 0; m < 229; m++)
            {
                totalDifference += Math.Abs(lowFeatures[t, m] - highFeatures[t, m]);
            }
        }

        Assert.True(totalDifference > 100, "Different frequencies should produce distinct features");
    }

    [Fact]
    public void ExtractFeatures_ProducesPositiveValues()
    {
        // Arrange
        const int sampleRate = 16000;
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(1.0)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);
        var extractor = new MelSpectrogramExtractor();

        // Act
        var features = extractor.ExtractFeatures(audio);

        // Assert - all values should be non-negative (log compression always positive)
        for (int t = 0; t < features.GetLength(0); t++)
        {
            for (int m = 0; m < features.GetLength(1); m++)
            {
                Assert.True(features[t, m] >= 0,
                    $"Feature at [{t},{m}] should be non-negative, got {features[t, m]}");
            }
        }
    }

    [Theory]
    [InlineData(16000, 2048, 512, 229)]
    [InlineData(22050, 2048, 512, 128)]
    [InlineData(16000, 1024, 256, 229)]
    public void ExtractFeatures_WithVariousOptions_ProducesCorrectShape(
        int sampleRate, int frameSize, int hopSize, int melBins)
    {
        // Arrange
        var options = new MelSpectrogramOptions
        {
            SampleRate = sampleRate,
            FrameSize = frameSize,
            HopSize = hopSize,
            MelBins = melBins
        };
        var samples = AudioSignalBuilder.Create()
            .WithSampleRate(sampleRate)
            .WithDuration(2.0)
            .AddSine(440.0)
            .Build();
        var audio = new AudioBuffer(samples, sampleRate, channels: 1);
        var extractor = new MelSpectrogramExtractor(options);

        // Act
        var features = extractor.ExtractFeatures(audio);

        // Assert
        Assert.Equal(melBins, features.GetLength(1));
        Assert.True(features.GetLength(0) > 0);
    }
}
