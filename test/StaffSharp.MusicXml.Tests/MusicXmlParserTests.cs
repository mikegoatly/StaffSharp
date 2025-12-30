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
}
