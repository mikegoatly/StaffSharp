using System.Numerics.Tensors;

namespace StaffSharp.TestHelpers.Builders;

/// <summary>
/// Fluent builder for creating test audio signals.
/// </summary>
public sealed class AudioSignalBuilder
{
    private readonly List<SignalComponent> _components = [];
    private int _sampleRate = 44100;
    private double _duration = 0.1; // 100ms default
    private int? _fixedLength;
    private double _currentTimeOffset;
    private IEnvelope? _currentEnvelope;

    private AudioSignalBuilder()
    {
    }

    /// <summary>
    /// Creates a new audio signal builder.
    /// </summary>
    public static AudioSignalBuilder Create() => new();

    /// <summary>
    /// Sets the sample rate for the signal.
    /// </summary>
    public AudioSignalBuilder WithSampleRate(int sampleRate)
    {
        _sampleRate = sampleRate;
        return this;
    }

    /// <summary>
    /// Sets the duration in seconds.
    /// </summary>
    public AudioSignalBuilder WithDuration(double durationSeconds)
    {
        _duration = durationSeconds;
        _fixedLength = null;
        return this;
    }

    /// <summary>
    /// Sets an exact sample count instead of duration.
    /// </summary>
    public AudioSignalBuilder WithSampleCount(int sampleCount)
    {
        _fixedLength = sampleCount;
        return this;
    }

    /// <summary>
    /// Sets the time offset for the next component(s) to be added.
    /// </summary>
    public AudioSignalBuilder AtTime(double timeSeconds)
    {
        _currentTimeOffset = timeSeconds;
        return this;
    }

    /// <summary>
    /// Applies an attack envelope to the next component(s).
    /// </summary>
    public AudioSignalBuilder WithAttack(double attackSeconds)
    {
        _currentEnvelope = new AttackEnvelope(attackSeconds);
        return this;
    }

    /// <summary>
    /// Applies an ADSR envelope to the next component(s).
    /// </summary>
    public AudioSignalBuilder WithADSR(double attackSeconds, double decaySeconds, double sustainLevel, double releaseSeconds)
    {
        _currentEnvelope = new ADSREnvelope(attackSeconds, decaySeconds, sustainLevel, releaseSeconds);
        return this;
    }

    /// <summary>
    /// Adds a sine wave component at the specified frequency.
    /// </summary>
    public AudioSignalBuilder AddSine(double frequency, double amplitude = 1.0, double phase = 0.0, double? durationSeconds = null)
    {
        _components.Add(new SineComponent(frequency, amplitude, phase, _currentTimeOffset, durationSeconds, _currentEnvelope));
        _currentEnvelope = null; // Reset after use
        return this;
    }

    /// <summary>
    /// Adds harmonics for the specified fundamental frequency.
    /// </summary>
    public AudioSignalBuilder AddHarmonics(double fundamental, int harmonicCount, double amplitude = 1.0, double? durationSeconds = null)
    {
        var envelope = _currentEnvelope; // Capture envelope for all harmonics
        for (int h = 1; h <= harmonicCount; h++)
        {
            var harmonicAmplitude = amplitude / h;
            _components.Add(new SineComponent(fundamental * h, harmonicAmplitude, 0.0, _currentTimeOffset, durationSeconds, envelope));
        }
        _currentEnvelope = null; // Reset after use
        return this;
    }

    /// <summary>
    /// Adds an impulse (brief click) at the current time offset.
    /// </summary>
    public AudioSignalBuilder AddImpulse(double amplitude = 1.0, double durationSeconds = 0.002)
    {
        _components.Add(new ImpulseComponent(amplitude, _currentTimeOffset, durationSeconds));
        return this;
    }

    /// <summary>
    /// Adds white noise across the entire duration (ignores time offset).
    /// </summary>
    public AudioSignalBuilder AddNoise(double amplitude = 1.0, int seed = 42)
    {
        _components.Add(new NoiseComponent(amplitude, seed, 0.0, null));
        return this;
    }

    /// <summary>
    /// Adds white noise at a specific time window.
    /// </summary>
    public AudioSignalBuilder AddNoiseAt(double timeSeconds, double durationSeconds, double amplitude = 1.0, int seed = 42)
    {
        _components.Add(new NoiseComponent(amplitude, seed, timeSeconds, durationSeconds));
        return this;
    }

    /// <summary>
    /// Adds a constant value for a specific time range.
    /// Useful for creating regions with specific amplitude levels.
    /// </summary>
    public AudioSignalBuilder AddConstant(double value, double? durationSeconds = null)
    {
        _components.Add(new ConstantComponent(value, _currentTimeOffset, durationSeconds));
        return this;
    }

    /// <summary>
    /// Builds the audio signal as a float array.
    /// </summary>
    public float[] Build()
    {
        var sampleCount = _fixedLength ?? (int)(_sampleRate * _duration);
        var buffer = new float[sampleCount];

        // Sum all components
        foreach (var component in _components)
        {
            component.AddToBuffer(buffer, _sampleRate);
        }

        return buffer;
    }

    /// <summary>
    /// Builds a pure sine wave (convenience method).
    /// </summary>
    public static float[] Sine(double frequency, double duration = 0.1, int sampleRate = 44100, double amplitude = 1.0)
    {
        return Create()
            .WithSampleRate(sampleRate)
            .WithDuration(duration)
            .AddSine(frequency, amplitude)
            .Build();
    }

    /// <summary>
    /// Builds a harmonic signal (convenience method).
    /// </summary>
    public static float[] Harmonics(double fundamental, int harmonicCount = 5, double duration = 0.1, int sampleRate = 44100)
    {
        return Create()
            .WithSampleRate(sampleRate)
            .WithDuration(duration)
            .AddHarmonics(fundamental, harmonicCount)
            .Build();
    }

    /// <summary>
    /// Builds white noise (convenience method).
    /// </summary>
    public static float[] Noise(double duration = 0.1, int sampleRate = 44100, int seed = 42)
    {
        return Create()
            .WithSampleRate(sampleRate)
            .WithDuration(duration)
            .AddNoise(1.0, seed)
            .Build();
    }

    /// <summary>
    /// Builds silence (convenience method).
    /// </summary>
    public static float[] Silence(double duration = 0.1, int sampleRate = 44100)
    {
        return Create()
            .WithSampleRate(sampleRate)
            .WithDuration(duration)
            .Build();
    }

    // Envelope interfaces and implementations
    private interface IEnvelope
    {
        double GetAmplitude(double time, double duration);
    }

    private sealed class AttackEnvelope(double attackTime) : IEnvelope
    {
        public double GetAmplitude(double time, double duration)
        {
            return time < attackTime ? time / attackTime : 1.0;
        }
    }

    private sealed class ADSREnvelope(double attack, double decay, double sustain, double release) : IEnvelope
    {
        public double GetAmplitude(double time, double duration)
        {
            if (time < attack)
            {
                return time / attack;
            }

            if (time < attack + decay)
            {
                var decayProgress = (time - attack) / decay;
                return 1.0 - (1.0 - sustain) * decayProgress;
            }

            var releaseStart = duration - release;
            if (time >= releaseStart)
            {
                var releaseProgress = (time - releaseStart) / release;
                return sustain * (1.0 - releaseProgress);
            }

            return sustain;
        }
    }

    // Component types
    private abstract class SignalComponent
    {
        public abstract void AddToBuffer(float[] buffer, int sampleRate);
    }

    private sealed class SineComponent(double frequency, double amplitude, double phase, double timeOffset, double? duration, AudioSignalBuilder.IEnvelope? envelope) : SignalComponent
    {
        public override void AddToBuffer(float[] buffer, int sampleRate)
        {
            var startSample = (int)(timeOffset * sampleRate);
            var endSample = duration.HasValue
                ? Math.Min(buffer.Length, startSample + (int)(duration.Value * sampleRate))
                : buffer.Length;

            for (int i = startSample; i < endSample; i++)
            {
                if (i < 0 || i >= buffer.Length)
                {
                    continue;
                }

                var t = (i - startSample) / (double)sampleRate;
                var totalDuration = duration ?? (buffer.Length / (double)sampleRate);
                var envelopeAmp = envelope?.GetAmplitude(t, totalDuration) ?? 1.0;

                buffer[i] += (float)(amplitude * envelopeAmp * Math.Sin(2 * Math.PI * frequency * t + phase));
            }
        }
    }

    private sealed class ImpulseComponent(double amplitude, double timeOffset, double duration) : SignalComponent
    {
        public override void AddToBuffer(float[] buffer, int sampleRate)
        {
            var startSample = (int)(timeOffset * sampleRate);
            var durationSamples = (int)(duration * sampleRate);

            for (int i = 0; i < durationSamples; i++)
            {
                var sampleIndex = startSample + i;
                if (sampleIndex < 0 || sampleIndex >= buffer.Length)
                {
                    continue;
                }

                // Exponentially decaying broadband impulse
                var decay = Math.Exp(-i / 10.0);
                buffer[sampleIndex] += (float)(amplitude * decay * Math.Sin(i * 0.5));
            }
        }
    }

    private sealed class NoiseComponent(double amplitude, int seed, double timeOffset, double? duration) : SignalComponent
    {
        public override void AddToBuffer(float[] buffer, int sampleRate)
        {
            var random = new Random(seed);
            var startSample = (int)(timeOffset * sampleRate);
            var endSample = duration.HasValue
                ? Math.Min(buffer.Length, startSample + (int)(duration.Value * sampleRate))
                : buffer.Length;

            for (int i = startSample; i < endSample; i++)
            {
                if (i < 0 || i >= buffer.Length)
                {
                    continue;
                }

                buffer[i] += (float)(amplitude * (random.NextDouble() * 2 - 1));
            }
        }
    }

    private sealed class ConstantComponent(double value, double timeOffset, double? duration) : SignalComponent
    {
        public override void AddToBuffer(float[] buffer, int sampleRate)
        {
            var startSample = (int)(timeOffset * sampleRate);
            var endSample = duration.HasValue
                ? Math.Min(buffer.Length, startSample + (int)(duration.Value * sampleRate))
                : buffer.Length;

            var targetBuffer = buffer.AsSpan(startSample, endSample - startSample);

            TensorPrimitives.Add(targetBuffer, (float)value, targetBuffer);
        }
    }
}
