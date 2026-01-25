namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;
using StaffSharp.Notation;

internal abstract class LayoutElementRenderer<T>
{
    protected static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>
    /// Creates an SVG representation of the given layout element, returning the created element.
    /// </summary>
    public abstract XElement Render(T symbol, SvgContext context);

    protected static XElement CreateLine(double x1, double y1, double x2, double y2, string stroke = "black", double strokeWidth = 1D, string? symbolId = null)
    {
        return AddId(
            symbolId,
            new XElement(SvgNamespace + "line",
                new XAttribute("x1", $"{x1:F2}"),
                new XAttribute("y1", $"{y1:F2}"),
                new XAttribute("x2", $"{x2:F2}"),
                new XAttribute("y2", $"{y2:F2}"),
                new XAttribute("stroke", stroke),
                new XAttribute("stroke-width", $"{strokeWidth:F2}")
            ));
    }

    protected static void AddBoundsRectangle(XElement group, Bounds bounds, string color)
    {
        var rectangle = new XElement(SvgNamespace + "rect",
            new XAttribute("x", bounds.X),
            new XAttribute("y", bounds.Y),
            new XAttribute("width", bounds.Width),
            new XAttribute("height", bounds.Height),
            new XAttribute("fill", "none"),
            new XAttribute("stroke", color),
            new XAttribute("stroke-width", 1),
            new XAttribute("stroke-dasharray", "4,1")
        );

        group.Add(rectangle);
    }

    /// <summary>
    /// Renders an accidental glyph at the specified position.
    /// </summary>
    protected static XElement RenderAccidental(Accidental accidental, double x, double y, SvgContext context, string? symbolId = null)
    {
        var accidentalGlyph = accidental switch
        {
            Accidental.Sharp => MusicGlyphs.Sharp,
            Accidental.Flat => MusicGlyphs.Flat,
            Accidental.Natural => MusicGlyphs.Natural,
            _ => default
        };

        var transform = CreateTranslate(x, y);
        var accidentalElement = RenderGlyph(accidentalGlyph, 2.0, transform, context, symbolId);
        return accidentalElement!;
    }

    /// <summary>
    /// Renders a glyph as an SVG use element referencing a shared definition.
    /// </summary>
    protected static XElement? RenderGlyph(GlyphInfo glyph, double targetHeightInStaffSpaces, string? transform, SvgContext context, string? symbolId = null)
    {
        if (glyph.Path == null)
        {
            return null;
        }

        var targetHeight = targetHeightInStaffSpaces * context.StaffSpace;
        var scale = glyph.Height > 0 ? targetHeight / glyph.Height : 1.0;

        return RenderGlyph(glyph, transform, context, scale, symbolId);
    }

    protected static XElement? RenderGlyph(GlyphInfo glyph, Bounds bounds, string? transform, SvgContext context, string? symbolId = null)
    {
        if (glyph.Path == null)
        {
            return null;
        }

        var scale = glyph.Height > 0 ? bounds.Height / glyph.Height : 1.0;
        return RenderGlyph(glyph, transform, context, scale, symbolId);
    }

    private static XElement RenderGlyph(GlyphInfo glyph, string? transform, SvgContext context, double scale, string? symbolId = null)
    {
        // Register this glyph for deduplication
        context.RegisterGlyph(glyph);

        var scaleTransform = $"scale({scale:F2})";
        var finalTransform = string.IsNullOrEmpty(transform)
            ? scaleTransform
            : $"{transform} {scaleTransform}";

        var useElement = new XElement(SvgNamespace + "use",
            new XAttribute("href", $"#{glyph.Id}"),
            new XAttribute("fill", context.Foreground),
            new XAttribute("transform", finalTransform)
        );

        // Add data-symbol-id for dynamic highlighting support
        return AddId(symbolId, useElement);
    }

    private static XElement AddId(string? symbolId, XElement element)
    {
        if (symbolId != null)
        {
            element.Add(new XAttribute("data-symbol-id", symbolId));
        }

        return element;
    }

    /// <summary>
    /// Renders a stem for a note or chord symbol.
    /// </summary>
    protected static void RenderStem(XElement group, IStemmedSymbol symbol)
    {
        // Stem X is calculated in layout pass and stored in symbol.Stem (absolute coordinates)
        // Convert to relative coordinates (relative to the note/chord group's transform)
        var stemX = symbol.Stem.X - symbol.Bounds.X;

        var symbolCenter = symbol.Bounds.Y;
        group.Add(
            CreateLine(
                stemX,
                symbol.Stem.Y1 - symbolCenter,
                stemX,
                symbol.Stem.Y2 - symbolCenter,
                strokeWidth: 1.5,
                symbolId: symbol.Id));
    }

    /// <summary>
    /// Renders flags for a note or chord symbol.
    /// </summary>
    protected static void RenderFlag(XElement group, IStemmedSymbol symbol)
    {
        if (!symbol.Beam.RequiresFlag || symbol.Beam.FlagCount == 0)
        {
            return;
        }

        // Build the flag path procedurally
        var flagPath = FlagPathBuilder.BuildFlagPath(
            symbol.Beam.FlagCount,
            symbol.Stem.Up,
            isGraceNote: false,
            useStraightFlags: false);

        if (string.IsNullOrEmpty(flagPath))
        {
            return;
        }

        // Position flag at the stem endpoint (relative to note/chord origin)
        var layoutElement = (LayoutElement)symbol;
        var flagX = symbol.Stem.X - layoutElement.Bounds.X;
        var flagY = symbol.Stem.Y2 - layoutElement.Bounds.Y;

        var flagElement = new XElement(SvgNamespace + "path",
            new XAttribute("d", flagPath),
            new XAttribute("fill", "black"),
            new XAttribute("transform", CreateTranslate(flagX, flagY))
        );

        group.Add(AddId(symbol.Id, flagElement));
    }

    /// <summary>
    /// Renders ledger lines for notes above or below the staff.
    /// </summary>
    protected static void RenderLedgerLines(XElement group, LayoutSymbol symbol, SvgContext context)
    {
        var lineSpacing = context.StaffSpace;

        for (int i = 0; i < symbol.LedgerLineCount; i++)
        {
            // Start from the first ledger line offset and draw toward the staff
            // For lines above: positive Y moves toward staff (downward)
            // For lines below: positive Y moves toward staff (upward)
            var lineY = symbol.LedgerLinesAbove
                ? symbol.FirstLedgerLineOffsetY + (i * lineSpacing)
                : symbol.FirstLedgerLineOffsetY - (i * lineSpacing);

            group.Add(
                CreateLine(
                    -context.StaffSpace * 0.4,
                    lineY,
                    context.StaffSpace * 1.4,
                    lineY
            ));
        }
    }

    /// <summary>
    /// Renders decorations/articulations for a note or chord.
    /// </summary>
    protected static void RenderDecorations(XElement group, AugmentationDottedLayoutSymbol symbol, SvgContext context)
    {
        if (symbol is StemmedSymbol stemmedSymbol)
        {
            foreach (var (type, decorationBounds) in stemmedSymbol.Decorations)
            {
                var decorationElement = RenderDecoration(type, decorationBounds.RelativeTo(symbol.Bounds), context);
                if (decorationElement != null)
                {
                    group.Add(AddId(symbol.Id, decorationElement));
                }
            }
        }
    }

    /// <summary>
    /// Renders a single decoration glyph.
    /// </summary>
    protected static XElement? RenderDecoration(Decoration decoration, Bounds bounds, SvgContext context)
    {
        var glyph = ArticulationCalculator.GetDecorationGlyph(decoration);
        if (glyph.Path == null)
        {
            return null;
        }

        var transform = CreateTranslate(bounds.X, bounds.Y);
        return RenderGlyph(glyph, bounds, transform, context);
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

    protected static string CreateTranslate(double x, double y)
    {
        return $"translate({x:F2},{y:F2})";
    }

    /// <summary>
    /// Renders a sequence of digit glyphs for time signatures.
    /// </summary>
    protected static void RenderDigits(XElement parent, string digits, double x, double y, SvgContext context)
    {
        double currentX = x;
        var digitWidth = 0.8 * context.StaffSpace;

        foreach (var glyph in digits.Select(GetDigitGlyph))
        {
            if (glyph is not null)
            {
                var transform = CreateTranslate(currentX, y);
                var glyphElement = RenderGlyph(glyph.Value, 1.0, transform, context);
                if (glyphElement != null)
                {
                    parent.Add(glyphElement);
                }
                currentX += digitWidth;
            }
        }
    }

    /// <summary>
    /// Renders augmentation dots for a symbol.
    /// </summary>
    protected static void RenderDots(XElement group, AugmentationDottedLayoutSymbol symbol, SvgContext context)
    {
        if (symbol.DotCount <= 0)
        {
            return;
        }

        var dotY = symbol.DotY;

        for (int i = 0; i < symbol.DotCount; i++)
        {
            var dotX = symbol.DotXPositions[i] - symbol.Bounds.X; // Relative to symbol position
            var dotYRelative = dotY - symbol.Bounds.Y;      // Relative to symbol position

            var dotElement = RenderGlyph(
                MusicGlyphs.AugmentationDot,
                0.5,
                CreateTranslate(dotX, dotYRelative),
                context);

            if (dotElement != null)
            {
                group.Add(AddId(symbol.Id, dotElement));
            }
        }
    }
}
