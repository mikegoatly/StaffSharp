namespace StaffSharp.Audio.Analysis.Tempo;

/// <summary>
/// Factory for creating tempo detector instances based on configuration.
/// </summary>
public static class TempoDetectorFactory
{
    /// <summary>
    /// Creates a tempo detector instance based on the specified options.
    /// </summary>
    /// <param name="options">Configuration options. If null, uses default CombFilter detector.</param>
    /// <returns>A configured tempo detector instance.</returns>
    public static ITempoDetector Create(TempoDetectionOptions? options = null)
    {
        options ??= new TempoDetectionOptions();

        return options.DetectorType switch
        {
            TempoDetectorType.CombFilter => new CombFilterTempoDetector(options),
            TempoDetectorType.InterOnsetInterval => new InterOnsetIntervalTempoDetector(options),
            _ => throw new ArgumentException($"Unknown tempo detector type: {options.DetectorType}", nameof(options))
        };
    }

    /// <summary>
    /// Creates a tempo detector instance of the specified type.
    /// </summary>
    /// <param name="type">The type of tempo detector to create.</param>
    /// <param name="options">Optional configuration options.</param>
    /// <returns>A configured tempo detector instance.</returns>
    public static ITempoDetector Create(TempoDetectorType type, TempoDetectionOptions? options = null)
    {
        var effectiveOptions = options ?? new TempoDetectionOptions();
        effectiveOptions = effectiveOptions with { DetectorType = type };
        return Create(effectiveOptions);
    }
}
