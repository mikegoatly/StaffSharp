namespace StaffSharp.Core.Notation;

/// <summary>
/// Top-level container for a musical score with hierarchical notation structure.
/// </summary>
public class NotationScore
{
    public NotationScore(ScoreMetadata metadata, IReadOnlyList<Part> parts)
    {
        Metadata = metadata;
        Parts = parts;
    }

    public ScoreMetadata Metadata { get; }
    public IReadOnlyList<Part> Parts { get; }
}
