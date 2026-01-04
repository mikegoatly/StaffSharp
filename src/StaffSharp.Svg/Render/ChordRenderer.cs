namespace StaffSharp.Svg;

using System.Globalization;
using System.Xml.Linq;

using StaffSharp.Notation;
using StaffSharp.Svg.Layout;

internal sealed class ChordRenderer : LayoutElementRenderer<ChordLayoutSymbol>
{
    public static ChordRenderer Instance { get; } = new();
    public override XElement Render(ChordLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(
            SvgNamespace + "g",
            new XAttribute("class", "chord"),
            new XAttribute("transform", $"translate({symbol.X.ToString(CultureInfo.InvariantCulture)},{symbol.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        // Render accidentals if present
        for (int i = 0; i < symbol.Accidentals.Count; i++)
        {
            var accidental = symbol.Accidentals[i];
            var xOffset = symbol.AccidentalXOffsets[i];
            var yPos = symbol.AccidentalYPositions[i] - symbol.Y;  // Make relative to chord Y
            group.Add(RenderAccidental(accidental, xOffset, yPos, context));
        }

        var notehead = GetNoteheadGlyph(symbol.Chord.Duration);

        // Render each notehead at its calculated Y position with optional X shift
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

        // Render stem if required (whole notes don't have stems)
        if (symbol.Chord.Duration.Base != NoteDurationBase.Whole)
        {
            RenderStem(group, symbol, context);
            RenderFlag(group, symbol, context);
        }

        // Render ledger lines if needed
        if (symbol.LedgerLineCount > 0)
        {
            RenderLedgerLines(group, symbol, context);
        }

        // Render augmentation dots if present
        RenderDots(group, symbol, context);

        return group;
    }
}
