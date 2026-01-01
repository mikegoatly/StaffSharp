namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;

/// <summary>
/// Pass to generate systems based on max width.
/// Breaks measures into multiple systems when they exceed the maximum width.
/// </summary>
public class SystemGenerationPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        // This pass runs BEFORE HorizontalSpacingPass, so we need to estimate widths
        // or run after and reorganize. For now, we'll just set Y positions.
        // TODO: Move this to run AFTER HorizontalSpacingPass so we have actual widths
        
        double currentY = context.Margins.Top;

        foreach (var system in model.Systems)
        {
            system.Y = currentY;
            system.X = context.Margins.Left;

            // Calculate system height based on staves
            double systemHeight = 0;
            foreach (var staff in system.Staves)
            {
                // Each staff is 4 staff spaces tall (5 lines)
                // Plus spacing between staves
                systemHeight += 4 * context.StaffSpace;
                if (staff != system.Staves[^1]) // Not the last staff
                {
                    systemHeight += 2 * context.StaffSpace; // Inter-staff spacing
                }
            }
            system.Height = systemHeight;
            
            currentY += systemHeight + (3 * context.StaffSpace); // System spacing
        }
    }
}