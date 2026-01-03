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

        double currentY = context.Margins.Top;

        foreach (var system in model.Systems)
        {
            system.Y = currentY;
            system.X = context.Margins.Left;

            // Calculate system height based on actual staff heights
            double systemHeight = 0;
            foreach (var staff in system.Staves)
            {
                // Use the actual calculated staff height
                systemHeight += staff.Height;
                if (staff != system.Staves[^1]) // Not the last staff
                {
                    systemHeight += 2 * context.StaffSpace; // Inter-staff spacing
                }
            }
            
            currentY += systemHeight + (3 * context.StaffSpace); // System spacing
        }
    }
}