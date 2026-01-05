namespace StaffSharp.Render;

using System.Globalization;
using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

internal sealed class TimeSignatureRenderer : LayoutElementRenderer<TimeSignatureLayoutSymbol>
{
    public static TimeSignatureRenderer Instance { get; } = new();
    public override XElement Render(TimeSignatureLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(
            SvgNamespace + "g",
            new XAttribute("class", "time-signature"),
            new XAttribute("transform", CreateTranslate(symbol.X, symbol.Y))
        );

        // Handle special time signatures with dedicated glyphs
        if (symbol.TimeSignature == TimeSignature.CommonTime)
        {
            var glyph = RenderGlyph(MusicGlyphs.CommonTime, 1.0,
               CreateTranslate(0, 2.0 * context.StaffSpace), context);
            if (glyph != null)
            {
                group.Add(glyph);
            }
        }
        else if (symbol.TimeSignature.Numerator == 2 && symbol.TimeSignature.Denominator == 2)
        {
            // Cut time (2/2)
            var glyph = RenderGlyph(MusicGlyphs.CutTime, 1.0,
                CreateTranslate(0, 2.0 * context.StaffSpace), context);
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
}
