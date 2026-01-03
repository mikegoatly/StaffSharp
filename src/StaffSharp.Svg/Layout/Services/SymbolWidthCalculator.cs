namespace StaffSharp.Svg.Layout.Services;

using StaffSharp.Notation;

/// <summary>
/// Provides width calculation services for layout symbols.
/// Centralizes all width and spacing calculations to ensure consistency across layout passes.
/// </summary>
internal static class SymbolWidthCalculator
{
    /// <summary>
    /// Calculate the base width of a symbol (no spacing applied).
    /// </summary>
    /// <param name="symbol">The symbol to calculate width for.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>The base width of the symbol in units.</returns>
    public static double CalculateSymbolWidth(LayoutSymbol symbol, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(context);

        return symbol switch
        {
            NoteLayoutSymbol note => GetDurationWidth(note.Note.Duration, context),
            RestLayoutSymbol rest => GetDurationWidth(rest.Rest.Duration, context),
            ChordLayoutSymbol chord => GetDurationWidth(chord.Chord.Duration, context),
            ClefLayoutSymbol => 2.2 * context.StaffSpace,
            KeySignatureLayoutSymbol keySymbol => GetKeySignatureWidth(keySymbol.KeySignature, context),
            TimeSignatureLayoutSymbol => 1.8 * context.StaffSpace,
            BarlineLayoutSymbol => 0.5 * context.StaffSpace,
            _ => context.StaffSpace
        };
    }

    /// <summary>
    /// Calculate spacing (padding) for a symbol.
    /// Notes/rests/chords get equal left/right padding to center them in their allocated space.
    /// Other symbols get right padding only (left-aligned).
    /// </summary>
    /// <param name="symbol">The symbol to calculate spacing for.</param>
    /// <param name="baseWidth">The base width of the symbol.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>The spacing (left and right padding) for the symbol.</returns>
    public static LayoutSpacing CalculateSpacing(LayoutSymbol symbol, double baseWidth, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(context);

        return symbol switch
        {
            NoteLayoutSymbol or RestLayoutSymbol or ChordLayoutSymbol =>
                // Center note in its allocated space
                CalculateNoteSpacing(baseWidth, context),

            _ =>
                // Non-note symbols: left-aligned with right padding
                new LayoutSpacing(0, context.StaffSpace)
        };
    }

    private static LayoutSpacing CalculateNoteSpacing(double baseWidth, SvgContext context)
    {
        // Total space allocated = baseWidth * NoteSpacing
        var totalSpace = baseWidth * context.NoteSpacing;

        // Equal padding on left and right centers the note
        var padding = (totalSpace - baseWidth) / 2;

        return new LayoutSpacing(padding, padding);
    }

    /// <summary>
    /// Calculate system-start width (clef + key sig + time sig).
    /// Replaces SystemBreakingPass.CalculateSystemStartWidth().
    /// </summary>
    /// <param name="clef">The clef to use.</param>
    /// <param name="keySignature">The key signature to use.</param>
    /// <param name="includeTimeSignature">Whether to include time signature in the width.</param>
    /// <param name="context">The rendering context.</param>
    /// <returns>The total width needed for system-start symbols in units.</returns>
    public static double CalculateSystemStartWidth(LayoutStaff staff, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        double width = 0;

        // Clef (use consistent width, not hardcoded 3.0)
        var clefSymbol = new ClefLayoutSymbol { Clef = staff.CurrentClef };
        var clefWidth = CalculateSymbolWidth(clefSymbol, context);
        var clefSpacing = CalculateSpacing(clefSymbol, clefWidth, context);
        width += clefSpacing.Left + clefWidth + clefSpacing.Right;

        // Key signature (if not C major)
        if (staff.CurrentKeySignature != KeySignature.C)
        {
            var keySymbol = new KeySignatureLayoutSymbol { KeySignature = staff.CurrentKeySignature };
            var keyWidth = CalculateSymbolWidth(keySymbol, context);
            var keySpacing = CalculateSpacing(keySymbol, keyWidth, context);
            width += keySpacing.Left + keyWidth + keySpacing.Right;
        }

        // Time signature
        var timeSymbol = new TimeSignatureLayoutSymbol { TimeSignature = new TimeSignature(4, 4) };
        var timeWidth = CalculateSymbolWidth(timeSymbol, context);
        var timeSpacing = CalculateSpacing(timeSymbol, timeWidth, context);
        width += timeSpacing.Left + timeWidth + timeSpacing.Right;

        return width;
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
