namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;

using StaffSharp.Layout.Services;
using StaffSharp.Notation;

/// <summary>
/// Pass to break measures into multiple systems when they exceed the maximum width.
/// Also inserts clef, key signature, and time signature symbols at the start of each new system.
/// Requires that widths are known, so the <see cref="MeasureWidthCalculationPass"/> must have been run.
/// </summary>
internal class SystemBreakingPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        if (model.Systems.Count == 0)
        {
            return;
        }

        // Process each system (typically only one before breaking)
        var newSystems = new List<LayoutSystem>();
        foreach (var system in model.Systems)
        {
            if (system.Staves.Count == 0)
            {
                continue;
            }

            // Calculate break points that work for ALL staves
            var breakPoints = CalculateUnifiedBreakPoints(system.Staves, context);

            // Break all staves at the same points
            var brokenStaves = new List<List<LayoutStaff>>();
            foreach (var staff in system.Staves)
            {
                brokenStaves.Add(BreakStaffAtPoints(staff, breakPoints, model.Metadata, context));
            }

            // All staves should now have the same number of systems
            var systemCount = brokenStaves[0].Count;

            for (int i = 0; i < systemCount; i++)
            {
                var newSystem = new LayoutSystem();
                
                foreach (var staffList in brokenStaves)
                {
                    newSystem.AddStaff(staffList[i]);
                }

                newSystems.Add(newSystem);
            }
        }

        // Replace the original systems with broken systems
        model.Systems.Clear();
        model.Systems.AddRange(newSystems);
    }

    /// <summary>
    /// Calculates unified break points that work for all staves in a system.
    /// Returns a list of measure indices where breaks should occur.
    /// </summary>
    private static List<int> CalculateUnifiedBreakPoints(List<LayoutStaff> staves, SvgContext context)
    {
        var breakPoints = new List<int>();

        if (staves.Count == 0)
        {
            return breakPoints;
        }

        // Get the maximum measure count across all staves
        var maxMeasureCount = staves.Max(s => s.Measures.Count);

        if (maxMeasureCount == 0)
        {
            return breakPoints;
        }

        // Calculate system start width
        var systemStartWidth = SymbolWidthCalculator.CalculateSystemStartWidth(staves[0], context);

        double currentWidth = context.Margins.Left + systemStartWidth;
        var firstMeasureInSystem = true;

        for (int measureIndex = 0; measureIndex < maxMeasureCount; measureIndex++)
        {
            // Find the widest measure at this index across all staves
            double maxMeasureWidth = 0;
            foreach (var staff in staves)
            {
                if (measureIndex < staff.Measures.Count)
                {
                    maxMeasureWidth = Math.Max(maxMeasureWidth, staff.Measures[measureIndex].Width);
                }
            }

            // Check if adding this measure would exceed max width
            if (currentWidth + maxMeasureWidth > context.MaxWidth && !firstMeasureInSystem)
            {
                // Record break point BEFORE this measure
                breakPoints.Add(measureIndex);
                currentWidth = context.Margins.Left + systemStartWidth;
                firstMeasureInSystem = true;
            }

            currentWidth += maxMeasureWidth;
            firstMeasureInSystem = false;
        }

        return breakPoints;
    }

    /// <summary>
    /// Breaks a staff at the specified measure indices.
    /// </summary>
    private static List<LayoutStaff> BreakStaffAtPoints(
        LayoutStaff staff, 
        List<int> breakPoints, 
        ScoreMetadata? metadata, 
        SvgContext context)
    {
        var result = new List<LayoutStaff>();
        var currentStaff = new LayoutStaff
        {
            CurrentClef = staff.CurrentClef,
            CurrentKeySignature = staff.CurrentKeySignature,
            PartIndex = staff.PartIndex,
            StaffNumber = staff.StaffNumber
        };

        result.Add(currentStaff);

        int nextBreakIndex = 0;
        bool isFirstSystem = true;

        for (int measureIndex = 0; measureIndex < staff.Measures.Count; measureIndex++)
        {
            // Check if we should break before this measure
            if (nextBreakIndex < breakPoints.Count && measureIndex == breakPoints[nextBreakIndex])
            {
                // Start a new system
                currentStaff = new LayoutStaff
                {
                    CurrentClef = staff.CurrentClef,
                    CurrentKeySignature = staff.CurrentKeySignature,
                    PartIndex = staff.PartIndex,
                    StaffNumber = staff.StaffNumber
                };

                result.Add(currentStaff);
                isFirstSystem = false;
                nextBreakIndex++;
            }

            var measure = staff.Measures[measureIndex];
            currentStaff.Measures.Add(measure);
            
            // Insert system-start symbols for systems after the first
            if (currentStaff.Measures.Count == 1 && !isFirstSystem)
            {
                InsertSystemStartSymbols(currentStaff, measure, metadata, context);
            }
        }

        return result;
    }

    private static void InsertSystemStartSymbols(LayoutStaff staff, LayoutMeasure measure, ScoreMetadata? metadata, SvgContext context)
    {
        // Insert symbols in reverse order so they end up sorted correctly
        // 3. Time signature (always show at start of new system)
        if (metadata?.TimeSignature != null)
        {
            measure.Symbols.Insert(0, TimeSignatureLayoutSymbol.Create(metadata.TimeSignature, context));
        }

        // 2. Key signature (if not C major)
        if (staff.CurrentKeySignature != KeySignature.C)
        {
            measure.Symbols.Insert(0, KeySignatureLayoutSymbol.Create(staff.CurrentKeySignature, staff.CurrentClef, context));
        }

        // 1. Clef (always insert at start of new system)
        measure.Symbols.Insert(0, ClefLayoutSymbol.Create(staff.CurrentClef, context));
    }
}
