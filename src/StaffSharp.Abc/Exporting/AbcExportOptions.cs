namespace StaffSharp.Abc.Exporting;

using StaffSharp;

/// <summary>
/// Options for exporting a NotationScore to ABC notation format.
/// </summary>
internal sealed record AbcExportOptions
{
    /// <summary>
    /// Gets the default note length for the ABC notation (L: header field).
    /// Default is 1/8 (eighth note), which is the most common in ABC notation.
    /// </summary>
    /// <remarks>
    /// This determines how note durations are encoded:
    /// - A note with duration equal to DefaultNoteLength is written without duration modifier
    /// - A note with duration 2× DefaultNoteLength is written with "2"
    /// - A note with duration ½× DefaultNoteLength is written with "/" or "/2"
    /// Common values: 1/8, 1/4, 1/16.
    /// </remarks>
    public Rational DefaultNoteLength { get; init; } = Rational.Create(1, 8);
}
