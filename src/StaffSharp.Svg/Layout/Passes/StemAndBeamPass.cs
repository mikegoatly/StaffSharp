namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Layout.Model;
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
                if (symbol is IStemmedSymbol stemmedSymbol)
                {
                    StemCalculator.CalculateStem(stemmedSymbol, staffBaseline, context);
                    FlagCalculator.CalculateFlag(stemmedSymbol, context);
                }
            }
        }

        // Process beam groups
        foreach (var group in beamGroups)
        {
            // Cast to IStemmedSymbol since beam groups only contain stemmed symbols
            var stemmedGroup = group.Cast<IStemmedSymbol>().ToList();

            if (stemmedGroup.Count > 1)
            {
                StemCalculator.CalculateBeamedGroupStems(group, staffBaseline, context);
            }
            else if (stemmedGroup.Count == 1)
            {
                StemCalculator.CalculateStem(stemmedGroup[0], staffBaseline, context);
                FlagCalculator.CalculateFlag(stemmedGroup[0], context);
            }
        }
    }

}