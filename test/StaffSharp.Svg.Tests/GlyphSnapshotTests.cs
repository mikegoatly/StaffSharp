namespace StaffSharp.Svg.Tests;

using StaffSharp.Svg.Tests.Infrastructure;
using StaffSharp.Svg.Render;
using System.Xml.Linq;
using Xunit;

/// <summary>
/// Visual snapshot tests for individual musical glyphs and symbols.
/// These tests verify that individual rendering components produce consistent output.
/// </summary>
public class GlyphSnapshotTests : VisualSnapshotTestBase
{
    [Fact]
    public void AllGlyphs()
    {
        var glyphs = typeof(MusicGlyphs)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(prop => (prop.Name, (GlyphInfo)prop.GetValue(null)!))
            .OrderBy(prop => prop.Name)
            .ToArray();

        var (svg, svgWidth, svgHeight) = CreateCompositeGlyphSvg(glyphs);
        AssertMatchesSnapshot(svg, SnapshotOptions.Default with { Width = svgWidth, Height = svgHeight });
    }

    private static (string, int, int) CreateCompositeGlyphSvg((string Name, GlyphInfo Info)[] glyphs)
    {
        var cellWidth = 120;
        var cellHeight = 120;
        var columns = 6;
        var rows = (glyphs.Length + columns - 1) / columns;

        var svgWidth = cellWidth * columns;
        var svgHeight = cellHeight * rows;

        // group elements
        var elements = new XElement("g");

        for (int i = 0; i < glyphs.Length; i++)
        {
            var (name, glyphInfo) = glyphs[i];
            var col = i % columns;
            var row = i / columns;

            var x = col * cellWidth + cellWidth / 2;
            var y = row * cellHeight + cellHeight / 2;

            // Scale to fit within cell
            var scale = Math.Min(cellWidth * 0.7 / glyphInfo.Width, (cellHeight * 0.7) / glyphInfo.Height);
            var translateX = x - (glyphInfo.MinX + glyphInfo.Width / 2) * scale;
            var translateY = y - (glyphInfo.MinY + glyphInfo.Height / 2) * scale;

            var group = new XElement("g",
                new XAttribute("transform", $"translate({translateX:F2},{translateY:F2}) scale({scale:F2})"));

            var path = new XElement("path",
                new XAttribute("d", glyphInfo.Path),
                new XAttribute("fill", "black"),
                new XAttribute("stroke", "none"));

            group.Add(path);
            elements.Add(group);

            // Add label
            var text = new XElement("text",
                new XAttribute("x", x),
                new XAttribute("y", row * cellHeight + cellHeight - 10),
                new XAttribute("text-anchor", "middle"),
                new XAttribute("font-size", "10"),
                new XAttribute("font-family", "Arial, sans-serif"),
                new XAttribute("fill", "#666"),
                name);

            elements.Add(text);
        }


        return (
            SvgTestHelpers.CreateSvgWrapper(elements, width: svgWidth, height: svgHeight),
            svgWidth,
            svgHeight);
    }
}
