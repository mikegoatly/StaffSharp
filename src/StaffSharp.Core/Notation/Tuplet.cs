namespace StaffSharp.Notation;

/// <summary>
/// Represents a tuplet (e.g., triplet, quintuplet).
/// </summary>
/// <param name="ActualNotes">Number of notes actually played.</param>
/// <param name="NormalNotes">Number of notes in normal time.</param>
/// <remarks>
/// Examples:
/// - Triplet: (3, 2) - play 3 notes in the time of 2
/// - Quintuplet: (5, 4) - play 5 notes in the time of 4
/// - Duplet: (2, 3) - play 2 notes in the time of 3
/// </remarks>
public record Tuplet(int ActualNotes, int NormalNotes)
{
    public static readonly Tuplet Triplet = new(3, 2);
    public static readonly Tuplet Quintuplet = new(5, 4);
    public static readonly Tuplet Sextuplet = new(6, 4);
    public static readonly Tuplet Septuplet = new(7, 4);
}
