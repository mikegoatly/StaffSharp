namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Svg;
using StaffSharp.Svg.Layout.Services;

/// <summary>
/// Calculates measure widths based on symbol durations and types.
/// This pass ONLY calculates widths and does NOT assign X positions.
/// It runs before SystemBreakingPass to provide the width information needed for line breaking decisions.
/// </summary>
public class MeasureWidthCalculationPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                foreach (var measure in staff.Measures)
                {
                    CalculateMeasureWidth(measure, context);
                }
            }
        }
    }

    private static void CalculateMeasureWidth(LayoutMeasure measure, SvgContext context)
    {
        // Group symbols by time position to handle multi-voice alignment
        var symbolsByTime = measure.Symbols
            .GroupBy(s => s.TimePosition)
            .OrderBy(g => g.Key)
            .ToList();

        double totalWidth = 0;

        foreach (var timeGroup in symbolsByTime)
        {
            // Find the maximum width needed for symbols at this time
            var maxWidth = timeGroup.Max(s => SymbolWidthCalculator.CalculateSymbolWidth(s, context));

            // Set individual symbol widths and spacing
            foreach (var symbol in timeGroup)
            {
                symbol.Width = SymbolWidthCalculator.CalculateSymbolWidth(symbol, context);
                symbol.Spacing = SymbolWidthCalculator.CalculateSpacing(symbol, symbol.Width, context);
            }

            // Calculate total width for this time position (use max width for spacing calculation)
            var spacing = SymbolWidthCalculator.CalculateSpacing(
                timeGroup.First(),
                maxWidth,
                context);

            // Total width = left padding + symbol width + right padding
            totalWidth += spacing.Left + maxWidth + spacing.Right;
        }

        measure.Width = totalWidth;
    }
}
