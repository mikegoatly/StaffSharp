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
    public async Task Export_BassClef_RendersCorrectly()
    {
        var metadata = new ScoreMetadata("Bass Clef Test", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        // Create a scale in bass clef range (E2 to E3)
        var notes = NotationEventBuilder.Create()
            .E(2).F(2).G(2).A(2).B(2).C(3).D(3).E(3)
            .Build();

        var measure = new Measure(1, notes);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Bass, [voice]);
        var part = new Part("Bass", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_AltoClef_RendersCorrectly()
    {
        var metadata = new ScoreMetadata("Alto Clef Test", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        // Create a scale in alto clef range (G3 to G4)
        var notes = NotationEventBuilder.Create()
            .G(3).A(3).B(3).C(4).D(4).E(4).F(4).G(4)
            .Build();

        var measure = new Measure(1, notes);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Alto, [voice]);
        var part = new Part("Viola", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_TenorClef_RendersCorrectly()
    {
        var metadata = new ScoreMetadata("Tenor Clef Test", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        // Create a scale in tenor clef range (C3 to C4)
        var notes = NotationEventBuilder.Create()
            .C(3).D(3).E(3).F(3).G(3).A(3).B(3).C(4)
            .Build();

        var measure = new Measure(1, notes);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Tenor, [voice]);
        var part = new Part("Cello", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    
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

        AssertMatchesSnapshot(svgContent, new SnapshotOptions(600, 400, 0.5, 5, true));
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
            .C().E().G().C() // Arch up in middle
            .E(5).D(5).C(5).E(5)// Dip down in middle
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
    public async Task Export_ChordWithStemUp_AttachesToBottomNote()
    {
        var metadata = new ScoreMetadata("Chord Stem Up", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        // Create a chord low on the staff (should have stem up)
        // C4-E4-G4 chord using builder
        var notes = NotationEventBuilder.Create()
            .DefaultOctave(4)
            .Chord(PitchClass.C, PitchClass.E, PitchClass.G)
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

        // This test verifies the stem attaches to the outermost notehead
        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_ChordWithStemDown_AttachesToTopNote()
    {
        var metadata = new ScoreMetadata("Chord Stem Down", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        // Create a chord high on the staff (should have stem down)
        // C5-E5-G5 chord using builder
        var notes = NotationEventBuilder.Create()
            .DefaultOctave(5)
            .Chord(PitchClass.C, PitchClass.E, PitchClass.G)
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

        // This test verifies the stem attaches to the outermost notehead
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

    [Fact]
    public async Task Export_DottedNotation_RendersAllDurationsCorrectly()
    {
        var metadata = new ScoreMetadata("Dotted Notation Test", "Test", KeySignature.C, TimeSignature.CommonTime, 120);

        // Test dotted notes of various durations, dotted rests, and dotted chords
        var dottedHalf = new SymbolicDuration(NoteDurationBase.Half, dots: 1);
        var dottedQuarter = new SymbolicDuration(NoteDurationBase.Quarter, dots: 1);
        var dottedEighth = new SymbolicDuration(NoteDurationBase.Eighth, dots: 1);
        var doubleDottedQuarter = new SymbolicDuration(NoteDurationBase.Quarter, dots: 2);

        var events = NotationEventBuilder.Create()
            // Dotted half note
            .C(4, dottedHalf)
            // Dotted quarter notes
            .D(4, dottedQuarter)
            .E(4, dottedQuarter)
            // Dotted eighth notes
            .F(4, dottedEighth)
            .G(4, dottedEighth)
            // Double-dotted quarter note
            .A(4, doubleDottedQuarter)
            // Dotted quarter rest
            .Rest(dottedQuarter)
            // Dotted chord
            .Chord(4, dottedQuarter, null, PitchClass.C, PitchClass.E, PitchClass.G)
            .Build();

        var measure = new Measure(1, events);
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
}