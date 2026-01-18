using System.Numerics.Tensors;

namespace StaffSharp.Synthesis.Internal;

/// <summary>
/// Applies ADSR (Attack, Decay, Sustain, Release) envelope to audio samples.
/// </summary>
internal static class AdsrEnvelope
{
    /// <summary>
    /// Applies an ADSR envelope to the given samples using SIMD operations.
    /// </summary>
    /// <param name="samples">Audio samples to process in-place.</param>
    /// <param name="noteDuration">Total duration of the note in seconds.</param>
    /// <param name="attackTime">Attack time in seconds.</param>
    /// <param name="decayTime">Decay time in seconds.</param>
    /// <param name="sustainLevel">Sustain level (0.0 - 1.0).</param>
    /// <param name="releaseTime">Release time in seconds.</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    public static void Apply(
        Span<float> samples,
        float noteDuration,
        float attackTime,
        float decayTime,
        float sustainLevel,
        float releaseTime,
        int sampleRate)
    {
        int totalSamples = samples.Length;
        if (totalSamples == 0) return;

        // Calculate envelope phase durations, ensuring they fit within total duration
        int attackSamples = Math.Min((int)(attackTime * sampleRate), totalSamples / 3);
        int releaseSamples = Math.Min((int)(releaseTime * sampleRate), totalSamples / 3);
        int decaySamples = Math.Min((int)(decayTime * sampleRate), (totalSamples - attackSamples - releaseSamples) / 2);
        int sustainSamples = Math.Max(0, totalSamples - attackSamples - decaySamples - releaseSamples);

        // Generate envelope curve
        Span<float> envelope = stackalloc float[totalSamples];
        int idx = 0;

        // Attack phase: linear ramp from 0 to 1
        for (int i = 0; i < attackSamples; i++, idx++)
        {
            envelope[idx] = attackSamples > 0 ? (float)i / attackSamples : 1.0f;
        }

        // Decay phase: linear ramp from 1 to sustainLevel
        for (int i = 0; i < decaySamples; i++, idx++)
        {
            float t = decaySamples > 0 ? (float)i / decaySamples : 1.0f;
            envelope[idx] = 1.0f - t * (1.0f - sustainLevel);
        }

        // Sustain phase: hold at sustainLevel
        for (int i = 0; i < sustainSamples; i++, idx++)
        {
            envelope[idx] = sustainLevel;
        }

        // Release phase: linear ramp from sustainLevel to 0
        for (int i = 0; i < releaseSamples; i++, idx++)
        {
            float t = releaseSamples > 0 ? (float)i / releaseSamples : 1.0f;
            envelope[idx] = sustainLevel * (1.0f - t);
        }

        // Apply envelope using SIMD multiplication
        TensorPrimitives.Multiply(samples, envelope, samples);
    }
}
