namespace StaffSharp.Svg;

using System.Globalization;
using System.Xml.Linq;
using StaffSharp.Notation;
using StaffSharp.Svg.Layout;
using StaffSharp.Svg.Render;

internal sealed class ClefRenderer : LayoutElementRenderer<ClefLayoutSymbol>
{
    public static ClefRenderer Instance { get; } = new();
    public override XElement Render(ClefLayoutSymbol symbol, SvgContext context)
    {
        // HACK: Pad the clefs to the left slightly
        var x = symbol.X + 5;

        var group = new XElement(
            SvgNamespace + "g",
            new XAttribute("class", "clef"),
            // TODO create common CreateTransform helper
            new XAttribute("transform", $"translate({x.ToString(CultureInfo.InvariantCulture)},{symbol.Y.ToString(CultureInfo.InvariantCulture)})")
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
