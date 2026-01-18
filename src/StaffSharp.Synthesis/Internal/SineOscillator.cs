namespace StaffSharp.Synthesis.Internal;

/// <summary>
/// Generates sine wave audio samples.
/// </summary>
internal static class SineOscillator
{
    /// <summary>
    /// Generates a sine wave at the specified frequency.
    /// </summary>
    /// <param name="frequency">Frequency in Hz.</param>
    /// <param name="output">Span to write samples into.</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <param name="phaseOffset">Phase offset in samples (for continuing waves).</param>
    public static void Generate(float frequency, Span<float> output, int sampleRate, int phaseOffset = 0)
    {
        float twoPiF = 2.0f * MathF.PI * frequency;
        float sampleRateF = (float)sampleRate;

        for (int i = 0; i < output.Length; i++)
        {
            float t = (phaseOffset + i) / sampleRateF;
            output[i] = MathF.Sin(twoPiF * t);
        }
    }
}
