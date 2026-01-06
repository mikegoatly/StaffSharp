using StaffSharp.Audio.Diagnostics;
using StaffSharp.Notation;

namespace StaffSharp.Audio.Analysis.Tempo;

/// <summary>
/// Options for tempo detection algorithms implementing <see cref="ITempoDetector"/>.
/// </summary>
public record TempoDetectionOptions : DiagnosticsOptions
{
    /// <summary>
    /// Gets or initializes the minimum detectable tempo in BPM.
    /// Default: 40.0 BPM.
    /// </summary>
    public double MinBpm { get; init; } = 40.0;

    /// <summary>
    /// Gets or initializes the maximum detectable tempo in BPM.
    /// Default: 240.0 BPM.
    /// </summary>
    public double MaxBpm { get; init; } = 240.0;

    /// <summary>
    /// Gets or initializes the default time signature for the tempo map.
    /// If null, 4/4 (common time) is used. Default: null.
    /// </summary>
    public TimeSignature? DefaultTimeSignature { get; init; }


    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when BPM range is invalid.</exception>
    public void Validate()
    {
        if (MinBpm <= 0 || MaxBpm <= MinBpm)
        {
            throw new ArgumentException("Invalid BPM range: MinBpm must be positive and less than MaxBpm");
        }
    }
}
