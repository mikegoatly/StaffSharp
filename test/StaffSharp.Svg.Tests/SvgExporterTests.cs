namespace StaffSharp.Svg.Tests;

using System.IO;
using System.Runtime.CompilerServices;
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
        // Create a scale in bass clef range (E2 to E3)
        var notes = NotationEventBuilder.Create()
            .E(2).F(2).G(2).A(2).B(2).C(3).D(3).E(3)
            .Build();

        var score = CreateScore(notes, Clef.Bass, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_AltoClef_RendersCorrectly()
    {
        // Create a scale in alto clef range (G3 to G4)
        var notes = NotationEventBuilder.Create()
            .G(3).A(3).B(3).C(4).D(4).E(4).F(4).G(4)
            .Build();

        var score = CreateScore(notes, Clef.Alto, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_TenorClef_RendersCorrectly()
    {
        // Create a scale in tenor clef range (C3 to C4)
        var notes = NotationEventBuilder.Create()
            .C(3).D(3).E(3).F(3).G(3).A(3).B(3).C(4)
            .Build();

        var score = CreateScore(notes, Clef.Tenor, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    
    [Fact]
    public async Task Export_EmptyScore_ProducesValidSvg()
    {
        var score = CreateScore([], Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_SimpleScale_RendersCorrectly()
    {
        var notes = NotationEventBuilder.Create()
            .C().D().E().F().G().A().B().C(5)
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_BeamedNotes_RendersBeamsCorrectly()
    {
        var notes = NotationEventBuilder.Create()
            .DefaultDuration(SymbolicDuration.Eighth)
            .C().D().E().F() // First beam group
            .G().A().B().C(5) // Second beam group
            .C().E().G().C() // Arch up in middle
            .E(5).D(5).C(5).E(5)// Dip down in middle
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_ChromaticScale_RendersAccidentalsCorrectly()
    {
        var notes = NotationEventBuilder.Create()
            .C().CSharp().D().DSharp().E().F().FSharp().G().GSharp().A().ASharp().B().C(5)
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
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

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_HighAndLowNotes_RenderExtraLedgerLinesCorrectly()
    {
        var notes = NotationEventBuilder.Create()
            .C(6) // High C
            .E(2) // Low E
            .G(5) // High G
            .A(1) // Very low A
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_TiedNotes_RendersTiesCorrectly()
    {
        var notes = NotationEventBuilder.Create()
            .C(tie: TieType.Start)
            .C(tie: TieType.End)
            .D(tie: TieType.Start)
            .D(tie: TieType.End)
            .E()
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default, "class=\"tie\"");
    }

    [Fact]
    public async Task Export_ChordWithStemUp_AttachesToBottomNote()
    {
        // Create a chord low on the staff (should have stem up)
        // C4-E4-G4 chord using builder
        var notes = NotationEventBuilder.Create()
            .DefaultOctave(4)
            .Chord(PitchClass.C, PitchClass.E, PitchClass.G)
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_ChordWithStemDown_AttachesToTopNote()
    {
        // Create a chord high on the staff (should have stem down)
        // C5-E5-G5 chord using builder
        var notes = NotationEventBuilder.Create()
            .DefaultOctave(5)
            .Chord(PitchClass.C, PitchClass.E, PitchClass.G)
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_KeySignature_RendersAccidentalsAtStart()
    {
        var notes = NotationEventBuilder.Create()
            .G().A().B().C(5).D(5).E(5).FSharp(5).G(5)
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.G, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default, "class=\"key-signature\"");
    }

    [Fact]
    public async Task Export_DottedNotation_RendersAllDurationsCorrectly()
    {
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
            .Chord(4, dottedQuarter, pitchClasses: [PitchClass.C, PitchClass.E, PitchClass.G ])
            .Build();

        var score = CreateScore(events, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_SingleEighthNotes_RendersFlags()
    {
        // Single eighth notes separated by quarter notes (won't beam)
        var notes = NotationEventBuilder.Create()
            .C(4, SymbolicDuration.Eighth)
            .D(4, SymbolicDuration.Quarter)
            .E(4, SymbolicDuration.Eighth)
            .F(4, SymbolicDuration.Quarter)
            .G(4, SymbolicDuration.Eighth)
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default, "class=\"note\"");
    }

    [Fact]
    public async Task Export_SixteenthNotes_RendersDoubleFlags()
    {
        // Single sixteenth notes separated by quarter notes
        var notes = NotationEventBuilder.Create()
            .C(4, SymbolicDuration.Sixteenth)
            .D(4, SymbolicDuration.Quarter)
            .E(4, SymbolicDuration.Sixteenth)
            .F(4, SymbolicDuration.Quarter)
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_ThirtySecondNotes_RendersTripleFlags()
    {
        // Single thirty-second notes
        var notes = NotationEventBuilder.Create()
            .C(4, SymbolicDuration.ThirtySecond)
            .D(4, SymbolicDuration.Half)
            .E(4, SymbolicDuration.ThirtySecond)
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_EighthNoteChords_RendersFlags()
    {
        // Eighth note chords separated by quarter notes (won't beam)
        var notes = NotationEventBuilder.Create()
            .Chord(4, SymbolicDuration.Eighth, pitchClasses: [PitchClass.C, PitchClass.E, PitchClass.G ])
            .D(4, SymbolicDuration.Quarter)
            .Chord(4, SymbolicDuration.Eighth, pitchClasses: [PitchClass.D, PitchClass.F, PitchClass.A ])
            .E(4, SymbolicDuration.Quarter)
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_AllBarlineTypes_RendersCorrectly()
    {
        // Create measures with different barline types
        var measures = new List<Measure>
        {
            // Normal barline (default)
            new(
                1,
                [new NotationNote(new Pitch(PitchClass.C, 4), SymbolicDuration.Whole, Velocity.MezzoForte)],
                endBarline: BarlineType.Normal),

            // Double bar
            new(
                2,
                [new NotationNote(new Pitch(PitchClass.D, 4), SymbolicDuration.Whole, Velocity.MezzoForte)],
                endBarline: BarlineType.DoubleBar),

            // Repeat start
            new(
                3,
                [new NotationNote(new Pitch(PitchClass.E, 4), SymbolicDuration.Whole, Velocity.MezzoForte)],
                startBarline: BarlineType.RepeatStart,
                endBarline: BarlineType.Normal),

            // Repeat end
            new(
                4,
                [new NotationNote(new Pitch(PitchClass.F, 4), SymbolicDuration.Whole, Velocity.MezzoForte)],
                endBarline: BarlineType.RepeatEnd),

            // Repeat both (:|:)
            new(
                5,
                [new NotationNote(new Pitch(PitchClass.G, 4), SymbolicDuration.Whole, Velocity.MezzoForte)],
                endBarline: BarlineType.RepeatBoth),

            // Final barline
            new(
                6,
                [new NotationNote(new Pitch(PitchClass.A, 4), SymbolicDuration.Whole, Velocity.MezzoForte)],
                endBarline: BarlineType.Final)
        };

        var voice = new Voice(1, measures);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Piano", [staff]);
        var metadata = new ScoreMetadata("Barline Types", "Test", KeySignature.C, TimeSignature.CommonTime, 120);
        var score = new NotationScore(metadata, [part]);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
        
    }

    [Fact]
    public async Task Export_VariousArticulations_RendersCorrectly()
    {
        var notes = NotationEventBuilder.Create()
            .C(4, SymbolicDuration.Quarter, decorations: [Decoration.Staccato])
            .D(4, SymbolicDuration.Quarter, decorations: [Decoration.Accent])
            .E(4, SymbolicDuration.Quarter, decorations: [Decoration.Tenuto])
            .F(4, SymbolicDuration.Quarter, decorations: [Decoration.Marcato])
            .G(4, SymbolicDuration.Quarter, decorations: [Decoration.Fermata])
            .A(4, SymbolicDuration.Quarter, decorations: [Decoration.Staccato, Decoration.Accent])
            .B(4, SymbolicDuration.Quarter, decorations: [Decoration.Trill])
            .C(5, SymbolicDuration.Quarter, decorations: [Decoration.UpBow])
            .C(5, SymbolicDuration.Quarter, decorations: [Decoration.DownBow])
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_ChordWithArticulations_RendersCorrectly()
    {
        var notes = NotationEventBuilder.Create()
            .Chord(decorations: [Decoration.Staccato], pitchClasses: [PitchClass.C, PitchClass.E, PitchClass.G])
            .Chord(decorations: [Decoration.Accent], pitchClasses: [PitchClass.D, PitchClass.F, PitchClass.A])
            .Chord(decorations: [Decoration.Fermata], pitchClasses: [PitchClass.C, PitchClass.E, PitchClass.G])
            .Build();

        var score = CreateScore(notes, Clef.Treble, KeySignature.C, TimeSignature.CommonTime);

        await AssertMatchesSnapshotAsync(score, SnapshotOptions.Default);
    }

    private static async Task AssertMatchesSnapshotAsync(
        NotationScore score, 
        SnapshotOptions options, 
        string? expectedContentCheck = null,
        [CallerMemberName] string testName = "")
    {
        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(
            score, 
            stream,
            new Dictionary<string, string>
            {
                ["staffSpace"]= "15",
                ["margins"] = "0,0,0,0",
                ["maxWidth"] = "800"
            });

        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        if (expectedContentCheck != null)
        {
            Assert.Contains(expectedContentCheck, svgContent);
        }

        AssertMatchesSnapshot(svgContent, options, testName);
    }

    private static NotationScore CreateScore(IReadOnlyList<INotationEvent> notes, Clef clef, KeySignature key, TimeSignature timeSignature)
    {
        var metadata = new ScoreMetadata("Test Score", "Test", key, timeSignature, 120);
        var voice = new Voice(1, [new Measure(1, notes)]);
        var staff = new Staff(1, clef, [voice]);
        var part = new Part("Instrument", [staff]);
        return new NotationScore(metadata, [part]);
    }
}