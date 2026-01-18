namespace StaffSharp.MachineLearning.ML.Features;

using StaffSharp.Audio;
using StaffSharp.Audio.Pipeline;

/// <summary>
/// Extracts features from audio for machine learning inference.
/// </summary>
public interface IFeatureExtractor
{
    /// <summary>
    /// Extracts features from audio buffer.
    /// </summary>
    /// <param name="progress">Pipeline progress and diagnostics collector.</param>
    /// <param name="audio">The audio buffer to extract features from.</param>
    /// <returns>Feature tensor with shape (time_frames, feature_bins).</returns>
    float[,] ExtractFeatures(PipelineProgress progress, AudioBuffer audio);
}
