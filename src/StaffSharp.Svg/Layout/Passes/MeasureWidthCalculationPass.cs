namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;
using StaffSharp.Svg.Layout.Services;

/// <summary>
/// Calculates measure widths based on symbol durations and types.
/// This pass ONLY calculates widths and does NOT assign X positions.
/// It runs before SystemBreakingPass to provide the width information needed for line breaking decisions.
/// </summary>
public class MeasureWidthCalculationPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                foreach (var measure in staff.Measures)
                {
                    CalculateMeasureWidth(measure, context);
                }
            }
        }
    }

    private static void CalculateMeasureWidth(LayoutMeasure measure, SvgContext context)
    {
        // Group symbols by time position to handle multi-voice alignment
        var symbolsByTime = measure.Symbols
            .GroupBy(s => s.TimePosition)
            .OrderBy(g => g.Key)
            .ToList();

        double totalWidth = 0;

        foreach (var timeGroup in symbolsByTime)
        {
            // Find the maximum width needed for symbols at this time
            var maxWidth = timeGroup.Max(s => GetSymbolWidth(s, context));

            // Set individual symbol widths
            foreach (var symbol in timeGroup)
            {
                symbol.Width = GetSymbolWidth(symbol, context);
            }

            // Add to total width with spacing
            totalWidth += maxWidth + (0.3 * context.StaffSpace); // Fixed gap between elements
        }

        measure.Width = totalWidth;
    }

    private static double GetSymbolWidth(LayoutSymbol symbol, SvgContext context)
    {
        var baseWidth = symbol switch
        {
            NoteLayoutSymbol noteSymbol => GetDurationWidth(noteSymbol.Note.Duration, context),
            RestLayoutSymbol restSymbol => GetDurationWidth(restSymbol.Rest.Duration, context),
            ChordLayoutSymbol chordSymbol => GetDurationWidth(chordSymbol.Chord.Duration, context),
            ClefLayoutSymbol => 2.2 * context.StaffSpace,
            KeySignatureLayoutSymbol keySymbol => GetKeySignatureWidth(keySymbol.KeySignature, context),
            TimeSignatureLayoutSymbol => 1.8 * context.StaffSpace,
            BarlineLayoutSymbol => 0.5 * context.StaffSpace,
            _ => context.StaffSpace
        };

        // Add extra width for accidentals
        var accidentalWidth = 0.0;
        if (symbol.Accidental.HasValue)
        {
            accidentalWidth = 1.5 * context.StaffSpace;
        }
        else if (symbol is ChordLayoutSymbol chordSymbol && chordSymbol.Accidentals.Count > 0)
        {
            // Account for multiple accidentals in chords
            var maxOffset = chordSymbol.AccidentalXOffsets.Count > 0
                ? Math.Abs(chordSymbol.AccidentalXOffsets.Min())
                : 0;
            accidentalWidth = maxOffset + (1.0 * context.StaffSpace);
        }

        return baseWidth + accidentalWidth;
    }

    private static double GetDurationWidth(SymbolicDuration duration, SvgContext context)
    {
        // Get duration in beats (quarter note = 1.0)
        var rational = duration.ToBeats();
        var beats = (double)rational.Numerator / rational.Denominator;

        // Scale width based on duration (more duration = more space)
        // Quarter note gets 2.0 staff spaces
        var baseWidth = beats * 2.0 * context.StaffSpace;

        // Minimum width for readability
        return Math.Max(baseWidth, 1.5 * context.StaffSpace);
    }

    private static double GetKeySignatureWidth(KeySignature keySignature, SvgContext context)
    {
        return KeySignatureService.CalculateWidth(keySignature, context.StaffSpace);
    }
}
