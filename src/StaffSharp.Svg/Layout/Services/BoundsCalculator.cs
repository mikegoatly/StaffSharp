namespace StaffSharp.Layout.Services;

using StaffSharp.Layout.Model;

/// <summary>
/// Service for calculating accurate bounds including stems, beams, ledger lines, and other visual elements.
/// </summary>
internal static class BoundsCalculator
{
    /// <summary>
    /// Calculates the minimum and maximum Y coordinates for a symbol, including all visual elements
    /// (noteheads, stems, ledger lines, etc.).
    /// </summary>
    /// <param name="symbol">The symbol to calculate bounds for</param>
    /// <param name="staffSpace">The staff space unit for calculating ledger line extents</param>
    /// <returns>Tuple of (MinY, MaxY) coordinates</returns>
    public static (double MinY, double MaxY) CalculateSymbolBounds(LayoutSymbol symbol, double staffSpace)
    {
        var minY = symbol.Bounds.Y;
        var maxY = symbol.Bounds.Y;

        // Include stem bounds
        if (symbol is IStemmedSymbol stemmedSymbol && (stemmedSymbol.Stem.Y1 != 0 || stemmedSymbol.Stem.Y2 != 0))
        {
            minY = Math.Min(minY, Math.Min(stemmedSymbol.Stem.Y1, stemmedSymbol.Stem.Y2));
            maxY = Math.Max(maxY, Math.Max(stemmedSymbol.Stem.Y1, stemmedSymbol.Stem.Y2));
        }

        // Include ledger lines
        if (symbol.LedgerLineCount > 0)
        {
            var ledgerExtent = symbol.LedgerLineCount * staffSpace;
            if (symbol.LedgerLinesAbove)
            {
                minY = Math.Min(minY, symbol.Bounds.Y - ledgerExtent);
            }
            else
            {
                maxY = Math.Max(maxY, symbol.Bounds.Y + ledgerExtent);
            }
        }

        // Include chord notehead extents
        if (symbol is ChordLayoutSymbol chordSymbol && chordSymbol.NoteheadYPositions.Count > 0)
        {
            minY = Math.Min(minY, chordSymbol.NoteheadYPositions.Min());
            maxY = Math.Max(maxY, chordSymbol.NoteheadYPositions.Max());
        }

        // Add some padding for the symbol itself (noteheads have height)
        maxY = Math.Max(maxY, symbol.Bounds.Y + staffSpace);

        return (minY, maxY);
    }

    /// <summary>
    /// Calculates the minimum and maximum Y coordinates for a curve.
    /// </summary>
    /// <param name="curve">The curve to calculate bounds for</param>
    /// <returns>Tuple of (MinY, MaxY) coordinates</returns>
    public static (double MinY, double MaxY) CalculateCurveBounds(LayoutCurve curve)
    {
        // For a quadratic Bézier curve, extrema can occur at start, end, or apex
        var minY = Math.Min(Math.Min(curve.Bounds.Y, curve.EndY), curve.ApexY);
        var maxY = Math.Max(Math.Max(curve.Bounds.Y, curve.EndY), curve.ApexY);
        return (minY, maxY);
    }

    /// <summary>
    /// Calculates bounds for an entire staff by examining all symbols within all measures.
    /// </summary>
    /// <param name="staff">The staff to calculate bounds for</param>
    /// <param name="staffY">The Y position of the staff origin</param>
    /// <param name="staffSpace">The staff space unit</param>
    /// <returns>Tuple of (MinY, MaxY, Height)</returns>
    public static (double MinY, double MaxY, double Height) CalculateStaffBounds(
        LayoutStaff staff,
        double staffY,
        double staffSpace)
    {
        // Start with the staff lines themselves (5 lines = 4 spaces)
        var minY = staffY;
        var maxY = staffY + (4 * staffSpace);

        // Examine all symbols in all measures
        foreach (var measure in staff.Measures)
        {
            foreach (var symbol in measure.Symbols)
            {
                var (symbolMinY, symbolMaxY) = CalculateSymbolBounds(symbol, staffSpace);

                // Convert symbol-relative coordinates to absolute coordinates
                var absoluteMinY = staffY + symbolMinY;
                var absoluteMaxY = staffY + symbolMaxY;

                minY = Math.Min(minY, absoluteMinY);
                maxY = Math.Max(maxY, absoluteMaxY);
            }

            // Include curves (ties, slurs)
            foreach (var curve in measure.Curves)
            {
                var (curveMinY, curveMaxY) = CalculateCurveBounds(curve);
                var absoluteCurveMinY = staffY + curveMinY;
                var absoluteCurveMaxY = staffY + curveMaxY;
                minY = Math.Min(minY, absoluteCurveMinY);
                maxY = Math.Max(maxY, absoluteCurveMaxY);
            }
        }

        var height = maxY - minY;
        return (minY, maxY, height);
    }

    /// <summary>
    /// Calculates the total height of a system by summing staff heights and inter-staff spacing.
    /// </summary>
    /// <param name="system">The system to calculate height for</param>
    /// <param name="interStaffSpacing">Space between staves (typically 2-3 staff spaces)</param>
    /// 
    /// <returns>Total system height</returns>
    public static double CalculateSystemHeight(
        LayoutSystem system,
        double interStaffSpacing)
    {
        if (system.Staves.Count == 0)
        {
            return 0;
        }

        double totalHeight = 0;
        for (int i = 0; i < system.Staves.Count; i++)
        {
            var staff = system.Staves[i];
            totalHeight += staff.Bounds.Height;

            // Add inter-staff spacing (except after last staff)
            if (i < system.Staves.Count - 1)
            {
                totalHeight += interStaffSpacing;
            }
        }

        return totalHeight;
    }
}
