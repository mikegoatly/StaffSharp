namespace StaffSharp.MusicXml.Tests;

using StaffSharp.MusicXml;
using StaffSharp.Notation;
using StaffSharp.TestHelpers;
using System.Xml.Linq;

public class MusicXmlParserTests : ScoreTestBase
{
    [Fact]
    public async Task Parse_SingleNote_CreatesCorrectScore()
    {
        // Arrange
        var testFilePath = Path.Combine("TestData", "single-note.xml");
        using var stream = File.OpenRead(testFilePath);
        var importer = new MusicXmlScoreImporter(enableValidation: false);

        // Act
        var score = await importer.ImportAsync(stream);

        // Assert metadata
        Assert.NotNull(score);
        Assert.Equal("Single Note Test", score.Metadata.Title);
        Assert.Equal("Test Composer", score.Metadata.Composer);

        // Verify structure
        AssertPartCount(score, 1);
        var part = GetPart(score);
        Assert.Equal("Melody", part.Name);
        Assert.Equal(Clef.Treble, part.Clef);

        AssertVoiceCount(score, 1);
        AssertVoice(score, voiceIndex: 0, expectedNumber: 1, expectedMeasureCount: 1);

        var measure = GetMeasure(score);
        Assert.Equal(1, measure.Number);

        // Verify note
        var notes = GetNotes(score);
        Assert.Single(notes);

        notes[0].AssertNote(
            expectedPitchClass: PitchClass.C,
            expectedDuration: SymbolicDuration.Quarter,
            expectedOctave: 4);
    }

    [Fact]
    public async Task Parse_CMajorScale_CreatesCorrectNotes()
    {
        // Arrange
        var testFilePath = Path.Combine("TestData", "c-major-scale.xml");
        using var stream = File.OpenRead(testFilePath);
        var importer = new MusicXmlScoreImporter(enableValidation: false);

        // Act
        var score = await importer.ImportAsync(stream);

        // Assert
        Assert.Equal("C Major Scale", score.Metadata.Title);
        AssertPartCount(score, 1);
        AssertVoiceCount(score, 1);

        // Verify all 8 notes of the scale
        var notes = GetNotes(score);
        Assert.Equal(8, notes.Count);

        // C D E F G A B C
        score.AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Eighth, octave: 4)
            .Note(PitchClass.D, SymbolicDuration.Eighth, octave: 4)
            .Note(PitchClass.E, SymbolicDuration.Eighth, octave: 4)
            .Note(PitchClass.F, SymbolicDuration.Eighth, octave: 4)
            .Note(PitchClass.G, SymbolicDuration.Eighth, octave: 4)
            .Note(PitchClass.A, SymbolicDuration.Eighth, octave: 4)
            .Note(PitchClass.B, SymbolicDuration.Eighth, octave: 4)
            .Note(PitchClass.C, SymbolicDuration.Eighth, octave: 5)
            .AndNoMore();
    }

    [Fact]
    public async Task Parse_RestsAndDurations_CreatesCorrectEvents()
    {
        // Arrange
        var testFilePath = Path.Combine("TestData", "rests-and-durations.xml");
        using var stream = File.OpenRead(testFilePath);
        var importer = new MusicXmlScoreImporter(enableValidation: false);

        // Act
        var score = await importer.ImportAsync(stream);

        // Assert
        Assert.Equal("Rests and Durations Test", score.Metadata.Title);

        // Verify sequence: half note, quarter rest, two eighth notes
        score.AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Half, octave: 4)
            .Rest(SymbolicDuration.Quarter)
            .Note(PitchClass.E, SymbolicDuration.Eighth, octave: 4)
            .Note(PitchClass.G, SymbolicDuration.Eighth, octave: 4)
            .AndNoMore();
    }

    [Fact]
    public async Task Parse_Directions_CreatesCorrectDirections()
    {
        // Arrange
        var testFilePath = Path.Combine("TestData", "directions.xml");
        using var stream = File.OpenRead(testFilePath);
        var importer = new MusicXmlScoreImporter(enableValidation: false);

        // Act
        var score = await importer.ImportAsync(stream);

        // Assert
        Assert.Equal("Directions Test", score.Metadata.Title);

        var measure = GetMeasure(score);

        // Should have 3 directions: Allegro text, tempo marking, and dynamic
        measure.AssertDirectionCount(3);

        // Verify Allegro tempo marking (text)
        measure.AssertHasDirection(
            DirectionType.Tempo,
            "Allegro",
            expectedPlacement: Placement.Above);

        // Verify metronome tempo marking
        measure.AssertHasDirection(
            DirectionType.Tempo,
            "♩ = 140",
            expectedPlacement: Placement.Above,
            expectedBpm: 140);

        // Verify dynamic marking
        measure.AssertHasDirection(
            DirectionType.Dynamic,
            "f",
            expectedPlacement: Placement.Below);
    }

    [Fact]
    public async Task Parse_Repeats_CreatesCorrectBarlines()
    {
        // Arrange
        var testFilePath = Path.Combine("TestData", "repeats.xml");
        using var stream = File.OpenRead(testFilePath);
        var importer = new MusicXmlScoreImporter(enableValidation: false);

        // Act
        var score = await importer.ImportAsync(stream);

        // Assert
        Assert.Equal("Repeats Test", score.Metadata.Title);
        var voice = GetVoice(score);
        Assert.Equal(4, voice.Measures.Count);

        // Measure 1: ends with repeat start
        score.GetMeasure(measureIndex: 0)
            .AssertBarlines(expectedEndBarline: BarlineType.RepeatStart);

        // Measure 2: has ending 1, final barline
        var measure2 = score.GetMeasure(measureIndex: 1);
        Assert.NotNull(measure2.RepeatVariants);
        Assert.Single(measure2.RepeatVariants);
        Assert.Equal(1, measure2.RepeatVariants[0]);
        measure2.AssertBarlines(expectedEndBarline: BarlineType.Final);

        // Measure 3: has ending 1 (from left barline), ends with repeat end
        var measure3 = score.GetMeasure(measureIndex: 2);
        Assert.NotNull(measure3.RepeatVariants);
        Assert.Single(measure3.RepeatVariants);
        Assert.Equal(1, measure3.RepeatVariants[0]);
        measure3.AssertBarlines(expectedEndBarline: BarlineType.RepeatEnd);

        // Measure 4: has ending 2, final barline
        var measure4 = score.GetMeasure(measureIndex: 3);
        Assert.NotNull(measure4.RepeatVariants);
        Assert.Single(measure4.RepeatVariants);
        Assert.Equal(2, measure4.RepeatVariants[0]);
        measure4.AssertBarlines(
            expectedStartBarline: null,
            expectedEndBarline: BarlineType.Final);
    }

    [Fact]
    public async Task Parse_MultiPart_CreatesCorrectParts()
    {
        // Arrange
        var testFilePath = Path.Combine("TestData", "multi-part.xml");
        using var stream = File.OpenRead(testFilePath);
        var importer = new MusicXmlScoreImporter(enableValidation: false);

        // Act
        var score = await importer.ImportAsync(stream);

        // Assert
        Assert.Equal("Multi-Part Score", score.Metadata.Title);
        Assert.Equal("Test Composer", score.Metadata.Composer);
        // Note: Key and time signatures are defined per-measure in MusicXML, not in score metadata

        // Verify parts
        AssertPartCount(score, 2);

        var flutePart = GetPart(score, 0);
        Assert.Equal("Flute", flutePart.Name);
        Assert.Equal(Clef.Treble, flutePart.Clef);

        var celloPart = GetPart(score, 1);
        Assert.Equal("Cello", celloPart.Name);
        Assert.Equal(Clef.Bass, celloPart.Clef);

        // Verify flute notes (dotted quarter, eighth, quarter)
        var fluteNotes = GetNotes(score, partIndex: 0);
        Assert.Equal(3, fluteNotes.Count);
        fluteNotes[0].AssertNote(PitchClass.G, new SymbolicDuration(NoteDurationBase.Quarter, dots: 1), expectedOctave: 5);
        fluteNotes[1].AssertNote(PitchClass.A, SymbolicDuration.Eighth, expectedOctave: 5);
        fluteNotes[2].AssertNote(PitchClass.B, SymbolicDuration.Quarter, expectedOctave: 5);

        // Verify cello notes (half, quarter)
        var celloNotes = GetNotes(score, partIndex: 1);
        Assert.Equal(2, celloNotes.Count);
        celloNotes[0].AssertNote(PitchClass.G, SymbolicDuration.Half, expectedOctave: 2);
        celloNotes[1].AssertNote(PitchClass.D, SymbolicDuration.Quarter, expectedOctave: 3);
    }
}
