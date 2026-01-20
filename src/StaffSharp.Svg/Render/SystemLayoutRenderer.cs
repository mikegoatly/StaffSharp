namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout.Model;

internal class SystemLayoutRenderer : LayoutElementRenderer<LayoutSystem>
{
    public static SystemLayoutRenderer Instance { get; } = new();
    public override XElement Render(LayoutSystem system, SvgContext context)
    {
        var group = new XElement(SvgNamespace + "g",
            new XAttribute("class", "system"),
            new XAttribute("transform", CreateTranslate(0, system.Bounds.Y))
        );

        foreach (var staff in system.Staves)
        {
            group.Add(StaffLayoutRenderer.Instance.Render(staff, context));
        }

        return group;
    }
}
