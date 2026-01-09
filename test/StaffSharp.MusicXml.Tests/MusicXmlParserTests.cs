namespace StaffSharp.MusicXml.Tests;

using StaffSharp.MusicXml;
using StaffSharp.Notation;
using StaffSharp.TestHelpers;
using System.Linq;
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

    [Fact]
    public async Task Parse_Slurs_CreatesCorrectSlurs()
    {
        // Arrange
        var testFilePath = Path.Combine("TestData", "slurs.xml");
        using var stream = File.OpenRead(testFilePath);
        var importer = new MusicXmlScoreImporter(enableValidation: false);

        // Act
        var score = await importer.ImportAsync(stream);

        // Assert
        Assert.Equal("Slurs Test", score.Metadata.Title);

        var measure = GetMeasure(score);
        var slurs = GetSlurs(score);
        var part = GetPart(score);
        var allNotes = GetNotes(score);

        // Should have one slur covering first three notes (C, D, E)
        Assert.Single(slurs);

        var slur = slurs[0];
        
        // Verify the notes in the slur
        ((NotationNote)slur.StartEvent).AssertNote(PitchClass.C, SymbolicDuration.Quarter, expectedOctave: 4);
        ((NotationNote)slur.EndEvent).AssertNote(PitchClass.E, SymbolicDuration.Quarter, expectedOctave: 4);

        // Verify all 4 notes in measure
        Assert.Equal(4, allNotes.Count);

        // Part-level slur span should link the first and third slurred notes
        var spans = part.Slurs;
        Assert.Single(spans);
        var span = spans[0];
        var startNote = Assert.IsType<NotationNote>(allNotes[0]); // C
        var endNote = Assert.IsType<NotationNote>(allNotes[2]);   // E
        Assert.Same(startNote, span.StartEvent);
        Assert.Same(endNote, span.EndEvent);
        Assert.Equal(1, span.StartStaffNumber);
        Assert.Equal(1, span.EndStaffNumber);
        Assert.Equal(1, span.StartVoiceNumber);
        Assert.Equal(1, span.EndVoiceNumber);
    }

    [Fact]
    public async Task Parse_CrossMeasureSlur_CreatesPartSpan()
    {
        var testFilePath = Path.Combine("TestData", "cross-measure-slur.xml");
        using var stream = File.OpenRead(testFilePath);
        var importer = new MusicXmlScoreImporter(enableValidation: false);

        var score = await importer.ImportAsync(stream);

        var part = GetPart(score);
        var notes = part.Voices[0].Measures
            .SelectMany(m => m.Events)
            .OfType<NotationNote>()
            .ToList();
        Assert.Equal(3, notes.Count);

        var spans = part.Slurs;
        Assert.Single(spans);
        var span = spans[0];

        Assert.Same(notes[0], span.StartEvent); // C4 in measure 1
        Assert.Same(notes[2], span.EndEvent);   // E4 in measure 2
        Assert.Equal(1, span.StartStaffNumber);
        Assert.Equal(1, span.EndStaffNumber);
        Assert.Equal(1, span.StartVoiceNumber);
        Assert.Equal(1, span.EndVoiceNumber);
    }

    [Fact]
    public async Task Parse_Lyrics_CreatesCorrectLyrics()
    {
        // Arrange
        var testFilePath = Path.Combine("TestData", "lyrics.xml");
        using var stream = File.OpenRead(testFilePath);
        var importer = new MusicXmlScoreImporter(enableValidation: false);

        // Act
        var score = await importer.ImportAsync(stream);

        // Assert
        Assert.Equal("Lyrics Test", score.Metadata.Title);

        // Measure 1: "Twin-kle twin-kle"
        score.GetMeasure(measureIndex: 0)
            .AssertLyricCount(1)
            .AssertLyricSyllable(0, 0, "Twin-", LyricSyllableType.Start)
            .AssertLyricSyllable(0, 1, "kle", LyricSyllableType.Middle)
            .AssertLyricSyllable(0, 2, "twin-", LyricSyllableType.Middle)
            .AssertLyricSyllable(0, 3, "kle", LyricSyllableType.End);

        // Measure 2: "star so bright"
        score.GetMeasure(measureIndex: 1)
            .AssertLyricCount(1)
            .AssertLyricSyllable(0, 0, "star", LyricSyllableType.Standalone)
            .AssertLyricSyllable(0, 1, "so", LyricSyllableType.Standalone)
            .AssertLyricSyllable(0, 2, "bright", LyricSyllableType.Hold); // has extend element
    }

    [Fact]
    public async Task Parse_PianoGrandStaff_CreatesCorrectStaves()
    {
        // Arrange
        var testFilePath = Path.Combine("TestData", "piano-grand-staff.xml");
        using var stream = File.OpenRead(testFilePath);
        var importer = new MusicXmlScoreImporter(enableValidation: false);

        // Act
        var score = await importer.ImportAsync(stream);

        // Assert metadata
        Assert.NotNull(score);
        Assert.Equal("Piano Grand Staff Test", score.Metadata.Title);

        // Verify single part
        AssertPartCount(score, 1);
        var part = GetPart(score);
        Assert.Equal("Piano", part.Name);

        // Verify grand staff structure
        Assert.True(part.IsGrandStaff, "Part should be a grand staff");
        Assert.Equal(2, part.Staves.Count);

        // Verify Staff 1 (Treble)
        var trebleStaff = part.Staves[0];
        Assert.Equal(1, trebleStaff.Number);
        Assert.Equal(Clef.Treble, trebleStaff.Clef);
        Assert.Single(trebleStaff.Voices); // Voice 1

        // Verify Staff 2 (Bass)
        var bassStaff = part.Staves[1];
        Assert.Equal(2, bassStaff.Number);
        Assert.Equal(Clef.Bass, bassStaff.Clef);
        Assert.Single(bassStaff.Voices); // Voice 2

        // Verify treble staff notes (measure 1: C5, E5, G5, C6)
        var trebleVoice = trebleStaff.Voices[0];
        Assert.Equal(1, trebleVoice.Number);
        Assert.Equal(2, trebleVoice.Measures.Count);

        var trebleMeasure1 = trebleVoice.Measures[0];
        Assert.Equal(4, trebleMeasure1.Events.Count);

        var trebleNotes1 = trebleMeasure1.Events.OfType<NotationNote>().ToList();
        Assert.Equal(4, trebleNotes1.Count);
        trebleNotes1[0].AssertNote(PitchClass.C, SymbolicDuration.Quarter, expectedOctave: 5);
        trebleNotes1[1].AssertNote(PitchClass.E, SymbolicDuration.Quarter, expectedOctave: 5);
        trebleNotes1[2].AssertNote(PitchClass.G, SymbolicDuration.Quarter, expectedOctave: 5);
        trebleNotes1[3].AssertNote(PitchClass.C, SymbolicDuration.Quarter, expectedOctave: 6);

        // Verify bass staff notes (measure 1: C3 whole note)
        var bassVoice = bassStaff.Voices[0];
        Assert.Equal(2, bassVoice.Number);
        Assert.Equal(2, bassVoice.Measures.Count);

        var bassMeasure1 = bassVoice.Measures[0];
        Assert.Single(bassMeasure1.Events);

        var bassNotes1 = bassMeasure1.Events.OfType<NotationNote>().ToList();
        Assert.Single(bassNotes1);
        bassNotes1[0].AssertNote(PitchClass.C, SymbolicDuration.Whole, expectedOctave: 3);

        // Verify treble staff measure 2 (G5, E5 half notes)
        var trebleMeasure2 = trebleVoice.Measures[1];
        Assert.Equal(2, trebleMeasure2.Events.Count);

        var trebleNotes2 = trebleMeasure2.Events.OfType<NotationNote>().ToList();
        Assert.Equal(2, trebleNotes2.Count);
        trebleNotes2[0].AssertNote(PitchClass.G, SymbolicDuration.Half, expectedOctave: 5);
        trebleNotes2[1].AssertNote(PitchClass.E, SymbolicDuration.Half, expectedOctave: 5);

        // Verify bass staff measure 2 (E3, G3 half notes)
        var bassMeasure2 = bassVoice.Measures[1];
        Assert.Equal(2, bassMeasure2.Events.Count);

        var bassNotes2 = bassMeasure2.Events.OfType<NotationNote>().ToList();
        Assert.Equal(2, bassNotes2.Count);
        bassNotes2[0].AssertNote(PitchClass.E, SymbolicDuration.Half, expectedOctave: 3);
        bassNotes2[1].AssertNote(PitchClass.G, SymbolicDuration.Half, expectedOctave: 3);
    }
}
