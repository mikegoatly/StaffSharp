namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;

/// <summary>
/// Calculates accurate bounds for individual layout elements (staves) including stems, beams, ledger lines, curves, and other visual elements.
/// MUST run after stems, beams, and curves have been positioned.
/// This pass sets the Height property on staves based on their actual rendered extents.
/// </summary>
internal class LayoutElementBoundsCalculationPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        foreach (var staff in model.Systems.SelectMany(s => s.Staves))
        {
            CalculateStaffBounds(staff, context);
        }
    }

    private static void CalculateStaffBounds(LayoutStaff staff, SvgContext context)
    {
        // Calculate staff bounds including all content (symbols, curves, stems, beams)
        // Note: minY and maxY are in absolute coordinates
        var (minY, _, height) = BoundsCalculator.CalculateStaffBounds(
            staff,
            staff.Bounds.Y,
            context.StaffSpace);

        // The staff origin (staff.Bounds.Y) represents the position of the top staff line.
        // Symbols are positioned relative to this origin, and can have negative Y values
        // (extending above the staff).
        //
        // When content extends upward, we need to:
        // 1. Shift all content DOWN by the upward extent (so nothing has negative Y)
        // 2. Adjust staff.Bounds.Y UPWARD by the upward extent (to maintain absolute positions)
        // 3. Track StaffYOffset to know where the staff lines (Y=0) are within the bounds
        // 4. Set Height to the full visual extent

        // Calculate how much content extends above the staff origin
        var upwardExtent = Math.Max(0, staff.Bounds.Y - minY);

        if (upwardExtent > 0)
        {
            // Shift all staff content down by the upward extent
            foreach (var measure in staff.Measures)
            {
                measure.Offset(0, upwardExtent);
            }

            // Adjust staff bounds: move Y up, set StaffYOffset, and set full height
            staff.Bounds = staff.Bounds with
            {
                Y = staff.Bounds.Y - upwardExtent,  // Move bounds up to visual top
                Height = height                      // Full visual height
            };

            staff.StaffYOffset = upwardExtent;      // Staff lines are now at this Y within bounds
        }
        else
        {
            // No upward extent - just set the height
            staff.Bounds = staff.Bounds with
            {
                Height = height
            };

            staff.StaffYOffset = 0;
        }
    }
}
