namespace StaffSharp.Render;

using System.Globalization;
using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

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
        var accidentalElement = RenderGlyph(accidentalGlyph, 2.0, transform, context);
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
    protected static void RenderStem(XElement group, IStemmedSymbol symbol, SvgContext context)
    {
        // Stem X is calculated in layout pass and stored in symbol.Stem (absolute coordinates)
        // Convert to relative coordinates (relative to the note/chord group's transform)
        var layoutElement = (LayoutElement)symbol;
        var stemX = symbol.Stem.X - layoutElement.X;

        group.Add(
            CreateLine(
                stemX,
                symbol.Stem.Y1 - layoutElement.Y,
                stemX,
                symbol.Stem.Y2 - layoutElement.Y,
                strokeWidth: 1.5));
    }

    /// <summary>
    /// Renders flags for a note or chord symbol.
    /// </summary>
    protected static void RenderFlag(XElement group, IStemmedSymbol symbol, SvgContext context)
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
        var flagX = symbol.Stem.X - layoutElement.X;
        var flagY = symbol.Stem.Y2 - layoutElement.Y;

        var flagElement = new XElement(SvgNamespace + "path",
            new XAttribute("d", flagPath),
            new XAttribute("fill", "black"),
            new XAttribute("transform", $"translate({flagX.ToString(System.Globalization.CultureInfo.InvariantCulture)},{flagY.ToString(System.Globalization.CultureInfo.InvariantCulture)})")
        );

        group.Add(flagElement);
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
        if (symbol is NoteLayoutSymbol noteSymbol)
        {
            foreach (var (type, x, y) in noteSymbol.PositionedDecorations)
            {
                var decorationElement = RenderDecoration(type, x - symbol.X, y - symbol.Y, context);
                if (decorationElement != null)
                {
                    group.Add(decorationElement);
                }
            }
        }
        else if (symbol is ChordLayoutSymbol chordSymbol)
        {
            foreach (var (type, x, y) in chordSymbol.PositionedDecorations)
            {
                var decorationElement = RenderDecoration(type, x - symbol.X, y - symbol.Y, context);
                if (decorationElement != null)
                {
                    group.Add(decorationElement);
                }
            }
        }
    }

    /// <summary>
    /// Renders a single decoration glyph.
    /// </summary>
    protected static XElement? RenderDecoration(Decoration decoration, double x, double y, SvgContext context)
    {
        var glyph = GetDecorationGlyph(decoration);
        if (glyph.Path == null)
        {
            return null;
        }

        // Get articulation-specific scaling and horizontal offset
        var targetHeight = GetDecorationTargetHeight(decoration);
        var xOffset = GetDecorationXOffset(decoration, glyph, targetHeight, context);

        var transform = $"translate({(x + xOffset).ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)})";
        return RenderGlyph(glyph, targetHeight, transform, context);
    }

    /// <summary>
    /// Gets the target height in staff spaces for a decoration glyph.
    /// </summary>
    protected static double GetDecorationTargetHeight(Decoration decoration)
    {
        return decoration switch
        {
            // Small articulations
            Decoration.Staccato => 0.4,
            
            // Medium articulations
            Decoration.Tenuto => 0.5,
            Decoration.Accent => 0.7,
            Decoration.Marcato => 0.7,
            Decoration.UpBow => 0.6,
            Decoration.DownBow => 0.6,
            
            // Large ornaments
            Decoration.Trill => 0.8,
            Decoration.Turn => 0.8,
            Decoration.UpperMordent => 0.8,
            Decoration.LowerMordent => 0.8,
            Decoration.Mordent => 0.8,
            Decoration.InvertedTurn => 0.8,
            
            // Fermata and breath marks
            Decoration.Fermata => 1.0,
            Decoration.Breath => 0.6,
            
            // Default for unspecified
            _ => 0.7
        };
    }

    /// <summary>
    /// Gets the horizontal offset to center wide glyphs over the notehead.
    /// </summary>
    protected static double GetDecorationXOffset(Decoration decoration, GlyphInfo glyph, double targetHeight, SvgContext context)
    {
        // Calculate the rendered width of the glyph
        var targetHeightPixels = targetHeight * context.StaffSpace;
        var scale = glyph.Height > 0 ? targetHeightPixels / glyph.Height : 1.0;
        var renderedWidth = glyph.Width * scale;
        
        // For wide glyphs, offset left to center them
        // Noteheads are approximately 1.5 staff spaces wide, centered at x=0
        var noteheadWidth = 1.5 * context.StaffSpace;
        
        return decoration switch
        {
            // Wide ornaments need centering adjustment
            Decoration.Trill => -(renderedWidth - noteheadWidth) / 2,
            Decoration.Fermata => -(renderedWidth - noteheadWidth) / 2,
            Decoration.Turn => -(renderedWidth - noteheadWidth) / 2,
            Decoration.UpperMordent => -(renderedWidth - noteheadWidth) / 2,
            Decoration.LowerMordent => -(renderedWidth - noteheadWidth) / 2,
            Decoration.Mordent => -(renderedWidth - noteheadWidth) / 2,
            Decoration.InvertedTurn => -(renderedWidth - noteheadWidth) / 2,
            
            // Other articulations are narrow enough to not need offset
            _ => 0
        };
    }

    /// <summary>
    /// Maps a Decoration enum to its corresponding SMuFL glyph.
    /// </summary>
    protected static GlyphInfo GetDecorationGlyph(Decoration decoration)
    {
        return decoration switch
        {
            Decoration.Staccato => MusicGlyphs.Staccato,
            Decoration.Tenuto => MusicGlyphs.Tenuto,
            Decoration.Accent => MusicGlyphs.Accent,
            Decoration.Marcato => MusicGlyphs.Marcato,
            Decoration.Fermata => MusicGlyphs.Hold,
            Decoration.Breath => MusicGlyphs.BreathMark,
            Decoration.Trill => MusicGlyphs.Trill,
            Decoration.Turn => MusicGlyphs.Turn,
            Decoration.UpperMordent => MusicGlyphs.MordentUpper,
            Decoration.LowerMordent => MusicGlyphs.MordentLower,
            Decoration.UpBow => MusicGlyphs.Upbow,
            // Note: Some decorations don't have glyphs yet or aren't rendered as symbols
            _ => default
        };
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

    /// <summary>
    /// Renders augmentation dots for a symbol.
    /// </summary>
    protected static void RenderDots(XElement group, AugmentationDottedLayoutSymbol symbol, SvgContext context)
    {
        if (symbol.DotCount <= 0) return;

        var dotY = symbol.DotY;

        for (int i = 0; i < symbol.DotCount; i++)
        {
            var dotX = symbol.DotXPositions[i] - symbol.X; // Relative to symbol position
            var dotYRelative = dotY - symbol.Y;      // Relative to symbol position

            var dotElement = RenderGlyph(
                MusicGlyphs.AugmentationDot,
                0.5,
                $"translate({dotX.ToString(CultureInfo.InvariantCulture)},{dotYRelative.ToString(CultureInfo.InvariantCulture)})",
                context);

            if (dotElement != null)
            {
                group.Add(dotElement);
            }
        }
    }
}
