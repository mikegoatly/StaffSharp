namespace StaffSharp.Importers.Tests.Abc;

using StaffSharp;
using StaffSharp.Importers.Abc;
using StaffSharp.Notation;

public class AbcParserTests
{

    [Fact]
    public void Parse_MinimalTune_CreatesScore()
    {
        var abc = """
            X:1
            T:Test Tune
            K:C
            C D E F|
            """;

        var score = AbcParser.Parse(abc);

        Assert.NotNull(score);
        Assert.Equal("Test Tune", score.Metadata.Title);
        Assert.Equal(KeySignature.C, score.Metadata.KeySignature);
    }

    [Fact]
    public void Parse_PaddyORafferty_ParsesCorrectly()
    {
        // Real example from ABC tutorial
        var abc = """
            X:1
            T:Paddy O'Rafferty
            C:Trad.
            M:6/8
            K:D
            dff cee|def gfe|
            """;

        var score = AbcParser.Parse(abc);

        Assert.Equal("Paddy O'Rafferty", score.Metadata.Title);
        Assert.Equal("Trad.", score.Metadata.Composer);
        Assert.Equal(new TimeSignature(6, 8), score.Metadata.TimeSignature);
        Assert.Equal(KeySignature.D, score.Metadata.KeySignature);

        var part = score.Parts[0];
        Assert.Single(part.Voices);

        var measures = part.Voices[0].Measures;
        Assert.Equal(2, measures.Count);
    }

    [Fact]
    public void Parse_OctaveRange_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Octaves
            M:C
            L:1/4
            K:C
            C, D, E, F,|G, A, B, C|D E F G|A B c d|
            """;

        var score = AbcParser.Parse(abc);

        Assert.Equal(4, score.Parts[0].Voices[0].Measures.Count);

        // First measure: C, D, E, F, (octave 3)
        score.AssertSequence(measureIndex: 0)
            .Note(PitchClass.C, SymbolicDuration.Quarter, octave: 3)
            .Note(PitchClass.D, SymbolicDuration.Quarter, octave: 3)
            .Note(PitchClass.E, SymbolicDuration.Quarter, octave: 3)
            .Note(PitchClass.F, SymbolicDuration.Quarter, octave: 3)
            .AndNoMore();

        // Third measure: D E F G (octave 4 - uppercase default)
        score.AssertSequence(measureIndex: 2)
            .Note(PitchClass.D, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.E, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.F, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.G, SymbolicDuration.Quarter, octave: 4)
            .AndNoMore();

        // Fourth measure: A B c d (c and d are lowercase = octave 5)
        score.AssertSequence(measureIndex: 3)
            .Note(PitchClass.A, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.B, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.C, SymbolicDuration.Quarter, octave: 5)
            .Note(PitchClass.D, SymbolicDuration.Quarter, octave: 5)
            .AndNoMore();
    }

    [Fact]
    public void Parse_NoteDurations_WithDefaultLength()
    {
        var abc = """
            X:1
            T:Durations
            M:C
            L:1/8
            K:C
            A A2 A4 A/2|
            """;

        var score = AbcParser.Parse(abc);

        // With L:1/8, default is eighth note
        score.AssertSequence()
            .Note(PitchClass.A, SymbolicDuration.Eighth)     // A (default)
            .Note(PitchClass.A, SymbolicDuration.Quarter)    // A2 (2 * 1/8 = 1/4)
            .Note(PitchClass.A, SymbolicDuration.Half)       // A4 (4 * 1/8 = 1/2)
            .Note(PitchClass.A, SymbolicDuration.Sixteenth)  // A/2 (1/8 / 2 = 1/16)
            .AndNoMore();
    }

    [Fact]
    public void Parse_Accidentals_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Accidentals
            M:C
            L:1/4
            K:C
            ^C _D =E|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Quarter, accidental: Accidental.Sharp)
            .Note(PitchClass.D, SymbolicDuration.Quarter, accidental: Accidental.Flat)
            .Note(PitchClass.E, SymbolicDuration.Quarter, accidental: Accidental.Natural)
            .AndNoMore();
    }

    [Fact]
    public void Parse_TimeSignatures_ParsesCorrectly()
    {
        var commonTime = AbcParser.Parse("X:1\nT:Test\nM:C\nK:C\nC|");
        Assert.Equal(TimeSignature.CommonTime, commonTime.Metadata.TimeSignature);

        var cutTime = AbcParser.Parse("X:1\nT:Test\nM:C|\nK:C\nC|");
        Assert.Equal(new TimeSignature(2, 2), cutTime.Metadata.TimeSignature);

        var sixEight = AbcParser.Parse("X:1\nT:Test\nM:6/8\nK:C\nC|");
        Assert.Equal(new TimeSignature(6, 8), sixEight.Metadata.TimeSignature);
    }

    [Fact]
    public void Parse_KeySignatures_AllMajorKeys()
    {
        var keys = new[]
        {
            ("C", KeySignature.C),
            ("G", KeySignature.G),
            ("D", KeySignature.D),
            ("A", KeySignature.A),
            ("E", KeySignature.E),
            ("B", KeySignature.B),
            ("F#", KeySignature.FSharp),
            ("F", KeySignature.F),
            ("Bb", KeySignature.BFlat),
            ("Eb", KeySignature.EFlat)
        };

        foreach (var (keyName, expected) in keys)
        {
            var abc = $"X:1\nT:Test\nK:{keyName}\nC|";
            var score = AbcParser.Parse(abc);
            Assert.Equal(expected, score.Metadata.KeySignature);
        }
    }

    [Fact]
    public void Parse_SimpleCMajorScale_VerifiesAllNotes()
    {
        var abc = """
            X:1
            T:C Major Scale
            M:4/4
            L:1/4
            K:C
            C D E F|G A B c|
            """;

        var score = AbcParser.Parse(abc);

        // Verify first measure: C D E F (all octave 4, quarter notes)
        score.AssertSequence(measureIndex: 0)
            .Note(PitchClass.C, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.D, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.E, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.F, SymbolicDuration.Quarter, octave: 4)
            .AndNoMore();

        // Verify second measure: G A B c (G, A, B at octave 4, c at octave 5)
        score.AssertSequence(measureIndex: 1)
            .Note(PitchClass.G, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.A, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.B, SymbolicDuration.Quarter, octave: 4)
            .Note(PitchClass.C, SymbolicDuration.Quarter, octave: 5)
            .AndNoMore();
    }

    [Fact]
    public void Parse_MixedDurations_VerifiesCorrectly()
    {
        var abc = """
            X:1
            T:Mixed Durations
            M:4/4
            L:1/8
            K:C
            C2 D E2 F|
            """;

        var score = AbcParser.Parse(abc);

        // C2 (quarter), D (eighth), E2 (quarter), F (eighth)
        score.AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Quarter)  // C2 with L:1/8 = 2*1/8 = 1/4
            .Note(PitchClass.D, SymbolicDuration.Eighth)   // D with L:1/8 = 1/8
            .Note(PitchClass.E, SymbolicDuration.Quarter)  // E2
            .Note(PitchClass.F, SymbolicDuration.Eighth)   // F
            .AndNoMore();
    }

    [Fact]
    public void Parse_SimpleRest_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Rest Test
            M:4/4
            L:1/4
            K:C
            C Z D Z|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Quarter)
            .Rest(SymbolicDuration.Quarter)
            .Note(PitchClass.D, SymbolicDuration.Quarter)
            .Rest(SymbolicDuration.Quarter)
            .AndNoMore();
    }

    [Fact]
    public void Parse_RestWithDurations_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Rest Durations
            M:4/4
            L:1/8
            K:C
            Z Z2 Z4 Z/2|
            """;

        var score = AbcParser.Parse(abc);

        // With L:1/8, default is eighth note
        score.AssertSequence()
            .Rest(SymbolicDuration.Eighth)      // Z (default)
            .Rest(SymbolicDuration.Quarter)     // Z2 (2 * 1/8 = 1/4)
            .Rest(SymbolicDuration.Half)        // Z4 (4 * 1/8 = 1/2)
            .Rest(SymbolicDuration.Sixteenth)   // Z/2 (1/8 / 2 = 1/16)
            .AndNoMore();
    }

    [Fact]
    public void Parse_LowercaseRest_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Lowercase Rest
            M:4/4
            L:1/4
            K:C
            C z D z|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Quarter)
            .Rest(SymbolicDuration.Quarter)
            .Note(PitchClass.D, SymbolicDuration.Quarter)
            .Rest(SymbolicDuration.Quarter)
            .AndNoMore();
    }

    [Fact]
    public void Parse_MeasureOfRests_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:All Rests
            M:4/4
            L:1/4
            K:C
            Z Z Z Z|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .Rest(SymbolicDuration.Quarter)
            .Rest(SymbolicDuration.Quarter)
            .Rest(SymbolicDuration.Quarter)
            .Rest(SymbolicDuration.Quarter)
            .AndNoMore();
    }

    [Fact]
    public void Parse_SimpleChord_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Chord Test
            M:4/4
            L:1/4
            K:C
            [CEG] [FAc]|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .Chord([PitchClass.C, PitchClass.E, PitchClass.G], SymbolicDuration.Quarter)
            .Chord([PitchClass.F, PitchClass.A, PitchClass.C], SymbolicDuration.Quarter)
            .AndNoMore();

        // For octave-specific checks, use the extension methods
        var chords = score.GetChords();
        Assert.Equal(4, chords[1].Pitches[0].Octave); // F is uppercase = octave 4
        Assert.Equal(5, chords[1].Pitches[2].Octave); // c is lowercase = octave 5
    }

    [Fact]
    public void Parse_ChordWithAccidentals_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Chord Accidentals
            M:4/4
            L:1/4
            K:C
            [^C_E=G] [CEG]2|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .Chord([PitchClass.C, PitchClass.E, PitchClass.G], SymbolicDuration.Quarter)
            .Chord([PitchClass.C, PitchClass.E, PitchClass.G], SymbolicDuration.Half)  // [CEG]2 = half note
            .AndNoMore();

        // Verify accidentals on first chord
        var chords = score.GetChords();
        Assert.Equal(Accidental.Sharp, chords[0].Pitches[0].Accidental);
        Assert.Equal(Accidental.Flat, chords[0].Pitches[1].Accidental);
        Assert.Equal(Accidental.Natural, chords[0].Pitches[2].Accidental);
    }

    [Fact]
    public void Parse_ChordWithOctaveModifiers_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Chord Octaves
            M:4/4
            L:1/4
            K:C
            [C,EG] [c'e'g']|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .Chord([PitchClass.C, PitchClass.E, PitchClass.G], SymbolicDuration.Quarter)
            .Chord([PitchClass.C, PitchClass.E, PitchClass.G], SymbolicDuration.Quarter)
            .AndNoMore();

        // Verify octaves
        var chords = score.GetChords();

        // First chord: C, E G (low C)
        Assert.Equal(3, chords[0].Pitches[0].Octave); // C, = octave 3
        Assert.Equal(4, chords[0].Pitches[1].Octave); // E = octave 4
        Assert.Equal(4, chords[0].Pitches[2].Octave); // G = octave 4

        // Second chord: c' e' g' (high)
        Assert.Equal(6, chords[1].Pitches[0].Octave); // c' = octave 6
        Assert.Equal(6, chords[1].Pitches[1].Octave); // e' = octave 6
        Assert.Equal(6, chords[1].Pitches[2].Octave); // g' = octave 6
    }

    [Fact]
    public void Parse_SimpleTie_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Tie Test
            M:4/4
            L:1/4
            K:C
            C-C D E|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Quarter, tie: TieType.Start)
            .Note(PitchClass.C, SymbolicDuration.Quarter)
            .Note(PitchClass.D, SymbolicDuration.Quarter)
            .Note(PitchClass.E, SymbolicDuration.Quarter)
            .AndNoMore();
    }

    [Fact]
    public void Parse_TieChain_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Tie Chain
            M:4/4
            L:1/4
            K:C
            A-A-A-A|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .Note(PitchClass.A, SymbolicDuration.Quarter, tie: TieType.Start)
            .Note(PitchClass.A, SymbolicDuration.Quarter, tie: TieType.Start)
            .Note(PitchClass.A, SymbolicDuration.Quarter, tie: TieType.Start)
            .Note(PitchClass.A, SymbolicDuration.Quarter)
            .AndNoMore();
    }

    [Fact]
    public void Parse_TiedChord_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Tied Chord
            M:4/4
            L:1/2
            K:C
            [CEG]-[CEG]|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .Chord([PitchClass.C, PitchClass.E, PitchClass.G], SymbolicDuration.Half, tie: TieType.Start)
            .Chord([PitchClass.C, PitchClass.E, PitchClass.G], SymbolicDuration.Half)
            .AndNoMore();
    }

    [Fact]
    public void Parse_TiedNotesWithDurations_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Tied Durations
            M:4/4
            L:1/8
            K:C
            C2-C2 D4|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .Note(PitchClass.C, SymbolicDuration.Quarter, tie: TieType.Start)  // C2 (quarter note, tied)
            .Note(PitchClass.C, SymbolicDuration.Quarter)                      // C2 (quarter note, not tied)
            .Note(PitchClass.D, SymbolicDuration.Half)                         // D4 (half note, not tied)
            .AndNoMore();
    }

    [Fact]
    public void Parse_BrokenRhythm_Greater_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Broken Rhythm Test
            M:4/4
            L:1/4
            K:C
            A>B C D|
            """;

        var score = AbcParser.Parse(abc);

        // A>B: A is dotted (3/2 of quarter = dotted quarter), B is halved (1/2 of quarter = eighth)
        score.AssertSequence()
            .Note(PitchClass.A, new SymbolicDuration(NoteDurationBase.Quarter, dots: 1))
            .Note(PitchClass.B, SymbolicDuration.Eighth)
            .Note(PitchClass.C, SymbolicDuration.Quarter)
            .Note(PitchClass.D, SymbolicDuration.Quarter)
            .AndNoMore();
    }

    [Fact]
    public void Parse_BrokenRhythm_Less_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Broken Rhythm Less
            M:4/4
            L:1/4
            K:C
            A<B C D|
            """;

        var score = AbcParser.Parse(abc);

        // A<B: A is halved (1/2 of quarter = eighth), B is dotted (3/2 of quarter = dotted quarter)
        score.AssertSequence()
            .Note(PitchClass.A, SymbolicDuration.Eighth)
            .Note(PitchClass.B, new SymbolicDuration(NoteDurationBase.Quarter, dots: 1))
            .Note(PitchClass.C, SymbolicDuration.Quarter)
            .Note(PitchClass.D, SymbolicDuration.Quarter)
            .AndNoMore();
    }

    [Fact]
    public void Parse_DoubleBrokenRhythm_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Double Broken
            M:4/4
            L:1/4
            K:C
            A>>B|
            """;

        var score = AbcParser.Parse(abc);

        // A>>B: A is double dotted (7/4 of quarter), B is quartered (1/4 of quarter = sixteenth)
        score.AssertSequence()
            .Note(PitchClass.A, new SymbolicDuration(NoteDurationBase.Quarter, dots: 2))
            .Note(PitchClass.B, SymbolicDuration.Sixteenth)
            .AndNoMore();
    }

    [Fact]
    public void Parse_BrokenRhythmWithDurations_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Broken with Durations
            M:4/4
            L:1/8
            K:C
            A2>B2 C4|
            """;

        var score = AbcParser.Parse(abc);

        // A2>B2: Base durations are quarters (L:1/8, multiplied by 2)
        // After broken rhythm: A = quarter * 3/2 = dotted quarter, B = quarter * 1/2 = eighth
        score.AssertSequence()
            .Note(PitchClass.A, new SymbolicDuration(NoteDurationBase.Quarter, dots: 1))
            .Note(PitchClass.B, SymbolicDuration.Eighth)
            .Note(PitchClass.C, SymbolicDuration.Half)
            .AndNoMore();
    }

    [Fact]
    public void Parse_Triplet_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Triplet Test
            M:4/4
            L:1/8
            K:C
            (3ABC DEF|
            """;

        var score = AbcParser.Parse(abc);

        score.AssertSequence()
            .HasCount(6)
            .Note(PitchClass.A, SymbolicDuration.Eighth, tuplet: new Tuplet(3, 2))
            .Note(PitchClass.B, SymbolicDuration.Eighth, tuplet: new Tuplet(3, 2))
            .Note(PitchClass.C, SymbolicDuration.Eighth, tuplet: new Tuplet(3, 2))
            .Note(PitchClass.D, SymbolicDuration.Eighth)
            .Note(PitchClass.E, SymbolicDuration.Eighth)
            .Note(PitchClass.F, SymbolicDuration.Eighth)
            .AndNoMore();
    }

    [Fact]
    public void Parse_Quintuplet_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Quintuplet Test
            M:4/4
            L:1/8
            K:C
            (5ABCDE F|
            """;

        var score = AbcParser.Parse(abc);

        // First five notes should be quintuplets (5 notes in time of 4)
        score.AssertSequence()
            .Note(PitchClass.A, SymbolicDuration.Eighth, tuplet: new Tuplet(5, 4))
            .Note(PitchClass.B, SymbolicDuration.Eighth, tuplet: new Tuplet(5, 4))
            .Note(PitchClass.C, SymbolicDuration.Eighth, tuplet: new Tuplet(5, 4))
            .Note(PitchClass.D, SymbolicDuration.Eighth, tuplet: new Tuplet(5, 4))
            .Note(PitchClass.E, SymbolicDuration.Eighth, tuplet: new Tuplet(5, 4))
            .Note(PitchClass.F, SymbolicDuration.Eighth)  // Normal
            .AndNoMore();
    }

    [Fact]
    public void Parse_TupletWithExplicitRatio_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Explicit Tuplet
            M:4/4
            L:1/8
            K:C
            (3:2 ABC D|
            """;

        var score = AbcParser.Parse(abc);

        // (3:2 means 3 notes in the time of 2
        score.AssertSequence()
            .Note(PitchClass.A, SymbolicDuration.Eighth, tuplet: new Tuplet(3, 2))
            .Note(PitchClass.B, SymbolicDuration.Eighth, tuplet: new Tuplet(3, 2))
            .Note(PitchClass.C, SymbolicDuration.Eighth, tuplet: new Tuplet(3, 2))
            .Note(PitchClass.D, SymbolicDuration.Eighth)
            .AndNoMore();
    }

    [Fact]
    public void Parse_Duplet_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Duplet Test
            M:6/8
            L:1/8
            K:C
            (2AB CDE|
            """;

        var score = AbcParser.Parse(abc);

        // (2 means 2 notes in time of 3 (duplet in compound time)
        score.AssertSequence()
            .Note(PitchClass.A, SymbolicDuration.Eighth, tuplet: new Tuplet(2, 3))
            .Note(PitchClass.B, SymbolicDuration.Eighth, tuplet: new Tuplet(2, 3))
            .Note(PitchClass.C, SymbolicDuration.Eighth)
            .Note(PitchClass.D, SymbolicDuration.Eighth)
            .Note(PitchClass.E, SymbolicDuration.Eighth)
            .AndNoMore();
    }

    [Fact]
    public void Parse_SimpleGraceNote_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Grace Note Test
            M:4/4
            L:1/4
            K:C
            {A}C D E|
            """;

        var score = AbcParser.Parse(abc);

        var notes = score.GetNotes();
        Assert.Equal(3, notes.Count);

        // First note should have grace note
        Assert.NotNull(notes[0].GraceNote);
        var graceNote = notes[0].GraceNote!.Value;
        Assert.Single(graceNote.Pitches);
        Assert.Equal(PitchClass.A, graceNote.Pitches[0].PitchClass);
        Assert.False(graceNote.IsAcciaccatura);

        // Other notes should not have grace notes
        Assert.Null(notes[1].GraceNote);
        Assert.Null(notes[2].GraceNote);
    }

    [Fact]
    public void Parse_MultipleGraceNotes_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Multiple Grace Notes
            M:4/4
            L:1/4
            K:C
            {ABC}D E F|
            """;

        var score = AbcParser.Parse(abc);

        var notes = score.GetNotes();
        Assert.Equal(3, notes.Count);

        // First note should have three grace notes
        Assert.NotNull(notes[0].GraceNote);
        var graceNote = notes[0].GraceNote!.Value;
        Assert.Equal(3, graceNote.Pitches.Count);
        Assert.Equal(PitchClass.A, graceNote.Pitches[0].PitchClass);
        Assert.Equal(PitchClass.B, graceNote.Pitches[1].PitchClass);
        Assert.Equal(PitchClass.C, graceNote.Pitches[2].PitchClass);
        Assert.False(graceNote.IsAcciaccatura);
    }

    [Fact]
    public void Parse_Acciaccatura_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Acciaccatura Test
            M:4/4
            L:1/4
            K:C
            {/A}C D E|
            """;

        var score = AbcParser.Parse(abc);

        var notes = score.GetNotes();
        Assert.Equal(3, notes.Count);

        // First note should have acciaccatura
        Assert.NotNull(notes[0].GraceNote);
        var graceNote = notes[0].GraceNote!.Value;
        Assert.Single(graceNote.Pitches);
        Assert.Equal(PitchClass.A, graceNote.Pitches[0].PitchClass);
        Assert.True(graceNote.IsAcciaccatura);
    }

    [Fact]
    public void Parse_GraceNoteWithAccidentals_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Grace Note Accidentals
            M:4/4
            L:1/4
            K:C
            {^A_B}C D|
            """;

        var score = AbcParser.Parse(abc);

        var notes = score.GetNotes();
        Assert.Equal(2, notes.Count);

        // First note should have two grace notes with accidentals
        Assert.NotNull(notes[0].GraceNote);
        var graceNote = notes[0].GraceNote!.Value;
        Assert.Equal(2, graceNote.Pitches.Count);
        Assert.Equal(Accidental.Sharp, graceNote.Pitches[0].Accidental);
        Assert.Equal(Accidental.Flat, graceNote.Pitches[1].Accidental);
    }

    [Fact]
    public void Parse_GraceNoteOnChord_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Grace Note on Chord
            M:4/4
            L:1/4
            K:C
            {A}[CEG] D|
            """;

        var score = AbcParser.Parse(abc);

        var events = score.GetEvents();
        Assert.Equal(2, events.Count);

        // First event should be a chord with grace note
        var chord = Assert.IsType<Chord>(events[0]);
        Assert.NotNull(chord.GraceNote);
        var graceNote = chord.GraceNote!.Value;
        Assert.Single(graceNote.Pitches);
        Assert.Equal(PitchClass.A, graceNote.Pitches[0].PitchClass);
    }

    [Fact]
    public void Parse_GraceNoteWithOctaves_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Grace Note Octaves
            M:4/4
            L:1/4
            K:C
            {A,a}C D|
            """;

        var score = AbcParser.Parse(abc);

        var notes = score.GetNotes();
        Assert.Equal(2, notes.Count);

        // First note should have two grace notes with different octaves
        Assert.NotNull(notes[0].GraceNote);
        var graceNote = notes[0].GraceNote!.Value;
        Assert.Equal(2, graceNote.Pitches.Count);
        Assert.Equal(3, graceNote.Pitches[0].Octave); // A, = octave 3
        Assert.Equal(5, graceNote.Pitches[1].Octave); // a = octave 5
    }
}
