namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;

/// <summary>
/// Pass to generate systems based on max width.
/// Breaks measures into multiple systems when they exceed the maximum width.
/// </summary>
internal class SystemGenerationPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        double currentY = context.Margins.Top;

        foreach (var system in model.Systems)
        {
            system.Y = currentY;
            system.X = context.Margins.Left;

            // Calculate system height based on actual staff heights, including inter-staff spacing
            double systemHeight = system.Staves.Sum(s => s.Height) + 
                (2.0 * context.StaffSpace * (system.Staves.Count - 1));

            currentY += systemHeight + (3.0 * context.StaffSpace); // System spacing
        }
    }
}