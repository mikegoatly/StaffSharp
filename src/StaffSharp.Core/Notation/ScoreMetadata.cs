namespace StaffSharp.Notation;

/// <summary>
/// Metadata for a musical score.
/// </summary>
public record ScoreMetadata(
    string? Title,
    string? Composer,
    KeySignature KeySignature,
    TimeSignature TimeSignature,
    int Tempo
)
{
    public static ScoreMetadata Empty => new(null, null, KeySignature.C, TimeSignature.CommonTime, 120);
}
