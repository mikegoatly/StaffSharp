namespace StaffSharp.Audio.Analysis.Meter;

/// <summary>
/// Options for time signature detection algorithms implementing <see cref="ITimeSignatureDetector"/>.
/// Currently a marker class for consistency with other analysis components.
/// Future options may include meter preferences, beat subdivision hints, etc.
/// </summary>
public record TimeSignatureDetectionOptions
{
    /// <summary>
    /// Validates the options.
    /// Currently no validation is required as there are no properties.
    /// </summary>
#pragma warning disable CA1822 // Mark members as static - Keep as instance method for consistency with other options
    public void Validate()
    {
        // No validation required for marker class
    }
#pragma warning restore CA1822
}
