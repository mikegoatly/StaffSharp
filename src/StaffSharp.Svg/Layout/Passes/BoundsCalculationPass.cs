namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Svg;
using StaffSharp.Svg.Layout.Services;

/// <summary>
/// Calculates accurate bounds for all layout elements including stems, beams, ledger lines, and other visual elements.
/// MUST run after all other layout passes that set positions (stems, beams, etc.).
/// This pass sets the Height property on symbols, staves, and systems based on their actual rendered extents.
/// </summary>
public class BoundsCalculationPass : ILayoutPass
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
            CalculateSystemBounds(system, context);
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

    private static void CalculateSystemBounds(LayoutSystem system, SvgContext context)
    {
        var interStaffSpacing = 2 * context.StaffSpace;
        system.Height = BoundsCalculator.CalculateSystemHeight(
            system,
            context.StaffSpace,
            interStaffSpacing);
    }
}
