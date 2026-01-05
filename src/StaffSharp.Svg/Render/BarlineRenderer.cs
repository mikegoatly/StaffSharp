namespace StaffSharp.Render;

using System.Globalization;
using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

internal sealed class BarlineRenderer : LayoutElementRenderer<BarlineLayoutSymbol>
{
    public static BarlineRenderer Instance { get; } = new();

    public override XElement Render(BarlineLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g");

        switch (symbol.BarlineType)
        {
            case BarlineType.Normal:
                RenderNormalBarline(group, symbol);
                break;

            case BarlineType.DoubleBar:
                RenderDoubleBarline(group, symbol, context);
                break;

            case BarlineType.Final:
                RenderFinalBarline(group, symbol, context);
                break;

            case BarlineType.RepeatStart:
                RenderRepeatStartBarline(group, symbol, context);
                break;

            case BarlineType.RepeatEnd:
                RenderRepeatEndBarline(group, symbol, context);
                break;

            case BarlineType.RepeatBoth:
                RenderRepeatBothBarline(group, symbol, context);
                break;
        }

        return group;
    }

    private static void RenderNormalBarline(XElement group, BarlineLayoutSymbol symbol)
    {
        var line = CreateLine(symbol.X, symbol.Y, symbol.X, symbol.Y + symbol.Height, strokeWidth: 2);
        group.Add(line);
    }

    private static void RenderDoubleBarline(XElement group, BarlineLayoutSymbol symbol, SvgContext context)
    {
        var spacing = context.StaffSpace * 0.15;
        
        // Two thin lines
        var line1 = CreateLine(symbol.X - spacing, symbol.Y, symbol.X - spacing, symbol.Y + symbol.Height, strokeWidth: 2);
        var line2 = CreateLine(symbol.X + spacing, symbol.Y, symbol.X + spacing, symbol.Y + symbol.Height, strokeWidth: 2);
        
        group.Add(line1);
        group.Add(line2);
    }

    private static void RenderFinalBarline(XElement group, BarlineLayoutSymbol symbol, SvgContext context)
    {
        var spacing = context.StaffSpace * 0.15;
        
        // Thin line on left, thick line on right
        var thinLine = CreateLine(symbol.X - spacing, symbol.Y, symbol.X - spacing, symbol.Y + symbol.Height, strokeWidth: 2);
        var thickLine = CreateLine(symbol.X + spacing, symbol.Y, symbol.X + spacing, symbol.Y + symbol.Height, strokeWidth: 6);
        
        group.Add(thinLine);
        group.Add(thickLine);
    }

    private static void RenderRepeatStartBarline(XElement group, BarlineLayoutSymbol symbol, SvgContext context)
    {
        var spacing = context.StaffSpace * 0.15;
        var dotOffset = context.StaffSpace * 0.4;
        
        // Thick line on left, thin line on right, dots on right
        var thickLine = CreateLine(symbol.X - spacing, symbol.Y, symbol.X - spacing, symbol.Y + symbol.Height, strokeWidth: 6);
        var thinLine = CreateLine(symbol.X + spacing, symbol.Y, symbol.X + spacing, symbol.Y + symbol.Height, strokeWidth: 2);
        
        group.Add(thickLine);
        group.Add(thinLine);
        
        // Add repeat dots (in spaces 2 and 4, counting from bottom)
        AddRepeatDots(group, symbol.X + spacing + dotOffset, symbol.Y, symbol.Height, context);
    }

    private static void RenderRepeatEndBarline(XElement group, BarlineLayoutSymbol symbol, SvgContext context)
    {
        var spacing = context.StaffSpace * 0.15;
        var dotOffset = context.StaffSpace * 0.4;
        
        // Dots on left, thin line on left, thick line on right
        var thinLine = CreateLine(symbol.X - spacing, symbol.Y, symbol.X - spacing, symbol.Y + symbol.Height, strokeWidth: 2);
        var thickLine = CreateLine(symbol.X + spacing, symbol.Y, symbol.X + spacing, symbol.Y + symbol.Height, strokeWidth: 6);
        
        group.Add(thinLine);
        group.Add(thickLine);
        
        // Add repeat dots (in spaces 2 and 4, counting from bottom)
        AddRepeatDots(group, symbol.X - spacing - dotOffset, symbol.Y, symbol.Height, context);
    }

    private static void RenderRepeatBothBarline(XElement group, BarlineLayoutSymbol symbol, SvgContext context)
    {
        var spacing = context.StaffSpace * 0.15;
        var dotOffset = context.StaffSpace * 0.4;
        
        // Dots, thin line, thick line, thin line, dots
        var thinLine1 = CreateLine(symbol.X - spacing * 3, symbol.Y, symbol.X - spacing * 3, symbol.Y + symbol.Height, strokeWidth: 2);
        var thickLine = CreateLine(symbol.X, symbol.Y, symbol.X, symbol.Y + symbol.Height, strokeWidth: 6);
        var thinLine2 = CreateLine(symbol.X + spacing * 3, symbol.Y, symbol.X + spacing * 3, symbol.Y + symbol.Height, strokeWidth: 2);
        
        group.Add(thinLine1);
        group.Add(thickLine);
        group.Add(thinLine2);
        
        // Add repeat dots on both sides
        AddRepeatDots(group, symbol.X - spacing * 3 - dotOffset, symbol.Y, symbol.Height, context);
        AddRepeatDots(group, symbol.X + spacing * 3 + dotOffset, symbol.Y, symbol.Height, context);
    }

    private static void AddRepeatDots(XElement group, double x, double y, double height, SvgContext context)
    {
        // Calculate staff space from height (height = 4 * staffSpace for 5-line staff)
        var staffSpace = context.StaffSpace;
        var radius = staffSpace * 0.5 * 0.25;

        // Position dots in spaces 2 and 4 (counting from bottom)
        // Space 2 is at 1.5 staff spaces from bottom
        // Space 4 is at 2.5 staff spaces from bottom
        group.Add(CreateCircle(x, y + height - (1.5 * staffSpace), radius));
        group.Add(CreateCircle(x, y + height - (2.5 * staffSpace), radius));
    }

    private static XElement CreateCircle(double x, double y, double radius)
    {
        return new XElement(SvgNamespace + "circle",
            new XAttribute("cx", x.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("cy", y.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("r", radius.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("fill", "black"));
    }
}
