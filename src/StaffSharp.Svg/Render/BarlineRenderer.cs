namespace StaffSharp.Svg;

using System.Xml.Linq;

using StaffSharp.Svg.Layout;

internal sealed class BarlineRenderer : LayoutElementRenderer<BarlineLayoutSymbol>
{
    public static BarlineRenderer Instance { get; } = new();

    public override XElement Render(BarlineLayoutSymbol symbol, SvgContext context)
    {
        return CreateLine(symbol.X, symbol.Y, symbol.X, symbol.Y + symbol.Height, strokeWidth: 2);
    }
}
