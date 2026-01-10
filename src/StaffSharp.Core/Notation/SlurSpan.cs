namespace StaffSharp.Notation;

/// <summary>
/// Represents a slur that may span measures, staves, and systems.
/// Stores concrete endpoints and endpoint metadata for layout mapping.
/// </summary>
/// <param name="StartEvent">The notation event (typically a note or chord) where the slur begins.</param>
/// <param name="EndEvent">The notation event (typically a note or chord) where the slur ends.</param>
/// <param name="Number">Optional slur number for disambiguation when multiple slurs exist, used in formats like MusicXML.</param>
/// <param name="IsDotted">Indicates whether the slur should be rendered with a dotted line.</param>
/// <param name="StartStaffNumber">The 1-based staff number where the slur starts, relevant for grand staff parts.</param>
/// <param name="EndStaffNumber">The 1-based staff number where the slur ends, relevant for grand staff parts.</param>
/// <param name="StartVoiceNumber">The 1-based voice number where the slur starts.</param>
/// <param name="EndVoiceNumber">The 1-based voice number where the slur ends.</param>
public record SlurSpan(
    INotationEvent StartEvent,
    INotationEvent EndEvent,
    int? Number,
    bool IsDotted,
    int StartStaffNumber,
    int EndStaffNumber,
    int StartVoiceNumber,
    int EndVoiceNumber
) : TieSpan(StartEvent, EndEvent, StartStaffNumber, EndStaffNumber, StartVoiceNumber, EndVoiceNumber);
