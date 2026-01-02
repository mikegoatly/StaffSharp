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
        var group = new XElement(
            SvgNamespace + "g",
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
}
