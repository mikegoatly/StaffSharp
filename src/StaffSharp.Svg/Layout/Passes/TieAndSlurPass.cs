namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;

/// <summary>
/// Pass that creates layout curves for ties and slurs.
/// Must run after HorizontalSpacingPass (needs X positions) and VerticalPositionPass (needs Y positions).
/// </summary>
public class TieAndSlurPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                ProcessStaffTies(staff, context);
            }
        }
    }

    private static void ProcessStaffTies(LayoutStaff staff, SvgContext context)
    {
        // Process ties across all measures in the staff
        NoteLayoutSymbol? pendingTieStart = null;

        foreach (var measure in staff.Measures)
        {
            foreach (var symbol in measure.Symbols)
            {
                if (symbol is NoteLayoutSymbol noteSymbol)
                {
                    var tie = noteSymbol.Note.Tie;

                    // Handle tie endings
                    if ((tie == TieType.End || tie == TieType.Both) && pendingTieStart != null)
                    {
                        // Create tie curve from pending start to this note
                        var curve = CreateTieCurve(pendingTieStart, noteSymbol, context);
                        measure.AddCurve(curve);
                        pendingTieStart = null;
                    }

                    // Handle tie starts
                    if (tie == TieType.Start || tie == TieType.Both)
                    {
                        pendingTieStart = noteSymbol;
                    }
                }
            }
        }
    }

    private static LayoutCurve CreateTieCurve(NoteLayoutSymbol startNote, NoteLayoutSymbol endNote, SvgContext context)
    {
        // Determine curve direction based on stem direction
        // If stems are up, curve goes below; if stems are down, curve goes above
        var curveAbove = !startNote.Stem.Up;

        // Calculate notehead width (scaled from SMuFL units)
        // NoteHeadBlack height: 279 units, width: 330 units, scaled to 1.0 staff spaces height
        var noteheadWidth = 1.18 * context.StaffSpace;
        var noteheadHeight = 1.0 * context.StaffSpace;

        // Start tie at right edge of first notehead
        var startX = startNote.X + noteheadWidth;
        // End tie at left edge of second notehead
        var endX = endNote.X;

        // Position tie above or below the notehead, not through the middle
        // Add small clearance (0.15 staff spaces) from the notehead edge
        var verticalOffset = curveAbove ? -noteheadHeight * 0.5 - 0.15 * context.StaffSpace
                                         : noteheadHeight * 0.5 + 0.15 * context.StaffSpace;
        var startY = startNote.Y + verticalOffset;
        var endY = endNote.Y + verticalOffset;

        // Calculate control points for a smooth curve
        var curveHeight = 0.5 * context.StaffSpace; // Reduced height for more subtle curve
        var controlYOffset = curveAbove ? -curveHeight : curveHeight;

        return new LayoutCurve
        {
            IsTie = true,
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
