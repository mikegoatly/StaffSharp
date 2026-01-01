namespace StaffSharp.Svg.Layout;

/// <summary>
/// Represents a tie or slur curve in the layout.
/// </summary>
public class LayoutCurve : LayoutElement
{
    /// <summary>
    /// True if this is a tie (same pitch), false if slur (different pitches).
    /// </summary>
    public bool IsTie { get; set; }

    /// <summary>
    /// True if the curve should arc above the notes, false if below.
    /// </summary>
    public bool CurveAbove { get; set; }

    /// <summary>
    /// X coordinate of the curve start.
    /// </summary>
    public double StartX { get; set; }

    /// <summary>
    /// Y coordinate of the curve start.
    /// </summary>
    public double StartY { get; set; }

    /// <summary>
    /// X coordinate of the curve end.
    /// </summary>
    public double EndX { get; set; }

    /// <summary>
    /// Y coordinate of the curve end.
    /// </summary>
    public double EndY { get; set; }

    /// <summary>
    /// X coordinate of the first control point (for Bézier curve).
    /// </summary>
    public double ControlX1 { get; set; }

    /// <summary>
    /// Y coordinate of the first control point.
    /// </summary>
    public double ControlY1 { get; set; }

    /// <summary>
    /// X coordinate of the second control point.
    /// </summary>
    public double ControlX2 { get; set; }

    /// <summary>
    /// Y coordinate of the second control point.
    /// </summary>
    public double ControlY2 { get; set; }
}
