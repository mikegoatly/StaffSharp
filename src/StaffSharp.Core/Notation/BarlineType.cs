namespace StaffSharp.Notation;

/// <summary>
/// Represents the type of barline.
/// </summary>
public enum BarlineType
{
    /// <summary>
    /// Normal single barline.
    /// </summary>
    Normal,

    /// <summary>
    /// Start of a repeated section (|:).
    /// </summary>
    RepeatStart,

    /// <summary>
    /// End of a repeated section (:|).
    /// </summary>
    RepeatEnd,

    /// <summary>
    /// Both repeat start and end (:|:).
    /// </summary>
    RepeatBoth,

    /// <summary>
    /// Double barline (||).
    /// </summary>
    DoubleBar,

    /// <summary>
    /// Final barline (bold double barline at the end of a piece).
    /// </summary>
    Final
}
