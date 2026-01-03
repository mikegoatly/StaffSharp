namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;
using StaffSharp.Svg.Layout.Services;

/// <summary>
/// Calculates stem directions, lengths, and beam positions for notes.
/// </summary>
public class StemAndBeamPass : ILayoutPass
{

    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                // Staff baseline (middle line)
                var staffBaseline = staff.Y + (2.0 * context.StaffSpace);

                foreach (var measure in staff.Measures)
                {
                    ProcessMeasure(measure, staffBaseline, context);
                }
            }
        }
    }

    private static void ProcessMeasure(LayoutMeasure measure, double staffBaseline, SvgContext context)
    {
        // Group beamable notes together, respecting voice boundaries and beat structure
        var beamGroups = BeamGrouper.GroupBeamableNotes(measure.Symbols, measure.TimeSignature);

        // Process non-beamed symbols
        foreach (var symbol in measure.Symbols)
        {
            if (!BeamGrouper.IsBeamable(symbol) && StemCalculator.RequiresStem(symbol))
            {
                StemCalculator.CalculateStem(symbol, staffBaseline, context);
            }
        }

        // Process beam groups
        foreach (var group in beamGroups)
        {
            if (group.Count > 1)
            {
                StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, context);
            }
            else if (group.Count == 1)
            {
                StemCalculator.CalculateStem(group[0], staffBaseline, context);
            }
        }
    }

}