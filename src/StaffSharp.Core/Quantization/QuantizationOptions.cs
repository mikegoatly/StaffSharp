namespace StaffSharp.Quantization;

/// <summary>
/// Options for quantization algorithms.
/// </summary>
public record QuantizationOptions
{
    /// <summary>
    /// Gets or initializes the quantization grid in beats.
    /// For example, 1/4 represents 16th notes in 4/4 time.
    /// Default: 1/4 (16th notes).
    /// </summary>
    public Rational QuantizationGrid { get; init; } = Rational.Create(1, 4);

    /// <summary>
    /// Gets or initializes the default duration for the last note in beats.
    /// Default: 1 (quarter note).
    /// </summary>
    public Rational DefaultLastNoteDuration { get; init; } = Rational.Create(1, 1);

    /// <summary>
    /// Gets or initializes the minimum note duration in beats.
    /// Notes shorter than this are extended.
    /// Default: 1/8 (32nd note).
    /// </summary>
    public Rational MinNoteDuration { get; init; } = Rational.Create(1, 8);

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when any duration is non-positive.</exception>
    public void Validate()
    {
        if (QuantizationGrid <= Rational.Zero)
        {
            throw new ArgumentException("Quantization grid must be positive", nameof(QuantizationGrid));
        }

        if (DefaultLastNoteDuration <= Rational.Zero)
        {
            throw new ArgumentException("Default last note duration must be positive", nameof(DefaultLastNoteDuration));
        }

        if (MinNoteDuration <= Rational.Zero)
        {
            throw new ArgumentException("Minimum note duration must be positive", nameof(MinNoteDuration));
        }
    }
}
