namespace StaffSharp.Importers.Tests.Abc;

using StaffSharp;
using StaffSharp.Importers.Abc;
using StaffSharp.Notation;

public class AbcParserTests
{
    // Helper to verify notes in a measure
    private static void VerifyNotes(Measure measure, params (PitchClass pitch, int octave, SymbolicDuration duration)[] expectedNotes)
    {
        var actualNotes = measure.Events.Cast<NotationNote>().ToList();
        Assert.Equal(expectedNotes.Length, actualNotes.Count);

        for (int i = 0; i < expectedNotes.Length; i++)
        {
            var (expectedPitch, expectedOctave, expectedDuration) = expectedNotes[i];
            var actual = actualNotes[i];

            Assert.Equal(expectedPitch, actual.Pitch.PitchClass);
            Assert.Equal(expectedOctave, actual.Pitch.Octave);
            Assert.Equal(expectedDuration, actual.Duration);
        }
    }

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

        var measures = score.Parts[0].Voices[0].Measures;
        Assert.Equal(4, measures.Count);

        // First measure: C, D, E, F, (octave 3)
        var firstMeasure = measures[0].Events.Cast<NotationNote>().ToList();
        Assert.Equal(4, firstMeasure.Count);
        Assert.Equal(3, firstMeasure[0].Pitch.Octave); // C,
        Assert.Equal(PitchClass.C, firstMeasure[0].Pitch.PitchClass);

        // Third measure: D E F G (octave 4 - uppercase default)
        var thirdMeasure = measures[2].Events.Cast<NotationNote>().ToList();
        Assert.Equal(4, thirdMeasure.Count);
        Assert.Equal(4, thirdMeasure[0].Pitch.Octave); // D

        // Fourth measure: A B c d (c and d are lowercase = octave 5)
        var fourthMeasure = measures[3].Events.Cast<NotationNote>().ToList();
        Assert.Equal(4, fourthMeasure.Count);
        Assert.Equal(4, fourthMeasure[0].Pitch.Octave); // A (uppercase)
        Assert.Equal(5, fourthMeasure[2].Pitch.Octave); // c (lowercase)
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

        var notes = score.Parts[0].Voices[0].Measures[0].Events.Cast<NotationNote>().ToList();
        Assert.Equal(4, notes.Count);

        // With L:1/8, default is eighth note
        Assert.Equal(SymbolicDuration.Eighth, notes[0].Duration); // A (default)
        Assert.Equal(SymbolicDuration.Quarter, notes[1].Duration); // A2 (2 * 1/8 = 1/4)
        Assert.Equal(SymbolicDuration.Half, notes[2].Duration); // A4 (4 * 1/8 = 1/2)
        Assert.Equal(new SymbolicDuration(NoteDurationBase.Sixteenth), notes[3].Duration); // A/2 (1/8 / 2 = 1/16)
    }

    [Fact]
    public void Parse_Accidentals_ParsesCorrectly()
    {
        var abc = """
            X:1
            T:Accidentals
            M:C
            K:C
            ^C _D =E|
            """;

        var score = AbcParser.Parse(abc);

        var notes = score.Parts[0].Voices[0].Measures[0].Events.Cast<NotationNote>().ToList();
        Assert.Equal(3, notes.Count);

        Assert.Equal(Accidental.Sharp, notes[0].Pitch.Accidental);
        Assert.Equal(PitchClass.C, notes[0].Pitch.PitchClass);

        Assert.Equal(Accidental.Flat, notes[1].Pitch.Accidental);
        Assert.Equal(PitchClass.D, notes[1].Pitch.PitchClass);

        Assert.Equal(Accidental.Natural, notes[2].Pitch.Accidental);
        Assert.Equal(PitchClass.E, notes[2].Pitch.PitchClass);
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
        var measures = score.Parts[0].Voices[0].Measures;

        // Verify first measure: C D E F (all octave 4, quarter notes)
        VerifyNotes(measures[0],
            (PitchClass.C, 4, SymbolicDuration.Quarter),
            (PitchClass.D, 4, SymbolicDuration.Quarter),
            (PitchClass.E, 4, SymbolicDuration.Quarter),
            (PitchClass.F, 4, SymbolicDuration.Quarter)
        );

        // Verify second measure: G A B c (G, A, B at octave 4, c at octave 5)
        VerifyNotes(measures[1],
            (PitchClass.G, 4, SymbolicDuration.Quarter),
            (PitchClass.A, 4, SymbolicDuration.Quarter),
            (PitchClass.B, 4, SymbolicDuration.Quarter),
            (PitchClass.C, 5, SymbolicDuration.Quarter)
        );
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
        var measure = score.Parts[0].Voices[0].Measures[0];

        // C2 (quarter), D (eighth), E2 (quarter), F (eighth)
        VerifyNotes(measure,
            (PitchClass.C, 4, SymbolicDuration.Quarter),  // C2 with L:1/8 = 2*1/8 = 1/4
            (PitchClass.D, 4, SymbolicDuration.Eighth),    // D with L:1/8 = 1/8
            (PitchClass.E, 4, SymbolicDuration.Quarter),  // E2
            (PitchClass.F, 4, SymbolicDuration.Eighth)     // F
        );
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
        var events = score.Parts[0].Voices[0].Measures[0].Events;

        Assert.Equal(4, events.Count);

        // First event should be a note
        Assert.IsType<NotationNote>(events[0]);
        var note1 = (NotationNote)events[0];
        Assert.Equal(PitchClass.C, note1.Pitch.PitchClass);
        Assert.Equal(SymbolicDuration.Quarter, note1.Duration);

        // Second event should be a rest
        Assert.IsType<Rest>(events[1]);
        var rest1 = (Rest)events[1];
        Assert.Equal(SymbolicDuration.Quarter, rest1.Duration);

        // Third event should be a note
        Assert.IsType<NotationNote>(events[2]);
        var note2 = (NotationNote)events[2];
        Assert.Equal(PitchClass.D, note2.Pitch.PitchClass);
        Assert.Equal(SymbolicDuration.Quarter, note2.Duration);

        // Fourth event should be a rest
        Assert.IsType<Rest>(events[3]);
        var rest2 = (Rest)events[3];
        Assert.Equal(SymbolicDuration.Quarter, rest2.Duration);
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
        var events = score.Parts[0].Voices[0].Measures[0].Events;

        Assert.Equal(4, events.Count);

        // All should be rests with different durations
        Assert.All(events, e => Assert.IsType<Rest>(e));

        var rests = events.Cast<Rest>().ToList();

        // With L:1/8, default is eighth note
        Assert.Equal(SymbolicDuration.Eighth, rests[0].Duration); // Z (default)
        Assert.Equal(SymbolicDuration.Quarter, rests[1].Duration); // Z2 (2 * 1/8 = 1/4)
        Assert.Equal(SymbolicDuration.Half, rests[2].Duration); // Z4 (4 * 1/8 = 1/2)
        Assert.Equal(new SymbolicDuration(NoteDurationBase.Sixteenth), rests[3].Duration); // Z/2 (1/8 / 2 = 1/16)
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
        var events = score.Parts[0].Voices[0].Measures[0].Events;

        Assert.Equal(4, events.Count);

        // Second and fourth events should be rests
        Assert.IsType<Rest>(events[1]);
        Assert.IsType<Rest>(events[3]);

        var rest1 = (Rest)events[1];
        var rest2 = (Rest)events[3];

        Assert.Equal(SymbolicDuration.Quarter, rest1.Duration);
        Assert.Equal(SymbolicDuration.Quarter, rest2.Duration);
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
        var events = score.Parts[0].Voices[0].Measures[0].Events;

        Assert.Equal(4, events.Count);
        Assert.All(events, e => Assert.IsType<Rest>(e));

        var rests = events.Cast<Rest>().ToList();
        Assert.All(rests, r => Assert.Equal(SymbolicDuration.Quarter, r.Duration));
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
        var events = score.Parts[0].Voices[0].Measures[0].Events;

        Assert.Equal(2, events.Count);

        // First chord: C E G (C major)
        Assert.IsType<Chord>(events[0]);
        var chord1 = (Chord)events[0];
        Assert.Equal(3, chord1.Pitches.Count);
        Assert.Equal(PitchClass.C, chord1.Pitches[0].PitchClass);
        Assert.Equal(PitchClass.E, chord1.Pitches[1].PitchClass);
        Assert.Equal(PitchClass.G, chord1.Pitches[2].PitchClass);
        Assert.Equal(SymbolicDuration.Quarter, chord1.Duration);

        // Second chord: F A c (F major with high C)
        Assert.IsType<Chord>(events[1]);
        var chord2 = (Chord)events[1];
        Assert.Equal(3, chord2.Pitches.Count);
        Assert.Equal(PitchClass.F, chord2.Pitches[0].PitchClass);
        Assert.Equal(PitchClass.A, chord2.Pitches[1].PitchClass);
        Assert.Equal(PitchClass.C, chord2.Pitches[2].PitchClass);
        Assert.Equal(4, chord2.Pitches[0].Octave); // F is uppercase = octave 4
        Assert.Equal(5, chord2.Pitches[2].Octave); // c is lowercase = octave 5
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
        var events = score.Parts[0].Voices[0].Measures[0].Events;

        Assert.Equal(2, events.Count);

        // First chord with accidentals
        var chord1 = (Chord)events[0];
        Assert.Equal(3, chord1.Pitches.Count);
        Assert.Equal(Accidental.Sharp, chord1.Pitches[0].Accidental);
        Assert.Equal(Accidental.Flat, chord1.Pitches[1].Accidental);
        Assert.Equal(Accidental.Natural, chord1.Pitches[2].Accidental);
        Assert.Equal(SymbolicDuration.Quarter, chord1.Duration);

        // Second chord with duration modifier
        var chord2 = (Chord)events[1];
        Assert.Equal(SymbolicDuration.Half, chord2.Duration); // [CEG]2 = half note
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
        var events = score.Parts[0].Voices[0].Measures[0].Events;

        Assert.Equal(2, events.Count);

        // First chord: C, E G (low C)
        var chord1 = (Chord)events[0];
        Assert.Equal(3, chord1.Pitches[0].Octave); // C, = octave 3
        Assert.Equal(4, chord1.Pitches[1].Octave); // E = octave 4
        Assert.Equal(4, chord1.Pitches[2].Octave); // G = octave 4

        // Second chord: c' e' g' (high)
        var chord2 = (Chord)events[1];
        Assert.Equal(6, chord2.Pitches[0].Octave); // c' = octave 6
        Assert.Equal(6, chord2.Pitches[1].Octave); // e' = octave 6
        Assert.Equal(6, chord2.Pitches[2].Octave); // g' = octave 6
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
        var events = score.Parts[0].Voices[0].Measures[0].Events.Cast<NotationNote>().ToList();

        Assert.Equal(4, events.Count);

        // First C should have TieType.Start
        Assert.Equal(PitchClass.C, events[0].Pitch.PitchClass);
        Assert.Equal(TieType.Start, events[0].Tie);

        // Second C should have TieType.None (we only mark the start of ties for now)
        Assert.Equal(PitchClass.C, events[1].Pitch.PitchClass);
        Assert.Equal(TieType.None, events[1].Tie);

        // D and E should not be tied
        Assert.Equal(TieType.None, events[2].Tie);
        Assert.Equal(TieType.None, events[3].Tie);
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
        var events = score.Parts[0].Voices[0].Measures[0].Events.Cast<NotationNote>().ToList();

        Assert.Equal(4, events.Count);

        // All should be A
        Assert.All(events, e => Assert.Equal(PitchClass.A, e.Pitch.PitchClass));

        // First three should have TieType.Start
        Assert.Equal(TieType.Start, events[0].Tie);
        Assert.Equal(TieType.Start, events[1].Tie);
        Assert.Equal(TieType.Start, events[2].Tie);

        // Last should not be tied
        Assert.Equal(TieType.None, events[3].Tie);
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
        var events = score.Parts[0].Voices[0].Measures[0].Events;

        Assert.Equal(2, events.Count);

        var chord1 = (Chord)events[0];
        var chord2 = (Chord)events[1];

        // First chord should be tied
        Assert.Equal(TieType.Start, chord1.Tie);

        // Second chord should not be tied
        Assert.Equal(TieType.None, chord2.Tie);
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
        var events = score.Parts[0].Voices[0].Measures[0].Events.Cast<NotationNote>().ToList();

        Assert.Equal(3, events.Count);

        // First C (quarter note, tied)
        Assert.Equal(SymbolicDuration.Quarter, events[0].Duration);
        Assert.Equal(TieType.Start, events[0].Tie);

        // Second C (quarter note, not tied)
        Assert.Equal(SymbolicDuration.Quarter, events[1].Duration);
        Assert.Equal(TieType.None, events[1].Tie);

        // D (half note, not tied)
        Assert.Equal(SymbolicDuration.Half, events[2].Duration);
        Assert.Equal(TieType.None, events[2].Tie);
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
        var events = score.Parts[0].Voices[0].Measures[0].Events.Cast<NotationNote>().ToList();

        Assert.Equal(4, events.Count);

        // A>B: A is dotted (3/2 of quarter = dotted quarter), B is halved (1/2 of quarter = eighth)
        Assert.Equal(PitchClass.A, events[0].Pitch.PitchClass);
        Assert.Equal(new SymbolicDuration(NoteDurationBase.Quarter, dots: 1), events[0].Duration);

        Assert.Equal(PitchClass.B, events[1].Pitch.PitchClass);
        Assert.Equal(SymbolicDuration.Eighth, events[1].Duration);

        // C and D should be normal quarters
        Assert.Equal(SymbolicDuration.Quarter, events[2].Duration);
        Assert.Equal(SymbolicDuration.Quarter, events[3].Duration);
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
        var events = score.Parts[0].Voices[0].Measures[0].Events.Cast<NotationNote>().ToList();

        Assert.Equal(4, events.Count);

        // A<B: A is halved (1/2 of quarter = eighth), B is dotted (3/2 of quarter = dotted quarter)
        Assert.Equal(PitchClass.A, events[0].Pitch.PitchClass);
        Assert.Equal(SymbolicDuration.Eighth, events[0].Duration);

        Assert.Equal(PitchClass.B, events[1].Pitch.PitchClass);
        Assert.Equal(new SymbolicDuration(NoteDurationBase.Quarter, dots: 1), events[1].Duration);
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
        var events = score.Parts[0].Voices[0].Measures[0].Events.Cast<NotationNote>().ToList();

        Assert.Equal(2, events.Count);

        // A>>B: A is double dotted (7/4 of quarter), B is quartered (1/4 of quarter = sixteenth)
        Assert.Equal(PitchClass.A, events[0].Pitch.PitchClass);
        Assert.Equal(new SymbolicDuration(NoteDurationBase.Quarter, dots: 2), events[0].Duration);

        Assert.Equal(PitchClass.B, events[1].Pitch.PitchClass);
        Assert.Equal(SymbolicDuration.Sixteenth, events[1].Duration);
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
        var events = score.Parts[0].Voices[0].Measures[0].Events.Cast<NotationNote>().ToList();

        Assert.Equal(3, events.Count);

        // A2>B2: Base durations are quarters (L:1/8, multiplied by 2)
        // After broken rhythm: A = quarter * 3/2 = dotted quarter, B = quarter * 1/2 = eighth
        Assert.Equal(PitchClass.A, events[0].Pitch.PitchClass);
        Assert.Equal(new SymbolicDuration(NoteDurationBase.Quarter, dots: 1), events[0].Duration);

        Assert.Equal(PitchClass.B, events[1].Pitch.PitchClass);
        Assert.Equal(SymbolicDuration.Eighth, events[1].Duration);

        Assert.Equal(PitchClass.C, events[2].Pitch.PitchClass);
        Assert.Equal(SymbolicDuration.Half, events[2].Duration);
    }
}
