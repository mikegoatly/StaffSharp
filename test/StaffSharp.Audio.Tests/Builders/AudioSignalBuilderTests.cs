namespace StaffSharp.Audio.Tests.Builders;

using StaffSharp.TestHelpers.Builders;
using Xunit;

/// <summary>
/// Tests for AudioSignalBuilder to ensure test infrastructure works correctly.
/// </summary>
public class AudioSignalBuilderTests
{
    private const int SampleRate = 44100;

    [Fact]
    public void Build_EmptyBuilder_ReturnsSilence()
    {
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.1)
            .Build();

        Assert.Equal((int)(SampleRate * 0.1), buffer.Length);
        Assert.All(buffer, sample => Assert.Equal(0f, sample));
    }

    [Fact]
    public void AddSine_GeneratesNonZeroSignal()
    {
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.1)
            .AddSine(440.0)
            .Build();

        // Should have non-zero samples
        Assert.Contains(buffer, sample => sample != 0f);

        // Should have both positive and negative samples (sine wave oscillates)
        Assert.Contains(buffer, sample => sample > 0f);
        Assert.Contains(buffer, sample => sample < 0f);
    }

    [Fact]
    public void AtTime_PlacesSignalAtCorrectTime()
    {
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.2)
            .AtTime(0.1).AddSine(440.0, durationSeconds: 0.05)
            .Build();

        var onsetSample = (int)(0.1 * SampleRate);
        var endSample = (int)(0.15 * SampleRate);

        // Before onset should be silent
        for (int i = 0; i < onsetSample - 10; i++)
        {
            Assert.Equal(0f, buffer[i]);
        }

        // During signal should be non-zero
        var hasNonZero = false;
        for (int i = onsetSample; i < endSample && i < buffer.Length; i++)
        {
            if (buffer[i] != 0f)
            {
                hasNonZero = true;
                break;
            }
        }
        Assert.True(hasNonZero, "Signal should be present at specified time");
    }

    [Fact]
    public void WithAttack_CreatesEnvelopeThatStartsAtZero()
    {
        var attackTime = 0.01;
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.1)
            .WithAttack(attackTime)
            .AddSine(440.0)
            .Build();

        // First sample should be very close to zero (attack envelope)
        Assert.True(Math.Abs(buffer[0]) < 0.01f, $"First sample should be near zero, got {buffer[0]}");

        // Samples should gradually increase during attack
        var attackSamples = (int)(attackTime * SampleRate);
        var amplitudes = new List<float>();
        for (int i = 0; i < Math.Min(attackSamples, buffer.Length); i++)
        {
            amplitudes.Add(Math.Abs(buffer[i]));
        }

        // Check that amplitude generally increases during attack period
        var avgFirstHalf = amplitudes.Take(amplitudes.Count / 2).Average();
        var avgSecondHalf = amplitudes.Skip(amplitudes.Count / 2).Average();
        Assert.True(avgSecondHalf > avgFirstHalf, "Amplitude should increase during attack");
    }

    [Fact]
    public void WithADSR_CreatesProperEnvelopeShape()
    {
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.5)
            .WithADSR(attackSeconds: 0.05, decaySeconds: 0.1, sustainLevel: 0.6, releaseSeconds: 0.1)
            .AddSine(440.0)
            .Build();

        // Attack: should start near zero
        Assert.True(Math.Abs(buffer[0]) < 0.01f, "ADSR should start near zero");

        // Attack peak: should reach near full amplitude (check a range to avoid zero crossings)
        var attackStart = (int)(0.04 * SampleRate);
        var attackEnd = (int)(0.06 * SampleRate);
        var attackPeakAmp = buffer[attackStart..attackEnd].Max(Math.Abs);
        Assert.True(attackPeakAmp > 0.8f, $"Attack peak should be high, got {attackPeakAmp}");

        // Sustain: should be lower than attack peak (check a range)
        var sustainStart = (int)(0.2 * SampleRate);
        var sustainEnd = (int)(0.25 * SampleRate);
        var sustainAmp = buffer[sustainStart..sustainEnd].Max(Math.Abs);
        Assert.True(sustainAmp < attackPeakAmp, "Sustain should be lower than attack peak");
        Assert.InRange(sustainAmp, 0.5, 0.7); // Should be around 0.6 sustain level

        // Release: should decay to near zero at end
        var endAmp = buffer[(buffer.Length - 100)..].Max(Math.Abs);
        Assert.True(endAmp < 0.3f, $"Release should decay to low amplitude, got {endAmp}");
    }

    [Fact]
    public void AddSine_MultipleCalls_SumsSignals()
    {
        var singleSine = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.1)
            .AddSine(440.0, amplitude: 0.5)
            .Build();

        var doubleSine = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.1)
            .AddSine(440.0, amplitude: 0.5)
            .AddSine(440.0, amplitude: 0.5)
            .Build();

        // Double sine should have roughly double the amplitude
        var singleMax = singleSine.Max(Math.Abs);
        var doubleMax = doubleSine.Max(Math.Abs);

        Assert.True(doubleMax > singleMax * 1.5, "Two sines should sum to higher amplitude");
    }

    [Fact]
    public void AddNoise_GeneratesRandomSignal()
    {
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.1)
            .AddNoise(amplitude: 1.0)
            .Build();

        // Should have variety of values
        var uniqueValues = buffer.Distinct().Count();
        Assert.True(uniqueValues > buffer.Length / 2, "Noise should have many unique values");

        // Should be roughly centered around zero
        var mean = buffer.Average();
        Assert.True(Math.Abs(mean) < 0.1, $"Noise mean should be near zero, got {mean}");
    }

    [Fact]
    public void AddNoise_DifferentSeeds_GeneratesDifferentSignals()
    {
        var noise1 = AudioSignalBuilder.Create()
            .WithDuration(0.1)
            .AddNoise(seed: 42)
            .Build();

        var noise2 = AudioSignalBuilder.Create()
            .WithDuration(0.1)
            .AddNoise(seed: 123)
            .Build();

        // Different seeds should produce different signals
        var differences = noise1.Zip(noise2, (a, b) => Math.Abs(a - b)).Count(d => d > 0.01f);
        Assert.True(differences > noise1.Length * 0.9, "Different seeds should produce different noise");
    }

    [Fact]
    public void AddImpulse_CreatesShortBurst()
    {
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.1)
            .AtTime(0.05)
            .AddImpulse()
            .Build();

        var impulseSample = (int)(0.05 * SampleRate);

        // Before impulse should be silent
        Assert.Equal(0f, buffer[impulseSample - 100]);

        // At impulse should have signal
        var hasSignal = false;
        for (int i = 0; i < 200 && impulseSample + i < buffer.Length; i++)
        {
            if (buffer[impulseSample + i] != 0f)
            {
                hasSignal = true;
                break;
            }
        }
        Assert.True(hasSignal, "Impulse should create non-zero samples");

        // After impulse should decay back to near-silence
        if (impulseSample + 500 < buffer.Length)
        {
            Assert.True(Math.Abs(buffer[impulseSample + 500]) < 0.1f, "Signal should decay after impulse");
        }
    }

    [Fact]
    public void AddHarmonics_GeneratesMultipleFrequencies()
    {
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.1)
            .AddHarmonics(220.0, harmonicCount: 3)
            .Build();

        // Harmonics should have higher peak amplitude than single sine
        var harmonicMax = buffer.Max(Math.Abs);
        var singleSineMax = AudioSignalBuilder.Sine(220.0, duration: 0.1, sampleRate: SampleRate).Max(Math.Abs);

        Assert.True(harmonicMax > singleSineMax, "Harmonics should sum to higher amplitude");
    }

    [Fact]
    public void MultipleSignalsAtDifferentTimes_DoNotOverlap()
    {
        var buffer = AudioSignalBuilder.Create()
            .WithSampleRate(SampleRate)
            .WithDuration(0.4)
            .AtTime(0.1).AddSine(440.0, durationSeconds: 0.05)
            .AtTime(0.2).AddSine(523.25, durationSeconds: 0.05)
            .Build();

        // Gap between signals should be silent
        var gapStart = (int)(0.16 * SampleRate);
        var gapEnd = (int)(0.19 * SampleRate);

        var gapEnergy = 0.0;
        for (int i = gapStart; i < gapEnd && i < buffer.Length; i++)
        {
            gapEnergy += Math.Abs(buffer[i]);
        }

        Assert.True(gapEnergy < 0.1, "Gap between non-overlapping signals should be silent");
    }

    [Fact]
    public void ConvenienceMethod_Sine_WorksCorrectly()
    {
        var buffer = AudioSignalBuilder.Sine(440.0, duration: 0.1, sampleRate: SampleRate);

        Assert.Equal((int)(SampleRate * 0.1), buffer.Length);
        Assert.Contains(buffer, sample => sample != 0f);
    }

    [Fact]
    public void ConvenienceMethod_Noise_WorksCorrectly()
    {
        var buffer = AudioSignalBuilder.Noise(duration: 0.1, sampleRate: SampleRate);

        Assert.Equal((int)(SampleRate * 0.1), buffer.Length);
        var uniqueValues = buffer.Distinct().Count();
        Assert.True(uniqueValues > buffer.Length / 2);
    }

    [Fact]
    public void ConvenienceMethod_Silence_WorksCorrectly()
    {
        var buffer = AudioSignalBuilder.Silence(duration: 0.1, sampleRate: SampleRate);

        Assert.Equal((int)(SampleRate * 0.1), buffer.Length);
        Assert.All(buffer, sample => Assert.Equal(0f, sample));
    }
}
