namespace StaffSharp.Core.Notation;

/// <summary>
/// Metadata for a musical score.
/// </summary>
public record ScoreMetadata(
    string? Title,
    string? Composer,
    KeySignature KeySignature,
    TimeSignature TimeSignature,
    int Tempo
);
