namespace StaffSharp.MachineLearning.Options;

/// <summary>
/// Configuration options for mel spectrogram extraction.
/// These parameters MUST match the Python training code exactly for correct inference.
/// </summary>
public sealed record MelSpectrogramOptions
{
    /// <summary>
    /// Target sample rate for audio processing (Hz).
    /// Audio will be resampled to this rate before feature extraction.
    /// </summary>
    public int SampleRate { get; init; } = 16000;

    /// <summary>
    /// FFT frame size (samples). Must be a power of 2.
    /// </summary>
    public int FrameSize { get; init; } = 2048;

    /// <summary>
    /// Hop size between frames (samples).
    /// </summary>
    public int HopSize { get; init; } = 512;

    /// <summary>
    /// Number of mel frequency bins.
    /// </summary>
    public int MelBins { get; init; } = 229;

    /// <summary>
    /// Minimum frequency for mel filterbank (Hz).
    /// </summary>
    public float MinFrequency { get; init; } = 27.5f;

    /// <summary>
    /// Maximum frequency for mel filterbank (Hz).
    /// </summary>
    public float MaxFrequency { get; init; } = 8000f;

    /// <summary>
    /// Constant for logarithmic compression: log(1 + C * magnitude).
    /// </summary>
    public float LogCompressionConstant { get; init; } = 10000f;
}
