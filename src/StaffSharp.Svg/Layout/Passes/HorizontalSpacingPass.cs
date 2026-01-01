namespace StaffSharp.Svg.Layout.Passes;

using StaffSharp.Notation;
using StaffSharp.Svg;

/// <summary>
/// Assigns horizontal positions (X coordinates) to all symbols.
/// MVP version uses fixed spacing based on duration.
/// Handles multi-voice by aligning symbols at the same time position.
/// </summary>
public class HorizontalSpacingPass : ILayoutPass
{
    public void Run(LayoutModel model, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var system in model.Systems)
        {
            foreach (var staff in system.Staves)
            {
                double currentX = context.Margins.Left;
                staff.X = currentX;

                foreach (var measure in staff.Measures)
                {
                    measure.X = currentX;
                    double measureStartX = currentX;

                    // Group symbols by time position to handle multi-voice alignment
                    var symbolsByTime = measure.Symbols
                        .GroupBy(s => s.TimePosition)
                        .OrderBy(g => g.Key)
                        .ToList();

                    foreach (var timeGroup in symbolsByTime)
                    {
                        // Find the maximum width needed for symbols at this time
                        var maxWidth = timeGroup.Max(s => GetSymbolWidth(s, context));

                        // All symbols at this time get the same X position
                        foreach (var symbol in timeGroup)
                        {
                            symbol.X = currentX;
                            symbol.Width = GetSymbolWidth(symbol, context);
                        }

                        currentX += maxWidth + (0.5 * context.StaffSpace); // Fixed gap
                    }

                    measure.Width = currentX - measureStartX;
                }

                staff.Width = currentX - staff.X;
            }

            // Update system width (use the widest staff)
            if (system.Staves.Count > 0)
            {
                system.Width = system.Staves.Max(s => s.Width);
            }
        }
    }

    private static double GetSymbolWidth(LayoutSymbol symbol, SvgContext context)
    {
        var baseWidth = symbol switch
        {
            NoteLayoutSymbol noteSymbol => GetDurationWidth(noteSymbol.Note.Duration, context),
            RestLayoutSymbol restSymbol => GetDurationWidth(restSymbol.Rest.Duration, context),
            ChordLayoutSymbol chordSymbol => GetDurationWidth(chordSymbol.Chord.Duration, context),
            ClefLayoutSymbol => 3.0 * context.StaffSpace,
            KeySignatureLayoutSymbol keySymbol => GetKeySignatureWidth(keySymbol.KeySignature, context),
            TimeSignatureLayoutSymbol => 2.0 * context.StaffSpace,
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
        // For now, just return a fixed width
        // TODO: Calculate based on actual number of accidentals
        if (keySignature == KeySignature.C)
        {
            return 0;
        }

        // Approximate: each accidental is about 1 staff space wide
        return 3.0 * context.StaffSpace;
    }
}
