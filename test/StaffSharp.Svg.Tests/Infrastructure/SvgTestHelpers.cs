namespace StaffSharp.Svg.Tests.Infrastructure;

using System.Globalization;
using System.Text;
using System.Xml.Linq;

/// <summary>
/// Helper methods for creating SVG test content.
/// </summary>
public static class SvgTestHelpers
{
    /// <summary>
    /// Creates a minimal SVG wrapper for testing individual glyphs or elements.
    /// </summary>
    public static string CreateSvgWrapper(
        XElement content,
        int width = 200,
        int height = 200,
        string? viewBox = null)
    {
        viewBox ??= $"0 0 {width} {height}";

        var svg = new XElement(
            XName.Get("svg", "http://www.w3.org/2000/svg"),
            new XAttribute("width", width),
            new XAttribute("height", height),
            new XAttribute("viewBox", viewBox),
            content
        );

        return svg.ToString(SaveOptions.DisableFormatting);
    }
}
