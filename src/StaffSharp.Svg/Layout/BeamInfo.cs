namespace StaffSharp.Layout;

/// <summary>
/// Contains beam and flag information for a note or chord.
/// </summary>
/// <param name="GroupId">Beam group ID if part of a beam, null otherwise.</param>
/// <param name="IsFirstInGroup">Whether this is the first symbol in the beam group.</param>
/// <param name="IsLastInGroup">Whether this is the last symbol in the beam group.</param>
/// <param name="BeamCount">Number of beams (1 for eighth, 2 for sixteenth, etc.).</param>
/// <param name="RequiresFlag">Whether this symbol requires a flag (non-beamed eighth note or shorter).</param>
/// <param name="FlagCount">Number of flags (1 for eighth, 2 for sixteenth, etc.).</param>
public readonly record struct BeamInfo(
    int? GroupId,
    bool IsFirstInGroup,
    bool IsLastInGroup,
    int BeamCount,
    bool RequiresFlag,
    int FlagCount)
{
    /// <summary>
    /// Gets whether this symbol is part of a beam group.
    /// </summary>
    public bool IsBeamed => GroupId.HasValue;

    /// <summary>
    /// Creates a BeamInfo for a non-beamed note with no flags (quarter note or longer).
    /// </summary>
    public static BeamInfo None => new(null, false, false, 0, false, 0);
}
