namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Layout.Services;
using StaffSharp.Notation;

internal sealed class KeySignatureRenderer : LayoutElementRenderer<KeySignatureLayoutSymbol>
{
    public static KeySignatureRenderer Instance { get; } = new();
    public override XElement Render(KeySignatureLayoutSymbol symbol, SvgContext context)
    {
        if (symbol.KeySignature.Sharps == 0)
        {
            return new XElement(SvgNamespace + "g"); // C major, no accidentals
        }

        var group = new XElement(
            SvgNamespace + "g",
            new XAttribute("class", "key-signature"),
            new XAttribute("transform", CreateTranslate(symbol.X, symbol.Y))
        );

        // Get accidental positions from service (handles all clef types)
        var positions = KeySignatureService.GetAccidentalPositions(
            symbol.KeySignature,
            symbol.Clef,
            context.StaffSpace);

        var xSpacing = KeySignatureService.AccidentalSpacing * context.StaffSpace;
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
                var transform = CreateTranslate(currentX, yPosition);
                var glyphElement = RenderGlyph(glyph.Value, 2.0, transform, context);
                if (glyphElement != null)
                {
                    group.Add(glyphElement);
                }
                currentX += xSpacing;
            }
        }

        return group;
    }
}
