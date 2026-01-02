namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;

/// <summary>
/// Inserts clef, key signature, and time signature symbols at the start of each system after the first.
/// This pass MUST run after SystemBreakingPass (which creates multiple systems from staves)
/// and before the second HorizontalSpacingPass (which calculates final X positions).
/// </summary>
public class SystemSymbolInsertionPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        // Skip first system - it already has clef, key signature, and time signature from initial layout
        foreach (var system in model.Systems.Skip(1))
        {
            foreach (var staff in system.Staves)
            {
                InsertSystemStartSymbols(staff, model.Metadata, context);
            }
        }
    }

    private static void InsertSystemStartSymbols(LayoutStaff staff, ScoreMetadata? metadata, SvgContext context)
    {
        if (staff.Measures.Count == 0) return;

        var firstMeasure = staff.Measures[0];
        var symbolsToInsert = new List<LayoutSymbol>();

        // 1. Clef (always insert at start of new system)
        var clefSymbol = new ClefLayoutSymbol
        {
            Clef = staff.CurrentClef,
            TimePosition = -3.0  // Negative time positions sort before measure content
        };
        
        SetClefYPosition(clefSymbol, staff.CurrentClef, context);
        symbolsToInsert.Add(clefSymbol);

        // 2. Key signature (if not C major)
        if (staff.CurrentKeySignature != KeySignature.C)
        {
            var keySymbol = new KeySignatureLayoutSymbol
            {
                KeySignature = staff.CurrentKeySignature,
                TimePosition = -2.0,
                Y = 0  // Position at top of staff
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
                Y = 0  // Position at top of staff
            };

            symbolsToInsert.Add(timeSymbol);
        }

        // Insert symbols at the beginning of the measure
        // Insert in reverse order to maintain correct ordering (clef, then key, then time)
        for (int i = symbolsToInsert.Count - 1; i >= 0; i--)
        {
            firstMeasure.InsertSymbol(0, symbolsToInsert[i]);
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
