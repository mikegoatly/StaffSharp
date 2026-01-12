namespace StaffSharp.MachineLearning.ML.Features;

using StaffSharp.Audio;

/// <summary>
/// Extracts features from audio for machine learning inference.
/// </summary>
public interface IFeatureExtractor
{
    /// <summary>
    /// Extracts features from audio buffer.
    /// </summary>
    /// <param name="audio">The audio buffer to extract features from.</param>
    /// <returns>Feature tensor with shape (time_frames, feature_bins).</returns>
    float[,] ExtractFeatures(AudioBuffer audio);
}
