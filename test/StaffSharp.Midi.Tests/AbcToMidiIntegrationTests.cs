namespace StaffSharp.Midi.Tests;

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

using StaffSharp.Abc.Importing;

public class AbcToMidiIntegrationTests
{
    [Fact]
    public async Task CMajorScale_FromAbc_ProducesCorrectMidiNotes()
    {
        // Arrange - ABC file with C major scale (C4 to C5)
        var abcContent = """
            X:1
            T:C Major Scale
            C:Test Composer
            M:4/4
            L:1/4
            Q:1/4=120
            K:C
            CDEF|GABc|
            """;

        // Import ABC
        var importer = new AbcScoreImporter();
        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(abcContent));
        var score = await importer.ImportAsync(inputStream);

        // Export to MIDI
        var exporter = new MidiScoreExporter();
        using var outputStream = new MemoryStream();
        await exporter.ExportAsync(score, outputStream);

        // Read back MIDI and verify
        outputStream.Position = 0;
        var midiFile = MidiFile.Read(outputStream);
        var notes = midiFile.GetNotes().OrderBy(n => n.Time).ToList();

        // Verify we have 8 notes (C major scale)
        Assert.Equal(8, notes.Count);

        // Verify the note numbers (MIDI note numbers)
        Assert.Equal(60, notes[0].NoteNumber); // C4
        Assert.Equal(62, notes[1].NoteNumber); // D4
        Assert.Equal(64, notes[2].NoteNumber); // E4
        Assert.Equal(65, notes[3].NoteNumber); // F4
        Assert.Equal(67, notes[4].NoteNumber); // G4
        Assert.Equal(69, notes[5].NoteNumber); // A4
        Assert.Equal(71, notes[6].NoteNumber); // B4
        Assert.Equal(72, notes[7].NoteNumber); // C5 - Should go up to C5, not back to C4!
    }

    [Fact]
    public async Task SimpleMelody_FromAbc_HasCorrectTempo()
    {
        // Arrange
        var abcContent = """
            X:1
            T:Test
            M:4/4
            L:1/4
            Q:1/4=90
            K:C
            CCCC|
            """;

        // Import ABC
        var importer = new AbcScoreImporter();
        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(abcContent));
        var score = await importer.ImportAsync(inputStream);

        // Verify tempo was parsed correctly (not 1 BPM!)
        Assert.Equal(90, score.Metadata.Tempo);

        // Export to MIDI
        var exporter = new MidiScoreExporter();
        using var outputStream = new MemoryStream();
        await exporter.ExportAsync(score, outputStream);

        // Read back MIDI and verify tempo
        outputStream.Position = 0;
        var midiFile = MidiFile.Read(outputStream);
        var tempoMap = midiFile.GetTempoMap();
        var tempo = tempoMap.GetTempoAtTime(new MetricTimeSpan(0));

        Assert.Equal(90, Math.Round(tempo.BeatsPerMinute));
    }
}
