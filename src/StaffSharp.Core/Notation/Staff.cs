namespace StaffSharp.Notation;

/// <summary>
/// Represents a single staff within a part.
/// Multiple staves are used for grand staff notation (e.g., piano).
/// </summary>
public class Staff
{
    public Staff(int number, Clef clef, IReadOnlyList<Voice> voices)
    {
        if (number < 1)
        {
            throw new ArgumentException("Staff number must be >= 1", nameof(number));
        }

        Number = number;
        Clef = clef;
        Voices = voices ?? throw new ArgumentNullException(nameof(voices));
    }

    /// <summary>
    /// Staff number (1-based). Staves ordered top to bottom.
    /// For piano: Staff 1 = treble, Staff 2 = bass.
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// Clef used for this staff.
    /// </summary>
    public Clef Clef { get; }

    /// <summary>
    /// Voices within this staff.
    /// </summary>
    public IReadOnlyList<Voice> Voices { get; }
}
