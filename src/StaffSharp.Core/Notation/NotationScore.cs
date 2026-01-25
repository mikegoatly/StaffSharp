namespace StaffSharp.Notation;

using StaffSharp.Performance;

/// <summary>
/// Top-level container for a musical score with hierarchical notation structure.
/// </summary>
public class NotationScore
{
    public NotationScore(
        ScoreMetadata metadata,
        IReadOnlyList<Part> parts,
        TempoMap? tempoMap = null)
    {
        Metadata = metadata;
        Parts = parts;
        TempoMap = tempoMap;
    }

    public ScoreMetadata Metadata { get; }
    public IReadOnlyList<Part> Parts { get; }
    public TempoMap? TempoMap { get; }
}
