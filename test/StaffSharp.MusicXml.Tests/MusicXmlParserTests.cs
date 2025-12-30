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
}
