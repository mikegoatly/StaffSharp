namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;
using StaffSharp.Svg.Layout.Services;

/// <summary>
/// Pass to break measures into multiple systems when they exceed the maximum width.
/// Also inserts clef, key signature, and time signature symbols at the start of each new system.
/// Runs AFTER MeasureWidthCalculationPass so measure widths are known.
/// </summary>
public class SystemBreakingPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        if (model.Systems.Count == 0)
        {
            return;
        }

        // Process each system (typically only one before breaking)
        var originalSystems = model.Systems.ToList();
        var newSystems = new List<LayoutSystem>();

        foreach (var system in originalSystems)
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
        var systemStartWidth = CalculateSystemStartWidth(staff, context);
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

    private static double CalculateSystemStartWidth(LayoutStaff staff, SvgContext context)
    {
        double width = 0;

        // Clef width
        width += 3.0 * context.StaffSpace;

        // Key signature width
        if (staff.CurrentKeySignature != KeySignature.C)
        {
            width += KeySignatureService.CalculateWidth(staff.CurrentKeySignature, context.StaffSpace);
        }

        // Time signature width
        width += 2.0 * context.StaffSpace;

        return width;
    }

    private static void InsertSystemStartSymbols(LayoutStaff staff, LayoutMeasure measure, ScoreMetadata? metadata, SvgContext context)
    {
        var symbolsToInsert = new List<LayoutSymbol>();

        // 1. Clef (always insert at start of new system)
        var clefSymbol = new ClefLayoutSymbol
        {
            Clef = staff.CurrentClef,
            TimePosition = -3.0,  // Negative time positions sort before measure content
            Width = 2.2 * context.StaffSpace
        };
        
        SetClefYPosition(clefSymbol, staff.CurrentClef, context);
        symbolsToInsert.Add(clefSymbol);

        // 2. Key signature (if not C major)
        if (staff.CurrentKeySignature != KeySignature.C)
        {
            var keySymbol = new KeySignatureLayoutSymbol
            {
                KeySignature = staff.CurrentKeySignature,
                Clef = staff.CurrentClef,
                TimePosition = -2.0,
                Y = 0,  // Position at top of staff
                Width = KeySignatureService.CalculateWidth(staff.CurrentKeySignature, context.StaffSpace)
            };

            symbolsToInsert.Add(keySymbol);
        }

        // 3. Time signature (always show at start of new system)
        if (metadata?.TimeSignature != null)
        {
            var timeSymbol = new TimeSignatureLayoutSymbol
            {
                TimeSignature = metadata.TimeSignature,
                TimePosition = -1.0,
                Y = 0,  // Position at top of staff
                Width = 1.8 * context.StaffSpace
            };

            symbolsToInsert.Add(timeSymbol);
        }

        // Insert symbols at the beginning of the measure
        // Insert in reverse order to maintain correct ordering (clef, then key, then time)
        for (int i = symbolsToInsert.Count - 1; i >= 0; i--)
        {
            measure.InsertSymbol(0, symbolsToInsert[i]);
        }
    }

    private static void SetClefYPosition(ClefLayoutSymbol symbol, Clef clef, SvgContext context)
    {
        // Match the Y positioning logic from VerticalPositionPass
        symbol.Y = clef switch
        {
            Clef.Treble => 3 * context.StaffSpace,  // Position at G line
            Clef.Bass => 2 * context.StaffSpace,    // Position at middle line (F)
            _ => 2 * context.StaffSpace             // Default to middle line
        };
    }
}
