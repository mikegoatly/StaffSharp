namespace StaffSharp.Layout.Passes;

using StaffSharp;
using StaffSharp.Layout;
using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;

/// <summary>
/// Calculates stem directions, lengths, and beam positions for notes.
/// </summary>
internal class StemAndBeamPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        foreach (var staff in model.Systems.SelectMany(s => s.Staves))
        {
            // Staff baseline (middle line)
            var staffBaseline = staff.Y + (2.0 * context.StaffSpace);

            foreach (var measure in staff.Measures)
            {
                ProcessMeasure(measure, staffBaseline, context);
            }
        }
    }

    private static void ProcessMeasure(LayoutMeasure measure, double staffBaseline, SvgContext context)
    {
        // Group beamable notes together, respecting voice boundaries and beat structure
        var beamGroups = BeamGrouper.GroupBeamableNotes(measure.Symbols, measure.TimeSignature);

        // Process non-beamed symbols
        foreach (var symbol in measure.Symbols
            .Where(s => !BeamGrouper.IsBeamable(s) && StemCalculator.RequiresStem(s))
            .OfType<IStemmedSymbol>())
        {
            StemCalculator.CalculateStem(symbol, staffBaseline, context);
            FlagCalculator.CalculateFlag(symbol, context);
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
                // Cast to IStemmedSymbol since beam groups only contain stemmed symbols
                var stemmedSymbol = (IStemmedSymbol)group[0];
                StemCalculator.CalculateStem(stemmedSymbol, staffBaseline, context);
                FlagCalculator.CalculateFlag(stemmedSymbol, context);
            }
        }
    }
}