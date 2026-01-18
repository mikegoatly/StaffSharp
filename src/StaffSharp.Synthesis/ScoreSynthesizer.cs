using System.Numerics.Tensors;
using StaffSharp.Audio;
using StaffSharp.Notation;
using StaffSharp.Synthesis.Internal;

namespace StaffSharp.Synthesis;

/// <summary>
/// Synthesizes musical scores into audio using sine wave synthesis with ADSR envelopes.
/// </summary>
public class ScoreSynthesizer : ISynthesizer
{
    private readonly SynthesisOptions _options;

    /// <summary>
    /// Initializes a new instance of the ScoreSynthesizer class.
    /// </summary>
    /// <param name="options">Synthesis options (optional).</param>
    public ScoreSynthesizer(SynthesisOptions? options = null)
    {
        _options = options ?? new SynthesisOptions();
    }

    /// <summary>
    /// Synthesizes a score to an AudioBuffer.
    /// </summary>
    /// <param name="score">The musical score to synthesize.</param>
    /// <param name="sampleRate">Sample rate in Hz (default: 44100).</param>
    /// <returns>An AudioBuffer containing the synthesized audio.</returns>
    public AudioBuffer Synthesize(NotationScore score, int sampleRate = 44100)
    {
        ArgumentNullException.ThrowIfNull(score);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);

        // Build timeline of all note events
        var timeline = NoteTimeline.FromScore(score);

        if (timeline.Events.Count == 0)
        {
            // Empty score returns silence (1 sample to avoid zero-length buffer)
            return new AudioBuffer([0f], sampleRate, 1);
        }

        // Calculate total duration and allocate output buffer
        double totalDuration = timeline.GetTotalDuration();
        int totalSamples = (int)Math.Ceiling(totalDuration * sampleRate);

        if (totalSamples == 0)
        {
            return new AudioBuffer([0f], sampleRate, 1);
        }

        float[] samples = new float[totalSamples];

        // Synthesize each note and mix into output
        foreach (var noteEvent in timeline.Events)
        {
            SynthesizeAndMixNote(noteEvent, samples, sampleRate);
        }

        // Normalize to prevent clipping
        if (_options.Normalize)
        {
            NormalizeSamples(samples);
        }

        return new AudioBuffer(samples, sampleRate, channels: 1);
    }

    private void SynthesizeAndMixNote(SynthNote note, Span<float> output, int sampleRate)
    {
        // Calculate sample range for this note
        int startSample = (int)(note.OnsetSeconds * sampleRate);
        int endSample = (int)(note.OffsetSeconds * sampleRate);
        int noteSampleCount = endSample - startSample;

        if (noteSampleCount <= 0 || startSample >= output.Length)
        {
            return; // Skip notes that are too short or out of range
        }

        // Clamp to output buffer bounds
        int actualEndSample = Math.Min(endSample, output.Length);
        noteSampleCount = actualEndSample - startSample;

        // Generate sine wave for this note
        float frequency = note.Pitch.ToFrequency().Value;
        Span<float> noteBuffer = stackalloc float[noteSampleCount];
        SineOscillator.Generate(frequency, noteBuffer, sampleRate, phaseOffset: startSample);

        // Apply ADSR envelope
        float noteDuration = (float)(note.OffsetSeconds - note.OnsetSeconds);
        AdsrEnvelope.Apply(
            noteBuffer,
            noteDuration,
            _options.AttackTime,
            _options.DecayTime,
            _options.SustainLevel,
            _options.ReleaseTime,
            sampleRate);

        // Apply velocity scaling using SIMD
        float velocity = note.Velocity;
        if (velocity != 1.0f)
        {
            TensorPrimitives.Multiply(noteBuffer, velocity, noteBuffer);
        }

        // Mix into output buffer using SIMD addition
        var outputSlice = output.Slice(startSample, noteSampleCount);
        TensorPrimitives.Add(outputSlice, noteBuffer, outputSlice);
    }

    private static void NormalizeSamples(Span<float> samples)
    {
        // Find maximum absolute value using SIMD
        float max = TensorPrimitives.Max(samples);
        float min = TensorPrimitives.Min(samples);
        float maxAmplitude = Math.Max(Math.Abs(max), Math.Abs(min));

        // Normalize if needed (prevent clipping)
        if (maxAmplitude > 1.0f)
        {
            TensorPrimitives.Divide(samples, maxAmplitude, samples);
        }
    }
}
