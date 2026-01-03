namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Svg;
using StaffSharp.Svg.Layout.Services;

/// <summary>
/// Calculates the final layout bounds for systems.
/// MUST run after SystemGenerationPass has positioned systems vertically.
/// This pass sets the Height property on systems based on their constituent staves.
/// </summary>
public class LayoutBoundsCalculationPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            CalculateSystemBounds(system, context);
        }
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
