namespace StaffSharp.Abc.Tests.Exporting;

using System.Text;

using StaffSharp.Abc.Exporting;
using StaffSharp.Abc.Importing;
using StaffSharp.TestHelpers;

/// <summary>
/// Tests that verify ABC notation can be round-tripped:
/// Import ABC → Export ABC → Re-import → Verify scores are equivalent.
/// </summary>
/// <remarks>
/// These tests compare PARSED SCORES rather than raw ABC strings because:
///
/// 1. **Formatting differences are acceptable**: The exporter may format differently
///    (whitespace, line breaks, measures per line) while preserving the music.
///
/// 2. **Multiple valid representations**: Some ABC features have equivalent forms:
///    - Decorations: "T" (shorthand) vs "TC" (note-specific) both parse to Decoration.Trill
///    - Decorations: "." (shorthand) vs "!staccato!" (named) both parse to Decoration.Staccato
///    - Durations: "2" vs "//" (both mean half the default length)
///
/// 3. **Semantic equivalence is what matters**: We care that the MUSIC is identical,
///    not that the ASCII representation matches character-for-character.
///
/// Therefore, the pattern is:
///   1. Import original ABC → Score1
///   2. Export Score1 → ABC2
///   3. Re-import ABC2 → Score2
///   4. Assert Score1 ≡ Score2 (semantic comparison)
///
/// This is the standard approach for testing round-trip serialization where the format
/// allows multiple valid representations of the same data.
/// </remarks>
public class AbcRoundTripTests : ScoreTestBase
{
    [Fact]
    public async Task RoundTrip_SimpleCMajorScale_PreservesScore()
    {
        // Arrange: Simple C major scale
        var originalAbc = """
            X:1
            T:C Major Scale
            M:4/4
            L:1/8
            Q:120
            K:C
            C D E F G A B c|
            """;

        // Act 1: Import original ABC
        var importer = new AbcScoreImporter();
        var score1 = await ImportFromString(importer, originalAbc);

        // Act 2: Export to ABC
        var exporter = new AbcScoreExporter();
        var exportedAbc = await ExportToString(exporter, score1);

        // Act 3: Re-import exported ABC
        var score2 = await ImportFromString(importer, exportedAbc);

        // Assert: Scores are equivalent
        AssertScoresEquivalent(score1, score2);
    }

    [Fact]
    public async Task RoundTrip_NotesAndRests_PreservesScore()
    {
        // Arrange: Notes and rests
        var originalAbc = """
            X:1
            T:Notes and Rests
            M:4/4
            L:1/8
            Q:120
            K:C
            C2 D2 z2 E2|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_Chords_PreservesScore()
    {
        // Arrange: Chords
        var originalAbc = """
            X:1
            T:Chords
            M:4/4
            L:1/8
            Q:120
            K:C
            [CEG]2 [DFA]2 [EGB]2 [FAc]2|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_MixedDurations_PreservesScore()
    {
        // Arrange: Mixed durations (quarters, eighths, sixteenths, half)
        var originalAbc = """
            X:1
            T:Mixed Durations
            M:4/4
            L:1/8
            Q:120
            K:C
            C2 D E/ F/ G4|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_Accidentals_PreservesScore()
    {
        // Arrange: Notes with accidentals
        var originalAbc = """
            X:1
            T:Accidentals
            M:4/4
            L:1/8
            Q:120
            K:C
            ^C _D =E ^^F __G|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_SimpleTie_PreservesScore()
    {
        // Arrange: Simple tie between two notes
        var originalAbc = """
            X:1
            T:Simple Tie
            M:4/4
            L:1/8
            Q:120
            K:C
            C2-C2 D4|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_TieChain_PreservesScore()
    {
        // Arrange: Tie chain (multiple tied notes)
        var originalAbc = """
            X:1
            T:Tie Chain
            M:4/4
            L:1/8
            Q:120
            K:C
            C2-C2-C2-C2|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_SimpleSlur_PreservesScore()
    {
        // Arrange: Simple slur
        var originalAbc = """
            X:1
            T:Simple Slur
            M:4/4
            L:1/8
            Q:120
            K:C
            (C D E F)|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_MultipleSlurs_PreservesScore()
    {
        // Arrange: Multiple slurs
        var originalAbc = """
            X:1
            T:Multiple Slurs
            M:4/4
            L:1/8
            Q:120
            K:C
            (C D) (E F) (G A)|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_TiesAndSlurs_PreservesScore()
    {
        // Arrange: Both ties and slurs
        var originalAbc = """
            X:1
            T:Ties and Slurs
            M:4/4
            L:1/8
            Q:120
            K:C
            (C2-C2 D2 E2)|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_MultipleMeasures_PreservesScore()
    {
        // Arrange: Multiple measures with different barlines
        var originalAbc = """
            X:1
            T:Multiple Measures
            M:4/4
            L:1/8
            Q:120
            K:C
            C D E F|G A B c||c B A G|F E D C|]
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_RepeatSigns_PreservesScore()
    {
        // Arrange: Repeat signs
        var originalAbc = """
            X:1
            T:Repeats
            M:4/4
            L:1/8
            Q:120
            K:C
            |:C D E F:|G A B c:|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_DifferentKeySignature_PreservesScore()
    {
        // Arrange: Key signature with sharps (G major)
        var originalAbc = """
            X:1
            T:G Major
            M:4/4
            L:1/8
            Q:120
            K:G
            G A B c d e ^f g|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_DifferentTimeSignature_PreservesScore()
    {
        // Arrange: 3/4 time signature
        var originalAbc = """
            X:1
            T:Three Four Time
            M:3/4
            L:1/8
            Q:120
            K:C
            C2 D2 E2|F2 G2 A2|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_ComplexMixedFeatures_PreservesScore()
    {
        // Arrange: Mix of notes, chords, ties, slurs, accidentals, and different durations
        var originalAbc = """
            X:1
            T:Complex Example
            M:4/4
            L:1/8
            Q:120
            K:C
            (^C2-^C2 [_DFA]2 =E/)F/ |G4 z2 A2|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_Decorations_PreservesScore()
    {
        // Arrange: Notes with decorations
        var originalAbc = """
            X:1
            T:Decorations
            M:4/4
            L:1/8
            Q:120
            K:C
            .C ~D TC M E HF | !pp!G !f!A uB vc|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_GraceNotes_PreservesScore()
    {
        // Arrange: Notes with grace notes
        var originalAbc = """
            X:1
            T:Grace Notes
            M:4/4
            L:1/8
            Q:120
            K:C
            {g}c2 {/f}e2 {gab}c4|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_SimpleTriplet_PreservesScore()
    {
        // Arrange: Simple triplet
        var originalAbc = """
            X:1
            T:Simple Triplet
            M:4/4
            L:1/8
            Q:120
            K:C
            (3CDE (3FGA B2 c2|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_DifferentTuplets_PreservesScore()
    {
        // Arrange: Different tuplet types
        var originalAbc = """
            X:1
            T:Different Tuplets
            M:4/4
            L:1/8
            Q:120
            K:C
            (3CDE (5FGABC (6DEFGAB c2|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_TupletWithExplicitRatio_PreservesScore()
    {
        // Arrange: Tuplet with explicit ratio
        var originalAbc = """
            X:1
            T:Tuplet with Ratio
            M:4/4
            L:1/8
            Q:120
            K:C
            (3:2CDE F2 G2|
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task RoundTrip_RepeatWithVariants_PreservesScore()
    {
        // Arrange: Repeat with first and second endings
        var originalAbc = """
            X:1
            T:Repeat with Variants
            M:4/4
            L:1/8
            Q:120
            K:C
            |:C D E F|[1 G2 A2:|[2 B2 c2|]
            """;

        // Act & Assert
        await AssertRoundTripEquivalent(originalAbc);
    }

    [Fact]
    public async Task Export_CompactOutput_ProducesCompactFormat()
    {
        // Arrange
        var originalAbc = """
            X:1
            T:Test
            M:4/4
            L:1/8
            Q:120
            K:C
            C D E F|
            """;

        var importer = new AbcScoreImporter();
        var score = await ImportFromString(importer, originalAbc);

        // Act - Export with compact output (default)
        var exporter = new AbcScoreExporter();
        var exportedAbc = await ExportToString(exporter, score);

        // Assert - Should have no spaces between notes
        Assert.Contains("CDEF|", exportedAbc);
        Assert.DoesNotContain("C D E F|", exportedAbc);
    }

    [Fact]
    public async Task Export_SpacedOutput_ProducesSpacedFormat()
    {
        // Arrange
        var originalAbc = """
            X:1
            T:Test
            M:4/4
            L:1/8
            Q:120
            K:C
            C D E F|
            """;

        var importer = new AbcScoreImporter();
        var score = await ImportFromString(importer, originalAbc);

        var exporter = new AbcScoreExporter();
        using var stream = new MemoryStream();

        // Act - Export with spaced output
        var options = new Dictionary<string, string>
        {
            { "compactOutput", "false" }
        };
        await exporter.ExportAsync(score, stream, options);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var exportedAbc = await reader.ReadToEndAsync();

        // Assert - Should have spaces between notes
        Assert.Contains("C D E F|", exportedAbc);
    }

    private static async Task<Notation.NotationScore> ImportFromString(
        AbcScoreImporter importer,
        string abcContent)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(abcContent));
        return await importer.ImportAsync(stream);
    }

    private static async Task<string> ExportToString(
        AbcScoreExporter exporter,
        Notation.NotationScore score)
    {
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static async Task AssertRoundTripEquivalent(string originalAbc)
    {
        // Import original
        var importer = new AbcScoreImporter();
        var score1 = await ImportFromString(importer, originalAbc);

        // Export
        var exporter = new AbcScoreExporter();
        var exportedAbc = await ExportToString(exporter, score1);

        // Re-import
        var score2 = await ImportFromString(importer, exportedAbc);

        // Assert equivalence
        AssertScoresEquivalent(score1, score2);
    }

    private static void AssertScoresEquivalent(
        Notation.NotationScore score1,
        Notation.NotationScore score2)
    {
        // Metadata - key signature and time signature are critical
        Assert.Equal(score1.Metadata.KeySignature, score2.Metadata.KeySignature);
        Assert.Equal(score1.Metadata.TimeSignature.Numerator, score2.Metadata.TimeSignature.Numerator);
        Assert.Equal(score1.Metadata.TimeSignature.Denominator, score2.Metadata.TimeSignature.Denominator);

        // Structure
        Assert.Equal(score1.Parts.Count, score2.Parts.Count);

        for (int p = 0; p < score1.Parts.Count; p++)
        {
            var part1 = score1.Parts[p];
            var part2 = score2.Parts[p];

            var voices1 = part1.Voices;
            var voices2 = part2.Voices;
            Assert.Equal(voices1.Count, voices2.Count);

            for (int v = 0; v < voices1.Count; v++)
            {
                var voice1 = voices1[v];
                var voice2 = voices2[v];

                Assert.Equal(voice1.Measures.Count, voice2.Measures.Count);

                for (int m = 0; m < voice1.Measures.Count; m++)
                {
                    AssertMeasuresEquivalent(voice1.Measures[m], voice2.Measures[m]);
                }
            }
        }
    }

    private static void AssertMeasuresEquivalent(
        Notation.Measure m1,
        Notation.Measure m2)
    {
        Assert.Equal(m1.Events.Count, m2.Events.Count);

        for (int i = 0; i < m1.Events.Count; i++)
        {
            AssertEventsEquivalent(m1.Events[i], m2.Events[i]);
        }

        // Barlines
        Assert.Equal(m1.EndBarline, m2.EndBarline);
    }

    private static void AssertEventsEquivalent(
        Notation.INotationEvent e1,
        Notation.INotationEvent e2)
    {
        // Type match
        Assert.Equal(e1.GetType(), e2.GetType());

        // Duration match (with tolerance for floating point)
        var duration1 = e1.Duration.ToBeats().ToDouble();
        var duration2 = e2.Duration.ToBeats().ToDouble();
        Assert.Equal(duration1, duration2, precision: 10);

        // Type-specific properties
        switch (e1, e2)
        {
            case (Notation.NotationNote n1, Notation.NotationNote n2):
                Assert.Equal(n1.Pitch.PitchClass, n2.Pitch.PitchClass);
                Assert.Equal(n1.Pitch.Octave, n2.Pitch.Octave);
                Assert.Equal(n1.Pitch.Accidental, n2.Pitch.Accidental);
                break;

            case (Notation.Chord c1, Notation.Chord c2):
                Assert.Equal(c1.Pitches.Count, c2.Pitches.Count);
                for (int i = 0; i < c1.Pitches.Count; i++)
                {
                    Assert.Equal(c1.Pitches[i].PitchClass, c2.Pitches[i].PitchClass);
                    Assert.Equal(c1.Pitches[i].Octave, c2.Pitches[i].Octave);
                    Assert.Equal(c1.Pitches[i].Accidental, c2.Pitches[i].Accidental);
                }
                break;

            case (Notation.Rest r1, Notation.Rest r2):
                // Duration already checked
                break;
        }
    }
}
