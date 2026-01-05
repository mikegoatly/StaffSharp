namespace StaffSharp.Svg.Tests;

using System.IO;
using System.Text;

using StaffSharp.Notation;
using StaffSharp.Svg.Tests.Infrastructure;

using Xunit;

/// <summary>
/// Snapshot tests for barline rendering.
/// </summary>
public class BarlineSnapshotTests : VisualSnapshotTestBase
{
    [Fact]
    public async Task Export_AllBarlineTypes_RendersCorrectly()
    {
        var metadata = new ScoreMetadata(
            "Barline Test",
            "Test",
            KeySignature.C,
            TimeSignature.CommonTime,
            120);

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
        var part = new Part("Test", [staff]);
        var score = new NotationScore(metadata, [part]);

        var exporter = new SvgScoreExporter();
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream);
        var svgContent = Encoding.UTF8.GetString(stream.ToArray());

        AssertMatchesSnapshot(svgContent, SnapshotOptions.Default);
    }
}
