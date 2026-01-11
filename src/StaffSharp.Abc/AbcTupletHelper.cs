namespace StaffSharp.Abc;

/// <summary>
/// Shared helper for ABC tuplet logic used by both import and export.
/// </summary>
internal static class AbcTupletHelper
{
    /// <summary>
    /// Gets the default normal notes for a given actual notes count (ABC standard).
    /// </summary>
    /// <param name="actualNotes">The number of notes in the tuplet.</param>
    /// <returns>The default normal notes count.</returns>
    /// <remarks>
    /// ABC standard defaults:
    /// - 2 notes -> 3 (duplet)
    /// - 3 notes -> 2 (triplet)
    /// - 4 notes -> 3 (quadruplet)
    /// - 5 notes -> 4 (quintuplet)
    /// - 6 notes -> 4 (sextuplet)
    /// - 7 notes -> 6 (septuplet)
    /// - 8 notes -> 6 (octuplet)
    /// - 9 notes -> 8 (nonuplet)
    /// </remarks>
    public static int GetDefaultNormalNotes(int actualNotes)
    {
        return actualNotes switch
        {
            2 => 3,  // Duplet
            3 => 2,  // Triplet
            4 => 3,  // Quadruplet
            5 => 4,  // Quintuplet
            6 => 4,  // Sextuplet
            7 => 6,  // Septuplet
            8 => 6,  // Octuplet
            9 => 8,  // Nonuplet
            _ => actualNotes - 1
        };
    }
}
