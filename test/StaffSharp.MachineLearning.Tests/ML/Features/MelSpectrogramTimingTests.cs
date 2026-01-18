namespace StaffSharp.MachineLearning.Tests.ML.Features;

using StaffSharp.Audio;
using StaffSharp.Audio.Pipeline;
using StaffSharp.MachineLearning.ML.Features;
using StaffSharp.MachineLearning.Options;
using Xunit.Abstractions;

/// <summary>
/// Tests to verify precise time alignment of mel spectrogram frames.
/// </summary>
public class MelSpectrogramTimingTests
{
    private readonly ITestOutputHelper _output;

    public MelSpectrogramTimingTests(ITestOutputHelper output)
    {
        _output = output;
    }
    /// <summary>
    /// Tests whether frame 0 is centered at time 0 (center padding)
    /// or starts at time 0 (no padding).
    /// 
    /// With center padding: Frame N represents time window centered at (N * hopSize / sampleRate)
    /// Without center padding: Frame N represents time window from (N * hopSize / sampleRate) to ((N * hopSize + frameSize) / sampleRate)
    /// 
    /// This makes a difference of frameSize/2 samples = 64ms for frameSize=2048, sampleRate=16kHz
    /// </summary>
    [Fact]
    public void ExtractFeatures_ImpulseAtTimeZero_MeasuresAlignment()
    {
        // Arrange: Create an impulse (single peak) at exactly time 0
        const int sampleRate = 16000;
        const int frameSize = 2048;
        const int hopSize = 512;
        
        var options = new MelSpectrogramOptions
        {
            SampleRate = sampleRate,
            FrameSize = frameSize,
            HopSize = hopSize
        };

        // Create 2 seconds of audio with single impulse at sample 0
        var samples = new float[sampleRate * 2];
        samples[0] = 1.0f; // Impulse at t=0

        var audio = new AudioBuffer(samples, sampleRate, channels: 1);
        var extractor = new MelSpectrogramExtractor(options);

        // Act
        var melSpec = extractor.ExtractFeatures(PipelineProgress.Null, audio);

        // Assert: Find which frame has the strongest energy
        var numFrames = melSpec.GetLength(0);
        var numBins = melSpec.GetLength(1);
        
        var frameEnergies = new float[numFrames];
        for (int frame = 0; frame < numFrames; frame++)
        {
            float energy = 0;
            for (int bin = 0; bin < numBins; bin++)
            {
                energy += melSpec[frame, bin];
            }
            frameEnergies[frame] = energy;
        }

        var peakFrame = Array.IndexOf(frameEnergies, frameEnergies.Max());

        // Without center padding: impulse at t=0 appears in frame 0
        // With center padding: impulse at t=0 appears in frame (frameSize/2)/hopSize = 1024/512 = 2
        
        var frameRate = (float)sampleRate / hopSize;
        var peakTime = peakFrame / frameRate;
        
        // Output for analysis
        var message = $"Impulse at t=0 detected at frame {peakFrame} (time={peakTime * 1000:F2}ms). " +
                     $"Expected: frame 0 (no padding) or frame 2 (with 64ms center padding). " +
                     $"Frame energies: [{string.Join(", ", frameEnergies.Take(5).Select(e => $"{e:F2}"))}]";
        
        _output.WriteLine(message);
        
        // The test just documents the behavior
        Assert.True(peakFrame <= 2, message);
    }

    /// <summary>
    /// Tests time alignment by measuring delay between an impulse and its detection.
    /// </summary>
    [Fact]
    public void ExtractFeatures_ImpulseAtKnownTime_MeasuresAlignment()
    {
        // Arrange: Create impulse at exactly 1.0 second
        const int sampleRate = 16000;
        const int frameSize = 2048;
        const int hopSize = 512;
        const double impulseTime = 1.0; // 1 second
        
        var options = new MelSpectrogramOptions
        {
            SampleRate = sampleRate,
            FrameSize = frameSize,
            HopSize = hopSize
        };

        var samples = new float[sampleRate * 3]; // 3 seconds
        var impulseSample = (int)(impulseTime * sampleRate);
        samples[impulseSample] = 1.0f;

        var audio = new AudioBuffer(samples, sampleRate, channels: 1);
        var extractor = new MelSpectrogramExtractor(options);

        // Act
        var melSpec = extractor.ExtractFeatures(PipelineProgress.Null, audio);
        
        // Find peak frame
        var numFrames = melSpec.GetLength(0);
        var numBins = melSpec.GetLength(1);
        
        var frameEnergies = new float[numFrames];
        for (int frame = 0; frame < numFrames; frame++)
        {
            float energy = 0;
            for (int bin = 0; bin < numBins; bin++)
            {
                energy += melSpec[frame, bin];
            }
            frameEnergies[frame] = energy;
        }

        var peakFrame = Array.IndexOf(frameEnergies, frameEnergies.Max());
        
        // Calculate frame rate and expected frame
        var frameRate = (float)sampleRate / hopSize;
        var actualTime = peakFrame / frameRate;
        var timingError = actualTime - impulseTime;
        
        // Without center padding: expected frame = impulseTime * frameRate
        var expectedFrameNoPadding = (int)Math.Round(impulseTime * frameRate);
        
        // With center padding: frames are shifted by frameSize/2 samples
        var paddingDelay = (frameSize / 2.0) / sampleRate;
        var expectedFrameWithPadding = (int)Math.Round((impulseTime + paddingDelay) * frameRate);

        // Output diagnostic information for analysis
        var message = $"Impulse at {impulseTime:F4}s detected at frame {peakFrame} (time={actualTime:F4}s). " +
                     $"Timing error: {timingError * 1000:F2}ms. " +
                     $"Expected: frame {expectedFrameNoPadding} (no padding) or frame {expectedFrameWithPadding} (with padding). " +
                     $"Padding delay would be {paddingDelay * 1000:F2}ms.";
        
        _output.WriteLine(message);
        
        // Test passes if timing error is reasonable (within a few frames)
        Assert.True(Math.Abs(timingError) < 0.2, 
            $"{message} Timing error exceeds 200ms threshold!");
    }

    /// <summary>
    /// Tests the exact timing relationship by creating a tone burst with known onset.
    /// This is more realistic than an impulse and better represents musical note onsets.
    /// </summary>
    [Fact]
    public void ExtractFeatures_ToneBurstAtKnownTime_MeasuresTimingOffset()
    {
        // Arrange: Create a tone burst (440 Hz) starting at exactly 0.5 seconds
        const int sampleRate = 16000;
        const int frameSize = 2048;
        const int hopSize = 512;
        const double onsetTime = 0.5; // 0.5 seconds
        const double frequency = 440.0; // A4
        const double duration = 0.1; // 100ms burst
        
        var options = new MelSpectrogramOptions
        {
            SampleRate = sampleRate,
            FrameSize = frameSize,
            HopSize = hopSize
        };

        var samples = new float[sampleRate * 2]; // 2 seconds total
        var onsetSample = (int)(onsetTime * sampleRate);
        var durationSamples = (int)(duration * sampleRate);
        
        // Generate tone burst with smooth envelope to avoid clicks
        for (int i = 0; i < durationSamples; i++)
        {
            var t = i / (double)sampleRate;
            var phase = 2.0 * Math.PI * frequency * t;
            
            // Apply Hann window envelope
            var envelope = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / durationSamples));
            
            samples[onsetSample + i] = (float)(0.8 * envelope * Math.Sin(phase));
        }

        var audio = new AudioBuffer(samples, sampleRate, channels: 1);
        var extractor = new MelSpectrogramExtractor(options);

        // Act
        var melSpec = extractor.ExtractFeatures(PipelineProgress.Null, audio);
        
        // Find onset frame: first frame where energy significantly increases
        var numFrames = melSpec.GetLength(0);
        var numBins = melSpec.GetLength(1);
        
        var frameEnergies = new float[numFrames];
        for (int frame = 0; frame < numFrames; frame++)
        {
            float energy = 0;
            for (int bin = 0; bin < numBins; bin++)
            {
                energy += melSpec[frame, bin];
            }
            frameEnergies[frame] = energy;
        }
        
        // Find first frame above 20% of peak energy (onset detection)
        var peakEnergy = frameEnergies.Max();
        var threshold = peakEnergy * 0.2f;
        var onsetFrame = -1;
        
        for (int frame = 1; frame < numFrames; frame++)
        {
            if (frameEnergies[frame] > threshold && frameEnergies[frame - 1] <= threshold)
            {
                onsetFrame = frame;
                break;
            }
        }
        
        Assert.True(onsetFrame >= 0, "Could not detect onset in mel spectrogram");
        
        // Calculate timing
        var frameRate = (float)sampleRate / hopSize;
        var detectedTime = onsetFrame / frameRate;
        var timingError = detectedTime - onsetTime;
        
        // Expected shift without center padding: ~0 ms
        // Expected shift with center padding: frameSize/2 = 1024 samples = 64ms
        var expectedShiftWithPadding = (frameSize / 2.0) / sampleRate;
        
        // Output diagnostic info
        var message = $"Onset at {onsetTime:F4}s detected at frame {onsetFrame} (time={detectedTime:F4}s). " +
                     $"Timing error: {timingError * 1000:F2}ms. " +
                     $"Expected shift with center padding: {expectedShiftWithPadding * 1000:F2}ms";
        
        // The test passes either way, but outputs the actual timing for analysis
        _output.WriteLine(message);
        
        Assert.True(Math.Abs(timingError) < 0.1, 
            $"{message}. Timing error exceeds 100ms threshold!");
    }
}
