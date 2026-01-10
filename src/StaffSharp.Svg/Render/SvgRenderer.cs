namespace StaffSharp.Render;

using System.Xml.Linq;

using StaffSharp;
using StaffSharp.Layout;

/// <summary>
/// Renders a LayoutModel to SVG.
/// </summary>
internal static class SvgRenderer
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

        // Add white background
        svg.Add(new XElement(SvgNamespace + "rect",
            new XAttribute("width", viewBoxWidth),
            new XAttribute("height", viewBoxHeight),
            new XAttribute("fill", "white")
        ));

        // Render each system
        foreach (var system in layoutModel.Systems)
        {
            svg.Add(SystemLayoutRenderer.Instance.Render(system, context));
        }

        // Add <defs> section with used glyphs after rendering completes
        var defs = CreateGlyphDefinitions(context);
        if (defs != null)
        {
            svg.AddFirst(defs);
        }

        return svg;
    }

    /// <summary>
    /// Creates a <defs> element containing path definitions for all glyphs used during rendering.
    /// </summary>
    private static XElement? CreateGlyphDefinitions(SvgContext context)
    {
        var usedGlyphs = context.UsedGlyphs;
        if (usedGlyphs.Count == 0)
        {
            return null;
        }

        var defs = new XElement(SvgNamespace + "defs");

        foreach (var glyph in usedGlyphs)
        {
            var path = new XElement(SvgNamespace + "path",
                new XAttribute("id", glyph.Id),
                new XAttribute("d", glyph.Path)
            );
            defs.Add(path);
        }

        return defs;
    }
}
