namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

internal sealed class ClefRenderer : LayoutElementRenderer<ClefLayoutSymbol>
{
    public static ClefRenderer Instance { get; } = new();
    public override XElement Render(ClefLayoutSymbol symbol, SvgContext context)
    {
        // HACK: Pad the clefs to the left slightly
        var x = symbol.Bounds.X + 5;

        var group = new XElement(
            SvgNamespace + "g",
            new XAttribute("class", "clef"),
            new XAttribute("transform", CreateTranslate(x, symbol.Bounds.Y))
        );

        var (clefGlyph, scale) = symbol.Clef switch
        {
            Clef.Treble => (MusicGlyphs.TrebleClef, 4.0),
            Clef.Bass => (MusicGlyphs.BassClef, 2.0),
            _ => (MusicGlyphs.TrebleClef, 4.0)
        };

        // Clefs are larger than a single staff-space; scale so clef height maps to ~4 staff-spaces
        var clefElement = RenderGlyph(clefGlyph, scale, null, context);
        if (clefElement != null)
        {
            group.Add(clefElement);
        }

        return group;
    }
}
