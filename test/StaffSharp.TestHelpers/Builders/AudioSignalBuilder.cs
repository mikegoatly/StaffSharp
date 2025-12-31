namespace StaffSharp.TestHelpers.Builders;

/// <summary>
/// Fluent builder for creating test audio signals.
/// </summary>
public sealed class AudioSignalBuilder
{
    private readonly List<SignalComponent> _components = new();
    private int _sampleRate = 44100;
    private double _duration = 0.1; // 100ms default
    private int? _fixedLength;
    private double _currentTimeOffset;
    private IEnvelope? _currentEnvelope;

    private AudioSignalBuilder() { }

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
    /// Adds a DC offset (constant value).
    /// </summary>
    public AudioSignalBuilder AddDC(double offset)
    {
        _components.Add(new DCComponent(offset));
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

    private sealed class AttackEnvelope : IEnvelope
    {
        private readonly double _attackTime;

        public AttackEnvelope(double attackTime) => _attackTime = attackTime;

        public double GetAmplitude(double time, double duration)
        {
            if (time < _attackTime)
                return time / _attackTime;
            return 1.0;
        }
    }

    private sealed class ADSREnvelope : IEnvelope
    {
        private readonly double _attack;
        private readonly double _decay;
        private readonly double _sustain;
        private readonly double _release;

        public ADSREnvelope(double attack, double decay, double sustain, double release)
        {
            _attack = attack;
            _decay = decay;
            _sustain = sustain;
            _release = release;
        }

        public double GetAmplitude(double time, double duration)
        {
            if (time < _attack)
                return time / _attack;

            if (time < _attack + _decay)
            {
                var decayProgress = (time - _attack) / _decay;
                return 1.0 - (1.0 - _sustain) * decayProgress;
            }

            var releaseStart = duration - _release;
            if (time >= releaseStart)
            {
                var releaseProgress = (time - releaseStart) / _release;
                return _sustain * (1.0 - releaseProgress);
            }

            return _sustain;
        }
    }

    // Component types
    private abstract class SignalComponent
    {
        public abstract void AddToBuffer(float[] buffer, int sampleRate);
    }

    private sealed class SineComponent : SignalComponent
    {
        private readonly double _frequency;
        private readonly double _amplitude;
        private readonly double _phase;
        private readonly double _timeOffset;
        private readonly double? _duration;
        private readonly IEnvelope? _envelope;

        public SineComponent(double frequency, double amplitude, double phase, double timeOffset, double? duration, IEnvelope? envelope)
        {
            _frequency = frequency;
            _amplitude = amplitude;
            _phase = phase;
            _timeOffset = timeOffset;
            _duration = duration;
            _envelope = envelope;
        }

        public override void AddToBuffer(float[] buffer, int sampleRate)
        {
            var startSample = (int)(_timeOffset * sampleRate);
            var endSample = _duration.HasValue
                ? Math.Min(buffer.Length, startSample + (int)(_duration.Value * sampleRate))
                : buffer.Length;

            for (int i = startSample; i < endSample; i++)
            {
                if (i < 0 || i >= buffer.Length) continue;

                var t = (i - startSample) / (double)sampleRate;
                var totalDuration = _duration ?? (buffer.Length / (double)sampleRate);
                var envelopeAmp = _envelope?.GetAmplitude(t, totalDuration) ?? 1.0;

                buffer[i] += (float)(_amplitude * envelopeAmp * Math.Sin(2 * Math.PI * _frequency * t + _phase));
            }
        }
    }

    private sealed class ImpulseComponent : SignalComponent
    {
        private readonly double _amplitude;
        private readonly double _timeOffset;
        private readonly double _duration;

        public ImpulseComponent(double amplitude, double timeOffset, double duration)
        {
            _amplitude = amplitude;
            _timeOffset = timeOffset;
            _duration = duration;
        }

        public override void AddToBuffer(float[] buffer, int sampleRate)
        {
            var startSample = (int)(_timeOffset * sampleRate);
            var durationSamples = (int)(_duration * sampleRate);

            for (int i = 0; i < durationSamples; i++)
            {
                var sampleIndex = startSample + i;
                if (sampleIndex < 0 || sampleIndex >= buffer.Length) continue;

                // Exponentially decaying broadband impulse
                var decay = Math.Exp(-i / 10.0);
                buffer[sampleIndex] += (float)(_amplitude * decay * Math.Sin(i * 0.5));
            }
        }
    }

    private sealed class NoiseComponent : SignalComponent
    {
        private readonly double _amplitude;
        private readonly int _seed;
        private readonly double _timeOffset;
        private readonly double? _duration;

        public NoiseComponent(double amplitude, int seed, double timeOffset, double? duration)
        {
            _amplitude = amplitude;
            _seed = seed;
            _timeOffset = timeOffset;
            _duration = duration;
        }

        public override void AddToBuffer(float[] buffer, int sampleRate)
        {
            var random = new Random(_seed);
            var startSample = (int)(_timeOffset * sampleRate);
            var endSample = _duration.HasValue
                ? Math.Min(buffer.Length, startSample + (int)(_duration.Value * sampleRate))
                : buffer.Length;

            for (int i = startSample; i < endSample; i++)
            {
                if (i < 0 || i >= buffer.Length) continue;
                buffer[i] += (float)(_amplitude * (random.NextDouble() * 2 - 1));
            }
        }
    }

    private sealed class DCComponent : SignalComponent
    {
        private readonly double _offset;

        public DCComponent(double offset)
        {
            _offset = offset;
        }

        public override void AddToBuffer(float[] buffer, int sampleRate)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] += (float)_offset;
            }
        }
    }
}
