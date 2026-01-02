namespace StaffSharp.Svg;

using System.Globalization;
using System.Xml.Linq;
using StaffSharp.Notation;
using StaffSharp.Svg.Layout;
using StaffSharp.Svg.Render;

/// <summary>
/// Renders a LayoutModel to SVG.
/// </summary>
public static class SvgRenderer
{
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    public static XElement Render(LayoutModel layoutModel, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(layoutModel);
        ArgumentNullException.ThrowIfNull(context);

        // Calculate viewBox from actual content bounds with margins
        var contentWidth = layoutModel.TotalWidth > 0 ? layoutModel.TotalWidth : context.MaxWidth;
        var contentHeight = layoutModel.TotalHeight > 0 ? layoutModel.TotalHeight : 400;
        var viewBoxWidth = contentWidth + context.Margins.Right;
        var viewBoxHeight = contentHeight + context.Margins.Bottom;

        var svg = new XElement(SvgNamespace + "svg",
            new XAttribute("viewBox", $"0 0 {viewBoxWidth} {viewBoxHeight}"),
            new XAttribute("width", viewBoxWidth),
            new XAttribute("height", viewBoxHeight)
        );

        // Render each system
        foreach (var system in layoutModel.Systems)
        {
            svg.Add(RenderSystem(system, context));
        }

        return svg;
    }

    private static XElement RenderSystem(LayoutSystem system, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "system"),
            new XAttribute("transform", $"translate(0,{system.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        foreach (var staff in system.Staves)
        {
            group.Add(RenderStaff(staff, context));
        }

        return group;
    }

    private static XElement RenderStaff(LayoutStaff staff, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "staff"),
            new XAttribute("transform", $"translate(0,{staff.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        // Draw 5 staff lines
        for (int i = 0; i < 5; i++)
        {
            var y = i * context.StaffSpace;
            var x1 = staff.X;
            var x2 = staff.X + staff.Width;
            group.Add(new XElement(SvgNamespace + "line",
                new XAttribute("x1", x1.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("y1", y.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("x2", x2.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("y2", y.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("stroke", "black"),
                new XAttribute("stroke-width", "1")
            ));
        }

        // Render symbols
        foreach (var measure in staff.Measures)
        {
            foreach (var symbol in measure.Symbols)
            {
                var rendered = RenderSymbol(symbol, context);
                if (rendered != null)
                {
                    group.Add(rendered);
                }
            }

            // Render beams for this measure (after symbols so they appear on top)
            RenderBeams(group, measure, context);

            // Render ties and slurs
            RenderCurves(group, measure, context);
        }

        return group;
    }

    private static void RenderCurves(XElement group, LayoutMeasure measure, SvgContext context)
    {
        foreach (var curve in measure.Curves)
        {
            // Create a filled tie shape with tapered ends
            // Thickness in the middle, tapered to points at the ends
            var thickness = 0.15 * context.StaffSpace; // Tie thickness at the thickest point
            var direction = curve.CurveAbove ? -1 : 1;

            // Create the tie as two Bézier curves forming a closed shape
            // Top curve (from start to end)
            var topPath = $"M {curve.StartX.ToString(CultureInfo.InvariantCulture)} {curve.StartY.ToString(CultureInfo.InvariantCulture)} " +
                          $"C {curve.ControlX1.ToString(CultureInfo.InvariantCulture)} {curve.ControlY1.ToString(CultureInfo.InvariantCulture)}, " +
                          $"{curve.ControlX2.ToString(CultureInfo.InvariantCulture)} {curve.ControlY2.ToString(CultureInfo.InvariantCulture)}, " +
                          $"{curve.EndX.ToString(CultureInfo.InvariantCulture)} {curve.EndY.ToString(CultureInfo.InvariantCulture)}";

            // Bottom curve (from end back to start, with thickness offset)
            var bottomStartY = curve.EndY + direction * thickness;
            var bottomEndY = curve.StartY + direction * thickness;
            var bottomControl1Y = curve.ControlY2 + direction * thickness;
            var bottomControl2Y = curve.ControlY1 + direction * thickness;

            var bottomPath = $" L {curve.EndX.ToString(CultureInfo.InvariantCulture)} {bottomStartY.ToString(CultureInfo.InvariantCulture)} " +
                            $"C {curve.ControlX2.ToString(CultureInfo.InvariantCulture)} {bottomControl1Y.ToString(CultureInfo.InvariantCulture)}, " +
                            $"{curve.ControlX1.ToString(CultureInfo.InvariantCulture)} {bottomControl2Y.ToString(CultureInfo.InvariantCulture)}, " +
                            $"{curve.StartX.ToString(CultureInfo.InvariantCulture)} {bottomEndY.ToString(CultureInfo.InvariantCulture)} Z";

            group.Add(new XElement(SvgNamespace + "path",
                new XAttribute("d", topPath + bottomPath),
                new XAttribute("fill", "black"),
                new XAttribute("class", curve.IsTie ? "tie" : "slur")
            ));
        }
    }

    private static void RenderBeams(XElement group, LayoutMeasure measure, SvgContext context)
    {
        // Group symbols by beam group ID
        var beamGroups = measure.Symbols
            .Where(s => s.BeamGroupId.HasValue)
            .GroupBy(s => s.BeamGroupId!.Value)
            .ToList();

        foreach (var beamGroup in beamGroups)
        {
            var symbols = beamGroup.OrderBy(s => s.TimePosition).ToList();
            if (symbols.Count < 2) continue;

            // Calculate beam Y position (horizontal beam for now)
            // Use the average of stem ends
            var stemEndYs = symbols.Select(s => s.StemY2).ToList();
            var beamY = stemEndYs.Average();
            var stemUp = symbols[0].StemUp;

            // Get beam count for the group (use minimum to handle mixed durations)
            var beamCount = symbols.Min(s => s.BeamCount);
            var beamThickness = 0.5 * context.StaffSpace;
            var beamGap = 0.25 * context.StaffSpace;

            for (int beamIndex = 0; beamIndex < beamCount; beamIndex++)
            {
                var firstSymbol = symbols.First();
                var lastSymbol = symbols.Last();

                var x1 = firstSymbol.X;
                var x2 = lastSymbol.X;
                var yOffset = beamIndex * (beamThickness + beamGap);
                var y = stemUp ? beamY + yOffset : beamY - yOffset - beamThickness;

                group.Add(new XElement(SvgNamespace + "rect",
                    new XAttribute("x", x1.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("y", y.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("width", (x2 - x1).ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("height", beamThickness.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("fill", "black")
                ));
            }

            // Handle partial beams for notes with more beams than the group minimum
            for (int i = 0; i < symbols.Count; i++)
            {
                var symbol = symbols[i];
                if (symbol.BeamCount > beamCount)
                {
                    // This note needs additional partial beams
                    for (int beamIndex = beamCount; beamIndex < symbol.BeamCount; beamIndex++)
                    {
                        var yOffset = beamIndex * (beamThickness + beamGap);
                        var y = stemUp ? beamY + yOffset : beamY - yOffset - beamThickness;

                        // Partial beam extends toward the center of the group
                        var partialLength = 0.75 * context.StaffSpace;
                        double x1, x2;

                        if (i == 0)
                        {
                            // First note: beam extends right
                            x1 = symbol.X;
                            x2 = symbol.X + partialLength;
                        }
                        else if (i == symbols.Count - 1)
                        {
                            // Last note: beam extends left
                            x1 = symbol.X - partialLength;
                            x2 = symbol.X;
                        }
                        else
                        {
                            // Middle note: beam extends left (by convention)
                            x1 = symbol.X - partialLength;
                            x2 = symbol.X;
                        }

                        group.Add(new XElement(SvgNamespace + "rect",
                            new XAttribute("x", x1.ToString(CultureInfo.InvariantCulture)),
                            new XAttribute("y", y.ToString(CultureInfo.InvariantCulture)),
                            new XAttribute("width", (x2 - x1).ToString(CultureInfo.InvariantCulture)),
                            new XAttribute("height", beamThickness.ToString(CultureInfo.InvariantCulture)),
                            new XAttribute("fill", "black")
                        ));
                    }
                }
            }
        }
    }

    private static XElement? RenderSymbol(LayoutSymbol symbol, SvgContext context)
    {
        return symbol switch
        {
            NoteLayoutSymbol noteSymbol => RenderNote(noteSymbol, context),
            RestLayoutSymbol restSymbol => RenderRest(restSymbol, context),
            ChordLayoutSymbol chordSymbol => RenderChord(chordSymbol, context),
            ClefLayoutSymbol clefSymbol => RenderClef(clefSymbol, context),
            KeySignatureLayoutSymbol keySymbol => RenderKeySignature(keySymbol, context),
            TimeSignatureLayoutSymbol timeSymbol => RenderTimeSignature(timeSymbol, context),
            BarlineLayoutSymbol barlineSymbol => RenderBarline(barlineSymbol, context),
            _ => null
        };
    }

    private static XElement RenderNote(NoteLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "note"),
            new XAttribute("transform", $"translate({symbol.X.ToString(CultureInfo.InvariantCulture)},{symbol.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        // Render accidental if present
        if (symbol.Accidental.HasValue)
        {
            RenderAccidental(group, symbol.Accidental.Value, symbol.AccidentalX, symbol.AccidentalY - symbol.Y, context);
        }

        // Choose notehead based on duration and render it
        var notehead = GetNoteheadGlyph(symbol.Note.Duration);
        var noteheadElement = RenderGlyph(notehead, 1.0, null, context);
        if (noteheadElement != null)
        {
            group.Add(noteheadElement);
        }

        // Render stem if present
        if (symbol.StemY2 != 0)
        {
            RenderStem(group, symbol, context);
        }

        // Render ledger lines if needed
        if (symbol.LedgerLineCount > 0)
        {
            RenderLedgerLines(group, symbol, context);
        }

        return group;
    }

    private static XElement RenderRest(RestLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "rest"),
            new XAttribute("transform", $"translate({symbol.X.ToString(CultureInfo.InvariantCulture)},{symbol.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        var restGlyph = GetRestGlyph(symbol.Rest.Duration);
        var restElement = RenderGlyph(restGlyph, 1.0, null, context);
        if (restElement != null)
        {
            group.Add(restElement);
        }

        return group;
    }

    private static XElement RenderChord(ChordLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "chord"),
            new XAttribute("transform", $"translate({symbol.X.ToString(CultureInfo.InvariantCulture)},{symbol.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        // Render accidentals if present
        for (int i = 0; i < symbol.Accidentals.Count; i++)
        {
            var accidental = symbol.Accidentals[i];
            var xOffset = symbol.AccidentalXOffsets[i];
            var yPos = symbol.AccidentalYPositions[i] - symbol.Y;  // Make relative to chord Y
            RenderAccidental(group, accidental, xOffset, yPos, context);
        }

        var notehead = GetNoteheadGlyph(symbol.Chord.Duration);

        // Render each notehead at its calculated Y position with optional X shift
        // Y positions are absolute, so make them relative to symbol.Y
        for (int i = 0; i < symbol.NoteheadYPositions.Count; i++)
        {
            var y = symbol.NoteheadYPositions[i] - symbol.Y;
            var xShift = symbol.NoteheadXShifts.Count > i ? symbol.NoteheadXShifts[i] : 0;
            var transform = $"translate({xShift.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)})";

            var noteheadElement = RenderGlyph(notehead, 1.0, transform, context);
            if (noteheadElement != null)
            {
                group.Add(noteheadElement);
            }
        }

        // Render stem if present
        if (symbol.StemY2 != 0)
        {
            RenderStem(group, symbol, context);
        }

        // Render ledger lines if needed
        if (symbol.LedgerLineCount > 0)
        {
            RenderLedgerLines(group, symbol, context);
        }

        return group;
    }

    private static XElement RenderClef(ClefLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "clef"),
            new XAttribute("transform", $"translate({symbol.X.ToString(CultureInfo.InvariantCulture)},{symbol.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        var clefGlyph = symbol.Clef switch
        {
            Clef.Treble => MusicGlyphs.TrebleClef,
            Clef.Bass => MusicGlyphs.BassClef,
            Clef.Alto => MusicGlyphs.AltoClef,
            Clef.Tenor => MusicGlyphs.TenorClef,
            _ => MusicGlyphs.TrebleClef
        };

        // Clefs are larger than a single staff-space; scale so clef height maps to ~4 staff-spaces
        var clefElement = RenderGlyph(clefGlyph, 4.0, null, context);
        if (clefElement != null)
        {
            group.Add(clefElement);
        }

        return group;
    }

    private static XElement? RenderKeySignature(KeySignatureLayoutSymbol symbol, SvgContext context)
    {
        if (symbol.KeySignature.Sharps == 0)
        {
            return null; // C major, no accidentals
        }

        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "key-signature"),
            new XAttribute("transform", $"translate({symbol.X.ToString(CultureInfo.InvariantCulture)},{symbol.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        // Get accidental positions from service (handles all clef types)
        var positions = Layout.Services.KeySignatureService.GetAccidentalPositions(
            symbol.KeySignature,
            symbol.Clef,
            context.StaffSpace);

        var xSpacing = Layout.Services.KeySignatureService.AccidentalSpacing * context.StaffSpace;
        var currentX = 0.0;

        foreach (var (accidental, yPosition) in positions)
        {
            var glyph = accidental switch
            {
                Accidental.Sharp => MusicGlyphs.Sharp,
                Accidental.Flat => MusicGlyphs.Flat,
                _ => default(GlyphInfo?)
            };

            if (glyph != null)
            {
                var transform = $"translate({currentX.ToString(CultureInfo.InvariantCulture)},{yPosition.ToString(CultureInfo.InvariantCulture)})";
                var glyphElement = RenderGlyph(glyph.Value, 1.0, transform, context);
                if (glyphElement != null)
                {
                    group.Add(glyphElement);
                }
                currentX += xSpacing;
            }
        }

        return group;
    }

    private static XElement RenderTimeSignature(TimeSignatureLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "time-signature"),
            new XAttribute("transform", $"translate({symbol.X.ToString(CultureInfo.InvariantCulture)},{symbol.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        // Handle special time signatures with dedicated glyphs
        if (symbol.TimeSignature == TimeSignature.CommonTime)
        {
            var glyph = RenderGlyph(MusicGlyphs.CommonTime, 1.0, $"translate(0,{(2 * context.StaffSpace).ToString(CultureInfo.InvariantCulture)})", context);
            if (glyph != null)
            {
                group.Add(glyph);
            }
        }
        else if (symbol.TimeSignature.Numerator == 2 && symbol.TimeSignature.Denominator == 2)
        {
            // Cut time (2/2)
            var glyph = RenderGlyph(MusicGlyphs.CutTime, 1.0, $"translate(0,{(2 * context.StaffSpace).ToString(CultureInfo.InvariantCulture)})", context);
            if (glyph != null)
            {
                group.Add(glyph);
            }
        }
        else
        {
            // Render numerator and denominator as digit glyphs
            var numerator = symbol.TimeSignature.Numerator;
            var denominator = symbol.TimeSignature.Denominator;

            RenderDigits(group, numerator.ToString(CultureInfo.InvariantCulture), 0, context.StaffSpace, context);
            RenderDigits(group, denominator.ToString(CultureInfo.InvariantCulture), 0, 3 * context.StaffSpace, context);
        }

        return group;
    }

    private static void RenderDigits(XElement parent, string digits, double x, double y, SvgContext context)
    {
        double currentX = x;
        var digitWidth = 0.8 * context.StaffSpace; // Approximate width for digit spacing

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

    private static GlyphInfo? GetDigitGlyph(char digit)
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

    private static XElement RenderBarline(BarlineLayoutSymbol symbol, SvgContext context)
    {
        return new XElement(SvgNamespace + "line",
            new XAttribute("x1", symbol.X.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("y1", symbol.Y.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("x2", symbol.X.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("y2", (symbol.Y + symbol.Height).ToString(CultureInfo.InvariantCulture)),
            new XAttribute("stroke", "black"),
            new XAttribute("stroke-width", "2")
        );
    }

    private static void RenderLedgerLines(XElement group, LayoutSymbol symbol, SvgContext context)
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
    /// Renders a glyph as an SVG path element with appropriate scaling.
    /// </summary>
    /// <param name="glyph">The glyph to render.</param>
    /// <param name="targetHeightInStaffSpaces">Target height in staff space units (default: 1.0).</param>
    /// <param name="transform">Optional additional transform string to prepend.</param>
    /// <param name="context">The SVG rendering context.</param>
    /// <returns>An SVG path element, or null if the glyph has no path.</returns>
    private static XElement? RenderGlyph(GlyphInfo glyph, double targetHeightInStaffSpaces, string? transform, SvgContext context)
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

    private static GlyphInfo GetNoteheadGlyph(SymbolicDuration duration)
    {
        return duration.Base switch
        {
            NoteDurationBase.Whole => MusicGlyphs.NoteHeadWhole,
            NoteDurationBase.Half => MusicGlyphs.NoteHeadHalf,
            _ => MusicGlyphs.NoteHeadBlack
        };
    }

    private static GlyphInfo GetRestGlyph(SymbolicDuration duration)
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

    private static void RenderStem(XElement group, LayoutSymbol symbol, SvgContext context)
    {
        // Calculate stem X offset based on direction
        var stemX = symbol.StemUp
            ? context.StaffSpace + 1  // Right edge for stem up
            : 1; // Left edge for stem down

        group.Add(new XElement(SvgNamespace + "line",
            new XAttribute("x1", stemX.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("y1", (symbol.StemY1 - symbol.Y).ToString(CultureInfo.InvariantCulture)),
            new XAttribute("x2", stemX.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("y2", (symbol.StemY2 - symbol.Y).ToString(CultureInfo.InvariantCulture)),
            new XAttribute("stroke", "black"),
            new XAttribute("stroke-width", "1.5")
        ));
    }

    private static void RenderAccidental(XElement group, Accidental accidental, double x, double y, SvgContext context)
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
        if (accidentalElement != null)
        {
            group.Add(accidentalElement);
        }
    }
}
