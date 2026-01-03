namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Svg;
using StaffSharp.Svg.Layout.Services;

/// <summary>
/// Calculates accurate bounds for individual layout elements (staves) including stems, beams, ledger lines, curves, and other visual elements.
/// MUST run after stems, beams, and curves have been positioned.
/// This pass sets the Height property on staves based on their actual rendered extents.
/// </summary>
public class LayoutElementBoundsCalculationPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                CalculateStaffBounds(staff, context);
            }
        }
    }

    private static void CalculateStaffBounds(LayoutStaff staff, SvgContext context)
    {
        var (minY, maxY, height) = BoundsCalculator.CalculateStaffBounds(
            staff,
            staff.Y,
            context.StaffSpace);

        staff.Height = height;
    }
}
