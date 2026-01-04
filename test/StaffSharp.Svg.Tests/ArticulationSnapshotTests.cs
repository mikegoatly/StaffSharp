namespace StaffSharp.Svg.Tests;

using System.IO;
using System.Text;

using StaffSharp.Notation;
using StaffSharp.Svg.Tests.Infrastructure;

using Xunit;

/// <summary>
/// Snapshot tests for articulations and decorations rendering.
/// </summary>
public class ArticulationSnapshotTests : VisualSnapshotTestBase
{
    [Fact]
    public async Task Export_VariousArticulations_RendersCorrectly()
    {
        var metadata = new ScoreMetadata(
            "Articulation Test",
            "Test",
            KeySignature.C,
            TimeSignature.CommonTime,
            120);

        // Create notes with different articulations
        var events = new List<INotationEvent>
        {
            // Staccato
            new NotationNote(
                new Pitch(PitchClass.C, 4),
                SymbolicDuration.Quarter,
                Velocity.MezzoForte,
                Decorations: [Decoration.Staccato]),

            // Accent
            new NotationNote(
                new Pitch(PitchClass.D, 4),
                SymbolicDuration.Quarter,
                Velocity.MezzoForte,
                Decorations: [Decoration.Accent]),

            // Tenuto
            new NotationNote(
                new Pitch(PitchClass.E, 4),
                SymbolicDuration.Quarter,
                Velocity.MezzoForte,
                Decorations: [Decoration.Tenuto]),

            // Marcato
            new NotationNote(
                new Pitch(PitchClass.F, 4),
                SymbolicDuration.Quarter,
                Velocity.MezzoForte,
                Decorations: [Decoration.Marcato]),

            // Fermata
            new NotationNote(
                new Pitch(PitchClass.G, 4),
                SymbolicDuration.Quarter,
                Velocity.MezzoForte,
                Decorations: [Decoration.Fermata]),

            // Staccato + Accent (multiple articulations)
            new NotationNote(
                new Pitch(PitchClass.A, 4),
                SymbolicDuration.Quarter,
                Velocity.MezzoForte,
                Decorations: [Decoration.Staccato, Decoration.Accent]),

            // Trill
            new NotationNote(
                new Pitch(PitchClass.B, 4),
                SymbolicDuration.Quarter,
                Velocity.MezzoForte,
                Decorations: [Decoration.Trill]),

            // UpBow
            new NotationNote(
                new Pitch(PitchClass.C, 5),
                SymbolicDuration.Quarter,
                Velocity.MezzoForte,
                Decorations: [Decoration.UpBow]),

            // DownBow
            new NotationNote(
                new Pitch(PitchClass.C, 5),
                SymbolicDuration.Quarter,
                Velocity.MezzoForte,
                Decorations: [Decoration.DownBow])
        };

        var measure = new Measure(1, events);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Test", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }

    [Fact]
    public async Task Export_ChordWithArticulations_RendersCorrectly()
    {
        var metadata = new ScoreMetadata(
            "Chord Articulation Test",
            "Test",
            KeySignature.C,
            TimeSignature.CommonTime,
            120);

        // Create chords with articulations
        var events = new List<INotationEvent>
        {
            // Chord with staccato
            new Chord(
                [new Pitch(PitchClass.C, 4), new Pitch(PitchClass.E, 4), new Pitch(PitchClass.G, 4)],
                SymbolicDuration.Quarter,
                Velocity.MezzoForte,
                decorations: [Decoration.Staccato]),

            // Chord with accent
            new Chord(
                [new Pitch(PitchClass.D, 4), new Pitch(PitchClass.F, 4), new Pitch(PitchClass.A, 4)],
                SymbolicDuration.Quarter,
                Velocity.MezzoForte,
                decorations: [Decoration.Accent]),

            // Chord with fermata
            new Chord(
                [new Pitch(PitchClass.C, 4), new Pitch(PitchClass.E, 4), new Pitch(PitchClass.G, 4)],
                SymbolicDuration.Half,
                Velocity.MezzoForte,
                decorations: [Decoration.Fermata])
        };

        var measure = new Measure(1, events);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Test", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }
}
