namespace StaffSharp.Svg;

using System.Globalization;
using System.Xml.Linq;

using StaffSharp.Svg.Layout;

internal class SystemLayoutRenderer : LayoutElementRenderer<LayoutSystem>
{
    public static SystemLayoutRenderer Instance { get; } = new();
    public override XElement Render(LayoutSystem system, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "system"),
            new XAttribute("transform", $"translate(0,{system.Y.ToString(CultureInfo.InvariantCulture)})")
        );

        foreach (var staff in system.Staves)
        {
            group.Add(StaffLayoutRenderer.Instance.Render(staff, context));
        }

        return group;
    }
}
