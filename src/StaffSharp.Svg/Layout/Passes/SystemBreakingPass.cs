namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;
using StaffSharp.Svg.Layout.Services;

/// <summary>
/// Pass to break measures into multiple systems when they exceed the maximum width.
/// Runs AFTER HorizontalSpacingPass so measure widths are known.
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
                brokenStaves.Add(BreakStaffIntoSystems(staff, context));
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

    private static List<LayoutStaff> BreakStaffIntoSystems(LayoutStaff staff, SvgContext context)
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
            }

            // Clone the measure and add to current staff
            // For now, we'll just add the reference (we're not modifying the measure)
            currentStaff.AddMeasure(measure);
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
}
