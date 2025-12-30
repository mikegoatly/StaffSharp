namespace StaffSharp.MusicXml;

using StaffSharp;
using StaffSharp.Notation;

/// <summary>
/// Tracks parsing state while processing a MusicXML document.
/// MusicXML uses cumulative state where attributes (divisions, key, time, clef)
/// persist until explicitly changed.
/// </summary>
internal sealed class MusicXmlContext
{
    /// <summary>
    /// Gets or sets the divisions value (ticks per quarter note).
    /// This is required for converting MusicXML durations to SymbolicDuration.
    /// Default is 1 (meaning duration values represent quarter notes directly).
    /// </summary>
    public int Divisions { get; set; } = 1;

    /// <summary>
    /// Gets or sets the current key signature.
    /// </summary>
    public KeySignature KeySignature { get; set; } = KeySignature.C;

    /// <summary>
    /// Gets or sets the current time signature.
    /// </summary>
    public TimeSignature TimeSignature { get; set; } = TimeSignature.CommonTime;

    /// <summary>
    /// Gets or sets the current clef.
    /// </summary>
    public Clef Clef { get; set; } = Clef.Treble;

    /// <summary>
    /// Gets or sets the current tempo in beats per minute.
    /// </summary>
    public int Tempo { get; set; } = 120;

    /// <summary>
    /// Tracks active slurs by their number (slur number → start event index).
    /// MusicXML uses numbered slurs that can span measures.
    /// </summary>
    public Dictionary<int, int> ActiveSlurs { get; } = new();

    /// <summary>
    /// Tracks active ties by voice and pitch (for detecting tie continuations).
    /// Key is (voiceNumber, pitchStep, octave), value is the event index where tie started.
    /// </summary>
    public Dictionary<(int Voice, string Step, int Octave), int> ActiveTies { get; } = new();

    /// <summary>
    /// Creates a new MusicXmlContext with default values.
    /// </summary>
    public MusicXmlContext()
    {
    }

    /// <summary>
    /// Creates a copy of this context to preserve state for a new part or section.
    /// </summary>
    public MusicXmlContext Clone()
    {
        var clone = new MusicXmlContext
        {
            Divisions = Divisions,
            KeySignature = KeySignature,
            TimeSignature = TimeSignature,
            Clef = Clef,
            Tempo = Tempo
        };

        // Note: We don't clone ActiveSlurs or ActiveTies as they are measure-specific
        return clone;
    }
}
