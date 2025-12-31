namespace StaffSharp.Performance;

/// <summary>
/// Represents a tempo change at a specific point in musical time.
/// </summary>
public sealed record TempoChange
{
    /// <summary>
    /// Creates a new tempo change.
    /// </summary>
    /// <param name="timeInBeats">The musical time (in beats from start) when this tempo begins.</param>
    /// <param name="beatsPerMinute">The tempo in beats per minute (BPM).</param>
    public TempoChange(Rational timeInBeats, double beatsPerMinute)
    {
        if (beatsPerMinute <= 0 || beatsPerMinute > 1000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(beatsPerMinute),
                beatsPerMinute,
                "Tempo must be between 0 and 1000 BPM");
        }

        TimeInBeats = timeInBeats;
        BeatsPerMinute = beatsPerMinute;
    }

    /// <summary>
    /// The musical time (in beats from start) when this tempo begins.
    /// </summary>
    public Rational TimeInBeats { get; init; }

    /// <summary>
    /// The tempo in beats per minute (BPM).
    /// </summary>
    public double BeatsPerMinute { get; init; }
}
