namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;

using StaffSharp.Notation;

/// <summary>
/// Pass that creates layout curves for ties and slurs.
/// Must run after HorizontalSpacingPass (needs X positions) and VerticalPositionPass (needs Y positions).
/// </summary>
internal class TiePass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        foreach (var staff in model.Systems.SelectMany(s => s.Staves))
        {
            ProcessStaffTies(staff, context);
        }
    }

    private static void ProcessStaffTies(LayoutStaff staff, SvgContext context)
    {
        // Process ties across all measures in the staff
        NoteLayoutSymbol? pendingTieStart = null;

        foreach (var measure in staff.Measures)
        {
            foreach (var noteSymbol in measure.Symbols.OfType<NoteLayoutSymbol>())
            {
                var tie = noteSymbol.Note.Tie;

                // Handle tie endings
                if ((tie == TieType.End || tie == TieType.Both) && pendingTieStart != null)
                {
                    // Create tie curve from pending start to this note
                    var curve = LayoutCurve.Create(pendingTieStart, noteSymbol, context, isTie: true);
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
