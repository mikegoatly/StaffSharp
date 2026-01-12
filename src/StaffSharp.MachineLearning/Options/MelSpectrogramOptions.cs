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
    /// Default: 16000 Hz (common for speech/music models).
    /// </summary>
    public int SampleRate { get; init; } = 16000;

    /// <summary>
    /// FFT frame size (samples). Must be a power of 2.
    /// Default: 2048 samples.
    /// </summary>
    public int FrameSize { get; init; } = 2048;

    /// <summary>
    /// Hop size between frames (samples).
    /// Default: 512 samples (~31.25 fps at 16kHz).
    /// </summary>
    public int HopSize { get; init; } = 512;

    /// <summary>
    /// Number of mel frequency bins.
    /// Default: 229 (covers piano range A0-C8).
    /// </summary>
    public int MelBins { get; init; } = 229;

    /// <summary>
    /// Minimum frequency for mel filterbank (Hz).
    /// Default: 27.5 Hz (piano key A0).
    /// </summary>
    public float MinFrequency { get; init; } = 27.5f;

    /// <summary>
    /// Maximum frequency for mel filterbank (Hz).
    /// Default: 4186 Hz (piano key C8).
    /// </summary>
    public float MaxFrequency { get; init; } = 4186f;

    /// <summary>
    /// Constant for logarithmic compression: log(1 + C * magnitude).
    /// Default: 10000 (standard for Onsets and Frames model).
    /// </summary>
    public float LogCompressionConstant { get; init; } = 10000f;
}
