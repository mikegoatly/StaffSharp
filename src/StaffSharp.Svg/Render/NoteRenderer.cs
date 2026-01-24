namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

internal sealed class NoteRenderer : LayoutElementRenderer<NoteLayoutSymbol>
{
    public static NoteRenderer Instance { get; } = new();
    public override XElement Render(NoteLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(
            SvgNamespace + "g",
            new XAttribute("class", "note"),
            new XAttribute("transform", CreateTranslate(symbol.Bounds.X, symbol.Bounds.Y))
        );

        // Render accidental if present
        if (symbol.Accidental.HasValue)
        {
            group.Add(RenderAccidental(symbol.Accidental.Value, symbol.AccidentalX, symbol.AccidentalY - symbol.Bounds.Y, context));
        }

        // Choose notehead based on duration and render it
        var notehead = GetNoteheadGlyph(symbol.Note.Duration);
        var noteheadElement = RenderGlyph(notehead, 1.0, null, context);
        if (noteheadElement != null)
        {
            group.Add(noteheadElement);
        }

        // Render stem if required (whole notes don't have stems)
        if (symbol.Note.Duration.Base != NoteDurationBase.Whole)
        {
            RenderStem(group, symbol);
            RenderFlag(group, symbol);
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
