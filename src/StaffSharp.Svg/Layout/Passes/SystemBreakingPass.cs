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
            // Break each staff independently, then align them
            var brokenStaves = new List<List<LayoutStaff>>();
            
            foreach (var staff in system.Staves)
            {
                brokenStaves.Add(BreakStaffIntoSystems(staff, model.Metadata, context));
            }

            // All staves should have the same number of systems
            var systemCount = brokenStaves.Max(staves => staves.Count);

            for (int i = 0; i < systemCount; i++)
            {
                var newSystem = new LayoutSystem();
                
                foreach (var staffList in brokenStaves)
                {
                    if (i < staffList.Count)
                    {
                        newSystem.AddStaff(staffList[i]);
                    }
                }

                newSystems.Add(newSystem);
            }
        }

        // Replace the original systems with broken systems
        model.ReplaceSystems(newSystems);
    }

    private static List<LayoutStaff> BreakStaffIntoSystems(LayoutStaff staff, ScoreMetadata? metadata, SvgContext context)
    {
        var result = new List<LayoutStaff>();
        var currentStaff = new LayoutStaff
        {
            CurrentClef = staff.CurrentClef,
            CurrentKeySignature = staff.CurrentKeySignature
        };

        result.Add(currentStaff);

        double currentWidth = context.Margins.Left;
        var firstMeasureInSystem = true;
        var isFirstSystem = true;

        // Calculate width for system-start symbols (clef + key signature + time signature)
        var systemStartWidth = SymbolWidthCalculator.CalculateSystemStartWidth(staff, context);

        currentWidth += systemStartWidth;

        foreach (var measure in staff.Measures)
        {
            var measureWidth = measure.Width;

            // Check if adding this measure would exceed max width
            if (currentWidth + measureWidth > context.MaxWidth && !firstMeasureInSystem)
            {
                // Start a new system
                currentStaff = new LayoutStaff
                {
                    CurrentClef = staff.CurrentClef,
                    CurrentKeySignature = staff.CurrentKeySignature
                };

                result.Add(currentStaff);
                currentWidth = context.Margins.Left + systemStartWidth;
                firstMeasureInSystem = true;
                isFirstSystem = false;
            }

            // Clone the measure and add to current staff
            // For now, we'll just add the reference (we're not modifying the measure)
            currentStaff.AddMeasure(measure);
            
            // Insert system-start symbols for systems after the first
            if (firstMeasureInSystem && !isFirstSystem && currentStaff.Measures.Count == 1)
            {
                InsertSystemStartSymbols(currentStaff, measure, metadata, context);
            }
            
            currentWidth += measureWidth;
            firstMeasureInSystem = false;
        }

        return result;
    }


    private static void InsertSystemStartSymbols(LayoutStaff staff, LayoutMeasure measure, ScoreMetadata? metadata, SvgContext context)
    {
        // Insert symbols in reverse order so they end up sorted correctly
        // 3. Time signature (always show at start of new system)
        if (metadata?.TimeSignature != null)
        {
            measure.InsertSymbol(0, TimeSignatureLayoutSymbol.Create(metadata.TimeSignature, context));
        }

        // 2. Key signature (if not C major)
        if (staff.CurrentKeySignature != KeySignature.C)
        {
            measure.InsertSymbol(0, KeySignatureLayoutSymbol.Create(staff.CurrentKeySignature, staff.CurrentClef, context));
        }

        // 1. Clef (always insert at start of new system)
        measure.InsertSymbol(0, ClefLayoutSymbol.Create(staff.CurrentClef, context));
    }
}
