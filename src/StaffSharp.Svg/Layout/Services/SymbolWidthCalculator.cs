namespace StaffSharp.Svg.Layout.Services;

using StaffSharp.Layout.Services;
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

        var baseWidth = symbol switch
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

        // Account for wide decorations (e.g., fermata, trill)
        if (symbol is NoteLayoutSymbol noteSymbol && noteSymbol.Note.Decorations.Count > 0)
        {
            var decorationWidth = GetDecorationWidth(noteSymbol.Note.Decorations, context);
            baseWidth = Math.Max(baseWidth, decorationWidth);
        }
        else if (symbol is ChordLayoutSymbol chordSymbol && chordSymbol.Chord.Decorations.Count > 0)
        {
            var decorationWidth = GetDecorationWidth(chordSymbol.Chord.Decorations, context);
            baseWidth = Math.Max(baseWidth, decorationWidth);
        }

        return baseWidth;
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

        // Clef
        var clefSymbol = new ClefLayoutSymbol { Clef = staff.CurrentClef };
        var clefWidth = CalculateSymbolWidth(clefSymbol, context);
        var clefSpacing = ClefCalculator.ClefSpacing(context);
        width += clefSpacing.Left + clefWidth + clefSpacing.Right;

        // Key signature (if not C major)
        if (staff.CurrentKeySignature != KeySignature.C)
        {
            var keySymbol = new KeySignatureLayoutSymbol { KeySignature = staff.CurrentKeySignature };
            var keyWidth = CalculateSymbolWidth(keySymbol, context);
            var keySpacing = KeySignatureService.KeySignatureSpacing(context);
            width += keySpacing.Left + keyWidth + keySpacing.Right;
        }

        // Time signature
        var timeSymbol = new TimeSignatureLayoutSymbol { TimeSignature = new TimeSignature(4, 4) };
        var timeWidth = CalculateSymbolWidth(timeSymbol, context);
        width += timeWidth;

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

    /// <summary>
    /// Gets the horizontal width required for decorations.
    /// Most articulations are centered and don't affect width, but some (like fermata) are wide.
    /// </summary>
    private static double GetDecorationWidth(IReadOnlyList<Decoration> decorations, SvgContext context)
    {
        var maxWidth = 0.0;

        foreach (var decoration in decorations)
        {
            var width = decoration switch
            {
                Decoration.Fermata => 3.0 * context.StaffSpace,  // Fermata is wide
                Decoration.Trill => 2.5 * context.StaffSpace,    // Trill can be wide
                _ => 0.0  // Most articulations don't add horizontal width
            };

            maxWidth = Math.Max(maxWidth, width);
        }

        return maxWidth;
    }
}
