namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Svg;

/// <summary>
/// Assigns horizontal positions (X coordinates) to all symbols.
/// This pass runs AFTER SystemBreakingPass to assign final X positions.
/// It uses pre-calculated symbol widths from MeasureWidthCalculationPass and does NOT recalculate measure widths.
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
                        // Use pre-calculated spacing from MeasureWidthCalculationPass (NO recalculation!)
                        var firstSymbol = timeGroup.First();
                        var spacing = firstSymbol.Spacing;
                        var maxWidth = timeGroup.Max(s => s.Width);

                        // Position symbol: currentX + left spacing
                        var symbolX = currentX + spacing.Left;

                        // All symbols at this time get the same X position
                        foreach (var symbol in timeGroup)
                        {
                            symbol.X = symbolX;
                        }

                        // Advance by total width: left padding + symbol width + right padding
                        currentX += spacing.Left + maxWidth + spacing.Right;
                    }
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
