namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;

/// <summary>
/// Unifies measure widths across all staves in a system so that barlines align vertically.
/// For each measure position, finds the widest measure across all staves and scales
/// narrower measures to match, adjusting symbol spacing proportionally.
/// </summary>
internal class UnifiedMeasureWidthPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            // Only unify if there are multiple staves (grand staff, multi-voice, etc.)
            if (system.Staves.Count <= 1)
            {
                continue;
            }

            UnifyMeasureWidths(system);
        }
    }

    private static void UnifyMeasureWidths(LayoutSystem system)
    {
        // Get the maximum number of measures across all staves
        var maxMeasureCount = system.Staves.Max(s => s.Measures.Count);

        // For each measure position
        for (int measureIndex = 0; measureIndex < maxMeasureCount; measureIndex++)
        {
            var stavesWithMeasure = system.Staves.Where(s => measureIndex < s.Measures.Count).ToList();

            // Find the widest measure at this position across all staves
            var maxWidth = stavesWithMeasure
                .Select(s => s.Measures[measureIndex].Bounds.Width)
                .DefaultIfEmpty(0)
                .Max();

            // Update all measures at this position to use the max width
            foreach (var staff in stavesWithMeasure)
            {
                var measure = staff.Measures[measureIndex];
                var originalWidth = measure.Bounds.Width;

                // If this measure is narrower than the max, scale its spacing proportionally
                if (originalWidth > 0 && originalWidth < maxWidth)
                {
                    var scaleFactor = maxWidth / originalWidth;

                    // Scale all symbol spacing proportionally to fill the new width
                    foreach (var symbol in measure.Symbols)
                    {
                        symbol.Spacing = new LayoutSpacing(
                            symbol.Spacing.Left * scaleFactor,
                            symbol.Spacing.Right * scaleFactor
                        );
                    }

                    // Update measure width to the unified width
                    measure.Bounds = measure.Bounds with { Width = maxWidth };

                    // Adjust the staff's total width accordingly
                    staff.Bounds = measure.Bounds with { Width = maxWidth - originalWidth };
                }
            }
        }
    }
}
