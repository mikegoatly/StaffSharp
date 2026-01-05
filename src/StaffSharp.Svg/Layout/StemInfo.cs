namespace StaffSharp.Layout;

/// <summary>
/// Contains stem information for a note or chord.
/// </summary>
/// <param name="X">X position of the stem.</param>
/// <param name="Y1">Y position of the stem start (at the notehead side).</param>
/// <param name="Y2">Y position of the stem end (opposite the notehead).</param>
/// <param name="Up">Whether the stem goes up (true) or down (false).</param>
public readonly record struct StemInfo(double X, double Y1, double Y2, bool Up)
{
    /// <summary>
    /// Gets the top Y position of the stem (accounting for direction).
    /// </summary>
    public double TopY => Up ? Y1 : Y2;

    /// <summary>
    /// Gets the bottom Y position of the stem (accounting for direction).
    /// </summary>
    public double BottomY => Up ? Y2 : Y1;
}
