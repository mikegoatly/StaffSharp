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
        var curveAbove = !startNote.StemUp;

        var startX = startNote.X + (1.0 * context.StaffSpace); // Offset from notehead center
        var endX = endNote.X - (0.5 * context.StaffSpace);
        var startY = startNote.Y;
        var endY = endNote.Y;

        // Calculate control points for a smooth curve
        var midX = (startX + endX) / 2;
        var curveHeight = 1.5 * context.StaffSpace;
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
