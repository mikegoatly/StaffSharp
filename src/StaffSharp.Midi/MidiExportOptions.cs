namespace StaffSharp.Midi;

/// <summary>
/// Options for exporting a NotationScore to MIDI format.
/// </summary>
public sealed record MidiExportOptions
{
    /// <summary>
    /// Gets the number of ticks per quarter note (MIDI time division).
    /// Default is 480 ticks per quarter note.
    /// </summary>
    /// <remarks>
    /// Higher values provide more precise timing at the cost of larger file sizes.
    /// Common values: 96, 192, 384, 480, 960.
    /// </remarks>
    public int TicksPerQuarterNote { get; init; } = 480;
}
