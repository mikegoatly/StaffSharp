namespace StaffSharp.Svg;

using System.Globalization;
using System.Xml.Linq;

using StaffSharp.Notation;
using StaffSharp.Svg.Layout;

internal sealed class NoteRenderer : LayoutElementRenderer<NoteLayoutSymbol>
{
    public static NoteRenderer Instance { get; } = new();
    public override XElement Render(NoteLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(
            SvgNamespace + "g",
            new XAttribute("class", "note"),
            new XAttribute("transform", $"translate({symbol.X.ToString(CultureInfo.InvariantCulture)},{symbol.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        // Render accidental if present
        if (symbol.Accidental.HasValue)
        {
            group.Add(RenderAccidental(symbol.Accidental.Value, symbol.AccidentalX, symbol.AccidentalY - symbol.Y, context));
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
            RenderStem(group, symbol, context);
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
