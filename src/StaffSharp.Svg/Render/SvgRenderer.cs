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
            group.Add(new XElement(SvgNamespace + "line",
                new XAttribute("x1", context.Margins.Left),
                new XAttribute("y1", y.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("x2", (staff.Width > 0 ? staff.Width : context.MaxWidth).ToString(CultureInfo.InvariantCulture)),
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
            // Create a cubic Bézier curve path
            var path = $"M {curve.StartX.ToString(CultureInfo.InvariantCulture)} {curve.StartY.ToString(CultureInfo.InvariantCulture)} " +
                       $"C {curve.ControlX1.ToString(CultureInfo.InvariantCulture)} {curve.ControlY1.ToString(CultureInfo.InvariantCulture)}, " +
                       $"{curve.ControlX2.ToString(CultureInfo.InvariantCulture)} {curve.ControlY2.ToString(CultureInfo.InvariantCulture)}, " +
                       $"{curve.EndX.ToString(CultureInfo.InvariantCulture)} {curve.EndY.ToString(CultureInfo.InvariantCulture)}";

            group.Add(new XElement(SvgNamespace + "path",
                new XAttribute("d", path),
                new XAttribute("fill", "none"),
                new XAttribute("stroke", "black"),
                new XAttribute("stroke-width", "1.5"),
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
            new XAttribute("transform", $"translate({symbol.X.ToString(CultureInfo.InvariantCulture)},0)")
        );

        // Render accidentals if present
        for (int i = 0; i < symbol.Accidentals.Count; i++)
        {
            var accidental = symbol.Accidentals[i];
            var xOffset = symbol.AccidentalXOffsets[i];
            var yPos = symbol.AccidentalYPositions[i];
            RenderAccidental(group, accidental, xOffset, yPos, context);
        }

        var notehead = GetNoteheadGlyph(symbol.Chord.Duration);

        // Render each notehead at its calculated Y position with optional X shift
        for (int i = 0; i < symbol.NoteheadYPositions.Count; i++)
        {
            var y = symbol.NoteheadYPositions[i];
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

        // Staff position offsets for sharps and flats in treble clef
        // These are measured in half-staff-spaces from the top line (0 = top line)
        // Order of sharps: F C G D A E B
        // Order of flats:  B E A D G C F
        int[] sharpPositions = [0, 3, -1, 2, 5, 1, 4]; // F#, C#, G#, D#, A#, E#, B#
        int[] flatPositions = [4, 1, 5, 2, 6, 3, 7];   // Bb, Eb, Ab, Db, Gb, Cb, Fb

        var glyph = symbol.KeySignature.HasSharps ? MusicGlyphs.Sharp : MusicGlyphs.Flat;
        var positions = symbol.KeySignature.HasSharps ? sharpPositions : flatPositions;
        var count = Math.Abs(symbol.KeySignature.Sharps);

        var xSpacing = 1.2 * context.StaffSpace;
        var currentX = 0.0;

        for (int i = 0; i < count; i++)
        {
            var staffPosition = positions[i];
            var y = staffPosition * 0.5 * context.StaffSpace;

            var transform = $"translate({currentX.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)})";
            var accidentalElement = RenderGlyph(glyph, 1.0, transform, context);
            if (accidentalElement != null)
            {
                group.Add(accidentalElement);
            }

            currentX += xSpacing;
        }

        return group;
    }

    private static XElement RenderTimeSignature(TimeSignatureLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "time-signature"),
            new XAttribute("transform", $"translate({symbol.X.ToString(CultureInfo.InvariantCulture)},{symbol.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        // For MVP: render common time as simple text
        if (symbol.TimeSignature == TimeSignature.CommonTime)
        {
            group.Add(new XElement(SvgNamespace + "text",
                new XAttribute("x", 0),
                new XAttribute("y", (2 * context.StaffSpace).ToString(CultureInfo.InvariantCulture)),
                new XAttribute("font-family", "serif"),
                new XAttribute("font-size", (2 * context.StaffSpace).ToString(CultureInfo.InvariantCulture)),
                new XAttribute("text-anchor", "middle"),
                "C"
            ));
        }
        else
        {
            // Render numerator and denominator as text (simplified)
            group.Add(new XElement(SvgNamespace + "text",
                new XAttribute("x", 0),
                new XAttribute("y", context.StaffSpace.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("font-family", "serif"),
                new XAttribute("font-size", context.StaffSpace.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("text-anchor", "middle"),
                "4" // TODO: extract from TimeSignature
            ));
            group.Add(new XElement(SvgNamespace + "text",
                new XAttribute("x", 0),
                new XAttribute("y", (3 * context.StaffSpace).ToString(CultureInfo.InvariantCulture)),
                new XAttribute("font-family", "serif"),
                new XAttribute("font-size", context.StaffSpace.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("text-anchor", "middle"),
                "4" // TODO: extract from TimeSignature
            ));
        }

        return group;
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
        var lineWidth = context.StaffSpace * 1.5;
        var lineSpacing = context.StaffSpace;

        for (int i = 0; i < symbol.LedgerLineCount; i++)
        {
            var lineY = symbol.LedgerLinesAbove
                ? -(i + 1) * lineSpacing
                : (i + 1) * lineSpacing;

            group.Add(new XElement(SvgNamespace + "line",
                new XAttribute("x1", (-lineWidth / 2).ToString(CultureInfo.InvariantCulture)),
                new XAttribute("y1", lineY.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("x2", (lineWidth / 2).ToString(CultureInfo.InvariantCulture)),
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
        // Render a simple stem line
        // For now, stems are centered on the notehead at X=0 (in the note's coordinate system)
        // TODO: Offset stems to attach to the edge of noteheads (right for stem-up, left for stem-down)
        var stemX = 0.0;

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
            _ => default(GlyphInfo)
        };

        var transform = $"translate({x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)})";
        var accidentalElement = RenderGlyph(accidentalGlyph, 1.0, transform, context);
        if (accidentalElement != null)
        {
            group.Add(accidentalElement);
        }
    }
}
