using StaffSharp.Quantization;

namespace StaffSharp;

/// <summary>
/// Interface for note detection algorithms that transcribe audio to musical notation.
/// Implementations own the full transcription pipeline: detection → tempo analysis → quantization.
/// </summary>
public interface INoteDetector
{
    /// <summary>
    /// Detects and quantizes notes from audio.
    /// </summary>
    /// <param name="audio">Normalized audio buffer to transcribe.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Transcription result containing quantized notes and tempo map.</returns>
    Task<QuantizationResult> DetectAsync(AudioBuffer audio, CancellationToken ct = default);
}
