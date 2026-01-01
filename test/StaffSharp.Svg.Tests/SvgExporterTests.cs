namespace StaffSharp.Svg.Tests;

using System.IO;
using System.Text;
using StaffSharp.Notation;
using StaffSharp.Svg.Tests.Infrastructure;
using StaffSharp.TestHelpers.Builders;

using Xunit;

/// <summary>
/// Integration tests for SVG score exporter.
/// These tests verify the complete pipeline from NotationScore to SVG output.
/// </summary>
public class SvgExporterTests : VisualSnapshotTestBase
{
    [Fact]
    public async Task Export_EmptyScore_ProducesValidSvg()
    {
        var score = CreateMinimalScore();
        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        Assert.Contains("<svg", svgContent);

        // Visual snapshot test - will create golden image on first run
        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_SimpleScale_RendersCorrectly()
    {
        var score = CreateSimpleScale();
        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_WithCustomOptions_RespectsSettings()
    {
        var score = CreateMinimalScore();
        var exporter = new SvgScoreExporter();
        var options = new Dictionary<string, string>
        {
            ["maxWidth"] = "600",
            ["staffSpace"] = "12",
            ["margins"] = "20,20,20,20"
        };

        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream, options);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        AssertMatchesSnapshot(svgContent, new SnapshotOptions
        {
            Width = 600,
            Height = 400,
            PixelDifferenceThreshold = 0.5,
            MaxPixelDelta = 5,
            GenerateDiffImage = true
        });
    }

    private static NotationScore CreateMinimalScore()
    {
        var metadata = new ScoreMetadata("Test", "Test", KeySignature.C, TimeSignature.CommonTime, 120);
        var measure = new Measure(1, []);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Test Part", [staff]);
        return new NotationScore(metadata, [part]);
    }

    private static NotationScore CreateSimpleScale()
    {
        var metadata = new ScoreMetadata("C Major Scale", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        var notes = NotationEventBuilder.Create()
            .C().D().E().F().G().A().B().C(5)
            .Build();

        var measure = new Measure(1, notes);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Piano", [staff]);
        return new NotationScore(metadata, [part]);
    }

    [Fact]
    public async Task Export_BeamedNotes_RendersBeamsCorrectly()
    {
        var metadata = new ScoreMetadata("Beamed Notes", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        var notes = NotationEventBuilder.Create()
            .DefaultDuration(SymbolicDuration.Eighth)
            .C().D().E().F() // First beam group
            .G().A().B().C(5) // Second beam group
            .Build();

        var measure = new Measure(1, notes);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Piano", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_ChromaticScale_RendersAccidentalsCorrectly()
    {
        var metadata = new ScoreMetadata("Chromatic Scale", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        var notes = NotationEventBuilder.Create()
            .C().CSharp().D().DSharp().E().F().FSharp().G().GSharp().A().ASharp().B().C(5)
            .Build();

        var measure = new Measure(1, notes);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Piano", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_MultiVoice_RendersBothVoicesCorrectly()
    {
        var metadata = new ScoreMetadata("Multi-Voice", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        // Voice 1 - higher melody
        var voice1Notes = NotationEventBuilder.Create()
            .G().A().B().C(5)
            .Build();
        var voice1Measure = new Measure(1, voice1Notes);
        var voice1 = new Voice(1, [voice1Measure]);

        // Voice 2 - lower melody
        var voice2Notes = NotationEventBuilder.Create()
            .C().D().E().F()
            .Build();
        var voice2Measure = new Measure(1, voice2Notes);
        var voice2 = new Voice(2, [voice2Measure]);

        var staff = new Staff(1, Clef.Treble, [voice1, voice2]);
        var part = new Part("Piano", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        // Verify both voices rendered - should have 8 notes total
        var noteCount = svgContent.Split("class=\"note\"").Length - 1;
        Assert.Equal(8, noteCount);
        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_LongScore_CreatesSystemBreaks()
    {
        var metadata = new ScoreMetadata("Long Score", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        // Create many measures to force system breaks
        var measures = Enumerable.Range(1, 8).Select(i =>
        {
            var notes = NotationEventBuilder.Create()
                .C().D().E().F()
                .Build();
            return new Measure(i, notes);
        }).ToList();

        var voice = new Voice(1, measures);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Piano", [staff]);
        var score = new NotationScore(metadata, [part]);

        // Use a small maxWidth to force system breaks
        var options = new Dictionary<string, string>
        {
            ["maxWidth"] = "300"
        };

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream, options);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        // Verify multiple systems are created
        var systemCount = svgContent.Split("class=\"system\"").Length - 1;
        Assert.True(systemCount > 1, $"Expected multiple systems, got {systemCount}");
        AssertMatchesSnapshot(svgContent, new SnapshotOptions
        {
            Width = 800,
            Height = 600,
            PixelDifferenceThreshold = 0.5,
            MaxPixelDelta = 5,
            GenerateDiffImage = true
        });
    }

    [Fact]
    public async Task Export_TiedNotes_RendersTiesCorrectly()
    {
        var metadata = new ScoreMetadata("Tied Notes", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        var notes = NotationEventBuilder.Create()
            .C(tie: TieType.Start)
            .C(tie: TieType.End)
            .D(tie: TieType.Start)
            .D(tie: TieType.End)
            .E()
            .Build();

        var measure = new Measure(1, notes);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Piano", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        // Verify ties are present - they render as class="tie"
        Assert.Contains("class=\"tie\"", svgContent);
        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_Chords_RendersMultipleNoteheadsCorrectly()
    {
        var metadata = new ScoreMetadata("Chords", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        var notes = NotationEventBuilder.Create()
            .Chord(PitchClass.C, PitchClass.E, PitchClass.G) // C major chord
            .Chord(PitchClass.D, PitchClass.F, PitchClass.A) // D minor chord
            .Chord(PitchClass.E, PitchClass.G, PitchClass.B) // E minor chord
            .Chord(PitchClass.F, PitchClass.A, PitchClass.C) // F major chord
            .Build();

        var measure = new Measure(1, notes);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Piano", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        // Verify chords are rendered - should have class="chord"
        Assert.Contains("class=\"chord\"", svgContent);
        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_KeySignature_RendersAccidentalsAtStart()
    {
        var metadata = new ScoreMetadata("G Major", "Test", KeySignature.G, TimeSignature.CommonTime, 120);

        var notes = NotationEventBuilder.Create()
            .G().A().B().C(5).D(5).E(5).FSharp(5).G(5)
            .Build();

        var measure = new Measure(1, notes);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Piano", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        // Verify key signature is present
        Assert.Contains("class=\"key-signature\"", svgContent);
        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }
}