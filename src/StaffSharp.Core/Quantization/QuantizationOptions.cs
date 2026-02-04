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
    /// Gets or initializes the tolerance for aligning note onsets when grouping chords (in beats).
    /// Notes starting within this tolerance and overlapping in time will be aligned to the same onset.
    /// This is important for piano recordings where chord notes may not start at exactly the same time.
    /// Default: 1/32 beat.
    /// </summary>
    public Rational OnsetAlignmentTolerance { get; init; } = Rational.Create(1, 32);

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when any duration is non-positive or tolerance is negative.</exception>
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

        if (OnsetAlignmentTolerance < Rational.Zero)
        {
            throw new ArgumentException("Onset alignment tolerance must be non-negative", nameof(OnsetAlignmentTolerance));
        }
    }
}
