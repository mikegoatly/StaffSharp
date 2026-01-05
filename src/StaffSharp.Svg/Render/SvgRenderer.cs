namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout;

/// <summary>
/// Renders a LayoutModel to SVG.
/// </summary>
public static class SvgRenderer
{
    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";

    public static XElement Render(LayoutModel layoutModel, SvgContext context)
    {
        ArgumentNullException.ThrowIfNull(layoutModel);
        ArgumentNullException.ThrowIfNull(context);

        // Calculate viewBox from actual content bounds with margins
        var contentWidth = layoutModel.TotalWidth > 0 ? layoutModel.TotalWidth : context.MaxWidth;
        var contentHeight = layoutModel.TotalHeight > 0 ? layoutModel.TotalHeight : 400;
        var viewBoxWidth = contentWidth + context.Margins.Right;
        var viewBoxHeight = contentHeight + context.Margins.Bottom;

        var svg = new XElement(SvgNamespace + "svg",
            new XAttribute("viewBox", $"0 0 {viewBoxWidth} {viewBoxHeight}"),
            new XAttribute("width", viewBoxWidth),
            new XAttribute("height", viewBoxHeight)
        );

        // Render each system
        foreach (var system in layoutModel.Systems)
        {
            svg.Add(SystemLayoutRenderer.Instance.Render(system, context));
        }

        return svg;
    }
}
