namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Svg;

/// <summary>
/// Assigns horizontal positions (X coordinates) to all symbols, measures, and staves.
/// This pass runs AFTER SystemSymbolInsertionPass to calculate final positions.
/// It assumes that symbol widths have already been calculated by MeasureWidthCalculationPass.
/// </summary>
public class HorizontalPositionPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                double currentX = context.Margins.Left;
                staff.X = currentX;

                foreach (var measure in staff.Measures)
                {
                    measure.X = currentX;
                    double measureStartX = currentX;

                    // Group symbols by time position to handle multi-voice alignment
                    var symbolsByTime = measure.Symbols
                        .GroupBy(s => s.TimePosition)
                        .OrderBy(g => g.Key)
                        .ToList();

                    foreach (var timeGroup in symbolsByTime)
                    {
                        // Find the maximum width from already-calculated symbol widths
                        var maxWidth = timeGroup.Max(s => s.Width);

                        // All symbols at this time get the same X position
                        foreach (var symbol in timeGroup)
                        {
                            symbol.X = currentX;
                        }

                        currentX += maxWidth + (0.3 * context.StaffSpace); // Fixed gap between elements
                    }

                    // Recalculate measure width based on actual positions
                    measure.Width = currentX - measureStartX;
                }

                staff.Width = currentX - staff.X;
            }

            // Update system width (use the widest staff)
            if (system.Staves.Count > 0)
            {
                system.Width = system.Staves.Max(s => s.Width);
            }
        }
    }
}
