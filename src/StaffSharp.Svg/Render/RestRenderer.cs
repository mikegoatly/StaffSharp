namespace StaffSharp.Svg;

using System.Globalization;
using System.Xml.Linq;

using StaffSharp.Svg.Layout;

internal sealed class RestRenderer : LayoutElementRenderer<RestLayoutSymbol>
{
    public static RestRenderer Instance { get; } = new();
    public override XElement Render(RestLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(
            SvgNamespace + "g",
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
}
