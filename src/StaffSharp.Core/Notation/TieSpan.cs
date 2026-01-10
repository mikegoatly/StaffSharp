namespace StaffSharp.Notation;

/// <summary>
/// Represents a tie that may span measures, staves, and systems.
/// Stores concrete start and end events with metadata for layout mapping.
/// Ties connect notes of the same pitch to indicate sustained duration.
/// </summary>
/// <param name="StartEvent">The notation event (typically a note or chord) where the tie begins.</param>
/// <param name="EndEvent">The notation event (typically a note or chord) where the tie ends.</param>
/// <param name="StartStaffNumber">The 1-based staff number where the tie starts, relevant for grand staff parts.</param>
/// <param name="EndStaffNumber">The 1-based staff number where the tie ends, relevant for grand staff parts.</param>
/// <param name="StartVoiceNumber">The 1-based voice number where the tie starts.</param>
/// <param name="EndVoiceNumber">The 1-based voice number where the tie ends.</param>
public record TieSpan(
    INotationEvent StartEvent,
    INotationEvent EndEvent,
    int StartStaffNumber,
    int EndStaffNumber,
    int StartVoiceNumber,
    int EndVoiceNumber
);
