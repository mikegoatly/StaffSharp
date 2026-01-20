namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;

/// <summary>
/// Calculates the final layout bounds for systems.
/// MUST run after SystemGenerationPass has positioned systems vertically.
/// This pass sets the Height property on systems based on their constituent staves.
/// </summary>
internal class LayoutBoundsCalculationPass : ILayoutPass
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
        var interStaffSpacing = 2.0 * context.StaffSpace;

        // TODO - I wonder if this should be a method on LayoutSystem, maybe RecalculateBounds(recalcWidth, recalcHeight)?
        system.Bounds = system.Bounds with
        {
            Height = BoundsCalculator.CalculateSystemHeight(system, interStaffSpacing)
        };
    }
}
