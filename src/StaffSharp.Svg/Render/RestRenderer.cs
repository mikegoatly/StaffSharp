namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;
using StaffSharp.Notation;

internal sealed class RestRenderer : LayoutElementRenderer<RestLayoutSymbol>
{
    public static RestRenderer Instance { get; } = new();
    public override XElement Render(RestLayoutSymbol symbol, SvgContext context)
    {
        var group = new XElement(
            SvgNamespace + "g",
            new XAttribute("class", "rest"),
            new XAttribute("transform", CreateTranslate(symbol.X, symbol.Y))
        );

        var restGlyph = GetRestGlyph(symbol.Rest.Duration);
        var targetHeight = GetRestTargetHeight(symbol.Rest.Duration);
        var restElement = RenderGlyph(restGlyph, targetHeight, null, context);
        if (restElement != null)
        {
            group.Add(restElement);
        }

        // Render augmentation dots if present
        RenderDots(group, symbol, context);

        return group;
    }

    /// <summary>
    /// Gets the target height in staff spaces for a rest based on its duration.
    /// Different rest types have different visual heights in standard music notation.
    /// </summary>
    private static double GetRestTargetHeight(SymbolicDuration duration)
    {
        return duration.Base switch
        {
            NoteDurationBase.Whole => 0.5,      // Small block
            NoteDurationBase.Half => 0.5,       // Small block
            NoteDurationBase.Quarter => 2.5,    // Tall flowing shape
            NoteDurationBase.Eighth => 2.0,     // Medium height
            NoteDurationBase.Sixteenth => 3.0,  // Taller shape
            _ => 3.5                             // Default to quarter rest size
        };
    }
}
