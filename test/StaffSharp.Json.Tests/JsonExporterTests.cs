namespace StaffSharp.Json.Tests;

using System.Text.Json;
using StaffSharp.Json;
using StaffSharp.Notation;
using StaffSharp.TestHelpers;
using StaffSharp.TestHelpers.Builders;

public class JsonExporterTests : ScoreTestBase
{
    [Fact]
    public async Task ExportAsync_SimpleCMajorScale_ProducesValidJson()
    {
        // Arrange
        var score = BuildCMajorScale();
        var exporter = new JsonScoreExporter();

        // Act
        var json = await ExportToString(exporter, score);

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);

        // Verify it's valid JSON by parsing it
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("metadata", out _));
        Assert.True(root.TryGetProperty("parts", out _));
    }

    [Fact]
    public async Task ExportAsync_WithIndentTrue_ProducesPrettyPrintedJson()
    {
        // Arrange
        var score = BuildCMajorScale();
        var exporter = new JsonScoreExporter();
        var options = new Dictionary<string, string> { ["indent"] = "true" };

        // Act
        var json = await ExportToString(exporter, score, options);

        // Assert - pretty-printed JSON should contain newlines and indentation
        Assert.Contains("\n", json);
        Assert.Contains("  ", json); // Check for indentation
    }

    [Fact]
    public async Task ExportAsync_WithIndentFalse_ProducesCompactJson()
    {
        // Arrange
        var score = BuildCMajorScale();
        var exporter = new JsonScoreExporter();
        var options = new Dictionary<string, string> { ["indent"] = "false" };

        // Act
        var json = await ExportToString(exporter, score, options);

        // Assert - compact JSON should not contain newlines (except in the single line)
        var lines = json.Split('\n');
        Assert.Single(lines);
    }

    [Fact]
    public async Task ExportAsync_Chord_SerializesCorrectly()
    {
        // Arrange
        var score = BuildScoreWithChord();
        var exporter = new JsonScoreExporter();

        // Act
        var json = await ExportToString(exporter, score);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Navigate to chord
        var firstEvent = root
            .GetProperty("parts")[0]
            .GetProperty("staves")[0]
            .GetProperty("voices")[0]
            .GetProperty("measures")[0]
            .GetProperty("events")[0];

        // Assert
        Assert.Equal("chord", firstEvent.GetProperty("$type").GetString());
        Assert.True(firstEvent.TryGetProperty("pitches", out var pitches));
        Assert.Equal(3, pitches.GetArrayLength()); // C major chord has 3 notes
    }

    [Fact]
    public async Task ExportAsync_Rest_SerializesCorrectly()
    {
        // Arrange
        var score = BuildScoreWithRest();
        var exporter = new JsonScoreExporter();

        // Act
        var json = await ExportToString(exporter, score);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Navigate to rest
        var firstEvent = root
            .GetProperty("parts")[0]
            .GetProperty("staves")[0]
            .GetProperty("voices")[0]
            .GetProperty("measures")[0]
            .GetProperty("events")[0];

        // Assert
        Assert.Equal("rest", firstEvent.GetProperty("$type").GetString());
        Assert.True(firstEvent.TryGetProperty("duration", out _));
    }

    [Fact]
    public async Task ExportAsync_Metadata_SerializesCorrectly()
    {
        // Arrange
        var score = BuildCMajorScale();
        var exporter = new JsonScoreExporter();

        // Act
        var json = await ExportToString(exporter, score);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        var metadata = root.GetProperty("metadata");
        Assert.Equal(120, metadata.GetProperty("tempo").GetInt32());

        var timeSignature = metadata.GetProperty("timeSignature");
        Assert.Equal(4, timeSignature.GetProperty("numerator").GetInt32());
        Assert.Equal(4, timeSignature.GetProperty("denominator").GetInt32());
    }

    // Helper methods

    private static async Task<string> ExportToString(
        JsonScoreExporter exporter,
        NotationScore score,
        IReadOnlyDictionary<string, string>? options = null)
    {
        using var stream = new MemoryStream();
        await exporter.ExportAsync(score, stream, options);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static NotationScore BuildCMajorScale()
    {
        var notes = NotationEventBuilder.Create()
            .C().D().E().F().G().A().B().C(octave: 5)
            .Build();

        var metadata = new ScoreMetadata("C Major Scale", "Test", KeySignature.C, Notation.TimeSignature.CommonTime, 120);
        var measure1 = new Measure(1, notes.Take(4).ToList());
        var measure2 = new Measure(2, notes.Skip(4).ToList());
        var voice = new Voice(1, [measure1, measure2]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Melody", [staff]);

        return new NotationScore(metadata, [part]);
    }

    private static NotationScore BuildScoreWithChord()
    {
        // C major chord: C4, E4, G4
        var events = NotationEventBuilder.Create()
            .Chord(PitchClass.C, PitchClass.E, PitchClass.G)
            .Build();

        var metadata = new ScoreMetadata("Chord Test", "Test", KeySignature.C, Notation.TimeSignature.CommonTime, 120);
        var measure = new Measure(1, events);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Melody", [staff]);

        return new NotationScore(metadata, [part]);
    }

    private static NotationScore BuildScoreWithRest()
    {
        var events = NotationEventBuilder.Create()
            .Rest()
            .Build();

        var metadata = new ScoreMetadata("Rest Test", "Test", KeySignature.C, Notation.TimeSignature.CommonTime, 120);
        var measure = new Measure(1, events);
        var voice = new Voice(1, [measure]);
        var staff = new Staff(1, Clef.Treble, [voice]);
        var part = new Part("Melody", [staff]);

        return new NotationScore(metadata, [part]);
    }
}
