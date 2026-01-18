namespace StaffSharp.Synthesis;

/// <summary>
/// Configuration options for audio synthesis.
/// </summary>
public class SynthesisOptions
{
    /// <summary>
    /// Sample rate in Hz.
    /// </summary>
    public int SampleRate { get; set; } = 44100;

    /// <summary>
    /// Attack time in seconds (linear ramp from 0 to peak).
    /// </summary>
    public float AttackTime { get; set; } = 0.005f; // 5ms

    /// <summary>
    /// Decay time in seconds (linear ramp from peak to sustain level).
    /// </summary>
    public float DecayTime { get; set; } = 0.02f; // 20ms

    /// <summary>
    /// Sustain level as a fraction of peak amplitude (0.0 - 1.0).
    /// </summary>
    public float SustainLevel { get; set; } = 0.7f; // 70%

    /// <summary>
    /// Release time in seconds (linear ramp from sustain to 0).
    /// </summary>
    public float ReleaseTime { get; set; } = 0.03f; // 30ms

    /// <summary>
    /// Whether to normalize the output to prevent clipping.
    /// </summary>
    public bool Normalize { get; set; } = true;
}
