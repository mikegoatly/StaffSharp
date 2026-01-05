namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

internal sealed class ChordRenderer : LayoutElementRenderer<ChordLayoutSymbol>
{
    public static ChordRenderer Instance { get; } = new();
    public override XElement Render(ChordLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(
            SvgNamespace + "g",
            new XAttribute("class", "chord"),
            new XAttribute("transform", CreateTranslate(symbol.X, symbol.Y))
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
            var transform = CreateTranslate(xShift, y);

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

        // Render decorations/articulations
        RenderDecorations(group, symbol, context);

        return group;
    }
}
