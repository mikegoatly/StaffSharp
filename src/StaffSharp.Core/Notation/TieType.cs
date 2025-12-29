namespace StaffSharp.Notation;

/// <summary>
/// Indicates if a note is tied to another note.
/// </summary>
public enum TieType
{
    /// <summary>No tie.</summary>
    None,

    /// <summary>Tie starts from this note to the next.</summary>
    Start,

    /// <summary>This note is the end of a tie from the previous note.</summary>
    End,

    /// <summary>This note is tied from previous AND to next (middle of a tie chain).</summary>
    Both
}
