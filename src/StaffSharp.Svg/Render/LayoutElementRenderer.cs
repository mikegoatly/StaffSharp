namespace StaffSharp.Svg;

using System.Globalization;
using System.Xml.Linq;
using StaffSharp.Notation;
using StaffSharp.Svg.Layout;
using StaffSharp.Svg.Render;

internal abstract class LayoutElementRenderer<T>
{
    protected static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>
    /// Creates an SVG representation of the given layout element, returning the created element.
    /// </summary>
    public abstract XElement Render(T symbol, SvgContext context);

    protected static XElement CreateLine(double x1, double y1, double x2, double y2, string stroke = "black", double strokeWidth = 1D)
    {
        return new XElement(SvgNamespace + "line",
            new XAttribute("x1", x1.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("y1", y1.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("x2", x2.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("y2", y2.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("stroke", stroke),
            new XAttribute("stroke-width", strokeWidth.ToString(CultureInfo.InvariantCulture))
        );
    }

    /// <summary>
    /// Renders an accidental glyph at the specified position.
    /// </summary>
    protected static XElement RenderAccidental(Accidental accidental, double x, double y, SvgContext context)
    {
        var accidentalGlyph = accidental switch
        {
            Accidental.Sharp => MusicGlyphs.Sharp,
            Accidental.Flat => MusicGlyphs.Flat,
            Accidental.Natural => MusicGlyphs.Natural,
            _ => default
        };

        var transform = $"translate({x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)})";
        var accidentalElement = RenderGlyph(accidentalGlyph, 1.0, transform, context);
        return accidentalElement!;
    }

    /// <summary>
    /// Renders a glyph as an SVG path element with appropriate scaling.
    /// </summary>
    protected static XElement? RenderGlyph(GlyphInfo glyph, double targetHeightInStaffSpaces, string? transform, SvgContext context)
    {
        if (glyph.Path == null) return null;

        var targetHeight = targetHeightInStaffSpaces * context.StaffSpace;
        var scale = glyph.Height > 0 ? targetHeight / glyph.Height : 1.0;

        var scaleTransform = $"scale({scale.ToString(CultureInfo.InvariantCulture)})";
        var finalTransform = string.IsNullOrEmpty(transform)
            ? scaleTransform
            : $"{transform} {scaleTransform}";

        return new XElement(SvgNamespace + "path",
            new XAttribute("d", glyph.Path),
            new XAttribute("fill", "black"),
            new XAttribute("transform", finalTransform)
        );
    }

    /// <summary>
    /// Renders a stem for a note or chord symbol.
    /// </summary>
    protected static void RenderStem(XElement group, LayoutSymbol symbol, SvgContext context)
    {
        // Stem X is calculated in layout pass and stored in symbol.StemX (absolute coordinates)
        // Convert to relative coordinates (relative to the note/chord group's transform)
        var stemX = symbol.StemX - symbol.X;

        group.Add(new XElement(SvgNamespace + "line",
            new XAttribute("x1", stemX.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("y1", (symbol.StemY1 - symbol.Y).ToString(CultureInfo.InvariantCulture)),
            new XAttribute("x2", stemX.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("y2", (symbol.StemY2 - symbol.Y).ToString(CultureInfo.InvariantCulture)),
            new XAttribute("stroke", "black"),
            new XAttribute("stroke-width", "1.5")
        ));
    }

    /// <summary>
    /// Renders ledger lines for notes above or below the staff.
    /// </summary>
    protected static void RenderLedgerLines(XElement group, LayoutSymbol symbol, SvgContext context)
    {
        var lineSpacing = context.StaffSpace;

        for (int i = 0; i < symbol.LedgerLineCount; i++)
        {
            var lineY = symbol.LedgerLinesAbove
                ? -i * lineSpacing
                : i * lineSpacing;

            group.Add(new XElement(SvgNamespace + "line",
                new XAttribute("x1", (-context.StaffSpace * 0.4).ToString(CultureInfo.InvariantCulture)),
                new XAttribute("y1", lineY.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("x2", (context.StaffSpace * 1.4).ToString(CultureInfo.InvariantCulture)),
                new XAttribute("y2", lineY.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("stroke", "black"),
                new XAttribute("stroke-width", "1")
            ));
        }
    }

    /// <summary>
    /// Gets the appropriate notehead glyph for a duration.
    /// </summary>
    protected static GlyphInfo GetNoteheadGlyph(SymbolicDuration duration)
    {
        return duration.Base switch
        {
            NoteDurationBase.Whole => MusicGlyphs.NoteHeadWhole,
            NoteDurationBase.Half => MusicGlyphs.NoteHeadHalf,
            _ => MusicGlyphs.NoteHeadBlack
        };
    }

    /// <summary>
    /// Gets the appropriate rest glyph for a duration.
    /// </summary>
    protected static GlyphInfo GetRestGlyph(SymbolicDuration duration)
    {
        return duration.Base switch
        {
            NoteDurationBase.Whole => MusicGlyphs.WholeRest,
            NoteDurationBase.Half => MusicGlyphs.HalfRest,
            NoteDurationBase.Quarter => MusicGlyphs.QuarterRest,
            NoteDurationBase.Eighth => MusicGlyphs.EighthRest,
            NoteDurationBase.Sixteenth => MusicGlyphs.SixteenthRest,
            _ => MusicGlyphs.QuarterRest
        };
    }

    /// <summary>
    /// Gets the digit glyph for a character.
    /// </summary>
    protected static GlyphInfo? GetDigitGlyph(char digit)
    {
        return digit switch
        {
            '0' => MusicGlyphs.Digit0,
            '1' => MusicGlyphs.Digit1,
            '2' => MusicGlyphs.Digit2,
            '3' => MusicGlyphs.Digit3,
            '4' => MusicGlyphs.Digit4,
            '5' => MusicGlyphs.Digit5,
            '6' => MusicGlyphs.Digit6,
            '7' => MusicGlyphs.Digit7,
            '8' => MusicGlyphs.Digit8,
            '9' => MusicGlyphs.Digit9,
            _ => null
        };
    }

    /// <summary>
    /// Renders a sequence of digit glyphs for time signatures.
    /// </summary>
    protected static void RenderDigits(XElement parent, string digits, double x, double y, SvgContext context)
    {
        double currentX = x;
        var digitWidth = 0.8 * context.StaffSpace;

        foreach (var digit in digits)
        {
            var glyph = GetDigitGlyph(digit);
            if (glyph != null)
            {
                var transform = $"translate({currentX.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)})";
                var glyphElement = RenderGlyph(glyph.Value, 1.0, transform, context);
                if (glyphElement != null)
                {
                    parent.Add(glyphElement);
                }
                currentX += digitWidth;
            }
        }
    }
}
