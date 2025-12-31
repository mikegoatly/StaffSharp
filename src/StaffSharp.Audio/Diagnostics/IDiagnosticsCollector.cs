namespace StaffSharp.Audio.Diagnostics;

/// <summary>
/// Interface for collecting diagnostic data from audio processing pipeline stages.
/// Implementations must be thread-safe if used in parallel processing.
/// </summary>
public interface IDiagnosticsCollector
{
    /// <summary>
    /// Collects a diagnostic value for a specific pipeline stage.
    /// </summary>
    /// <typeparam name="T">The type of the diagnostic value.</typeparam>
    /// <param name="stageName">The name of the pipeline stage (e.g., "OnsetDetection", "PitchDetection").</param>
    /// <param name="key">The key identifying this diagnostic value (e.g., "onsetTimes", "pitchConfidence").</param>
    /// <param name="value">The diagnostic value to store.</param>
    void Collect<T>(string stageName, string key, T value);

    /// <summary>
    /// Gets all collected diagnostics, organized by stage name and key.
    /// </summary>
    /// <returns>
    /// A read-only dictionary where:
    /// - First level key is stage name
    /// - Second level key is diagnostic key
    /// - Value is the diagnostic data (type varies)
    /// </returns>
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetDiagnostics();
}
