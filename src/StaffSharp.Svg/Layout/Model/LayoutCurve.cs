namespace StaffSharp.Layout.Model;

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

    /// <summary>
    /// True if this curve segment starts in the middle of an ongoing slur (previous system continues).
    /// </summary>
    public bool ContinuationStart { get; set; }

    /// <summary>
    /// True if this curve segment ends in the middle of an ongoing slur (continues to next system).
    /// </summary>
    public bool ContinuationEnd { get; set; }

    internal static LayoutCurve Create(IStemmedSymbol startNote, IStemmedSymbol endNote, SvgContext context, bool isTie)
    {
        // Determine curve direction based on stem direction
        // If stems are up, curve goes below; if stems are down, curve goes above
        var curveAbove = !startNote.Stem.Up;

        // Calculate notehead width (scaled from SMuFL units)
        // NoteHeadBlack height: 279 units, width: 330 units, scaled to 1.0 staff spaces height
        var noteheadWidth = 1.18 * context.StaffSpace;
        var noteheadHeight = context.StaffSpace;

        // Start tie/slur at right edge of first notehead
        var startX = startNote.X + noteheadWidth;
        // End tie/slur at left edge of second notehead
        var endX = endNote.X;

        // Position curve above or below the notehead, not through the middle
        // Add small clearance (0.15 staff spaces) from the notehead edge
        var verticalOffset = curveAbove ? -noteheadHeight * 0.5 - 0.15 * context.StaffSpace
                                         : noteheadHeight * 0.5 + 0.15 * context.StaffSpace;
        var startY = startNote.Y + verticalOffset;
        var endY = endNote.Y + verticalOffset;

        // Calculate control points for a smooth curve
        var curveHeight = isTie ? 0.5 * context.StaffSpace : 0.7 * context.StaffSpace;
        var controlYOffset = curveAbove ? -curveHeight : curveHeight;

        return new LayoutCurve
        {
            IsTie = isTie,
            CurveAbove = curveAbove,
            StartX = startX,
            StartY = startY,
            EndX = endX,
            EndY = endY,
            ControlX1 = startX + (endX - startX) * 0.25,
            ControlY1 = startY + controlYOffset,
            ControlX2 = startX + (endX - startX) * 0.75,
            ControlY2 = endY + controlYOffset
        };
    }
}
