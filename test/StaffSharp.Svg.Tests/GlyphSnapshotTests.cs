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
    public void NoteHeadWhole()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.NoteHeadWhole);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void NoteHeadHalf()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.NoteHeadHalf);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void NoteHeadBlack()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.NoteHeadBlack);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void WholeRest()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.WholeRest);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void HalfRest()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.HalfRest);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void QuarterRest()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.QuarterRest);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void EighthRest()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.EighthRest);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void SixteenthRest()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.SixteenthRest);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void TrebleClef()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.TrebleClef);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void BassClef()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.BassClef);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void CClef()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.CClef);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void AltoClef()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.AltoClef);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void TenorClef()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.TenorClef);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Flat()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Flat);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Natural()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Natural);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Sharp()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Sharp);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Digit0()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Digit0);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Digit1()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Digit1);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Digit2()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Digit2);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Digit3()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Digit3);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Digit4()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Digit4);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Digit5()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Digit5);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Digit6()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Digit6);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Digit7()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Digit7);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Digit8()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Digit8);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void Digit9()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.Digit9);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void CommonTime()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.CommonTime);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    [Fact]
    public void CutTime()
    {
        var svg = CreateGlyphSvg(MusicGlyphs.CutTime);
        AssertMatchesSnapshot(svg, SnapshotOptions.Strict);
    }

    private static string CreateGlyphSvg(GlyphInfo glyphInfo)
    {
        var path = new XElement("path",
            new XAttribute("d", glyphInfo.Path),
            new XAttribute("fill", "black"),
            new XAttribute("stroke", "none"));

        // Use a viewBox that fits most glyphs with some padding
        string viewBox = $"{glyphInfo.MinX - 10} {glyphInfo.MinY - 10} {glyphInfo.Width + 20} {glyphInfo.Height + 20}";
        return SvgTestHelpers.CreateSvgWrapper(path, width: 200, height: 200, viewBox: viewBox);
    }
}
