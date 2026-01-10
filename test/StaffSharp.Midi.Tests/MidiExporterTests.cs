namespace StaffSharp.Midi.Tests;

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using StaffSharp;
using StaffSharp.Notation;
using MidiTimeSignature = Melanchall.DryWetMidi.Interaction.TimeSignature;
using StaffSharp.TestHelpers.Builders;
using StaffSharp.TestHelpers;

public class MidiExporterTests : ScoreTestBase
{
    [Fact]
    public async Task ExportAsync_SimpleCMajorScale_CreatesValidMidiFile()
    {
        // Arrange
        var score = BuildCMajorScale();

        // Act
        var midiBytes = await ExportToBytes(score);

        // Assert
        var midiFile = MidiFile.Read(new MemoryStream(midiBytes));

        // Get notes from the entire MIDI file
        var notes = midiFile.GetNotes().OrderBy(n => n.Time).ToList();

        Assert.Equal(8, notes.Count);
        Assert.Equal(60, notes[0].NoteNumber); // C4
        Assert.Equal(62, notes[1].NoteNumber); // D4
        Assert.Equal(64, notes[2].NoteNumber); // E4
        Assert.Equal(65, notes[3].NoteNumber); // F4
        Assert.Equal(67, notes[4].NoteNumber); // G4
        Assert.Equal(69, notes[5].NoteNumber); // A4
        Assert.Equal(71, notes[6].NoteNumber); // B4
        Assert.Equal(72, notes[7].NoteNumber); // C5
    }

    [Fact]
    public async Task ExportAsync_TiedNotes_MergesIntoSingleNote()
    {
        // Arrange
        var score = BuildScoreWithTiedNotes();

        // Act
        var midiBytes = await ExportToBytes(score);

        // Assert
        var midiFile = MidiFile.Read(new MemoryStream(midiBytes));
        var notes = midiFile.GetTrackChunks().Skip(1).SelectMany(track => track.GetNotes()).ToList();

        // Should have 1 note with combined duration
        Assert.Single(notes);
        Assert.Equal(60, notes[0].NoteNumber); // C4

        // Two quarter notes tied = half note = 2 beats = 960 ticks (at 480 TPQN)
        Assert.Equal(960, notes[0].Length);
    }

    [Fact]
    public async Task ExportAsync_Chord_CreatesSimultaneousNotes()
    {
        // Arrange
        var score = BuildScoreWithChord();

        // Act
        var midiBytes = await ExportToBytes(score);

        // Assert
        var midiFile = MidiFile.Read(new MemoryStream(midiBytes));
        var notes = midiFile.GetTrackChunks().Skip(1).SelectMany(track => track.GetNotes()).ToList();

        // C major chord: C, E, G
        Assert.Equal(3, notes.Count);
        Assert.Equal(60, notes[0].NoteNumber); // C4
        Assert.Equal(64, notes[1].NoteNumber); // E4
        Assert.Equal(67, notes[2].NoteNumber); // G4

        // All should start at the same time
        Assert.Equal(0L, notes[0].Time);
        Assert.Equal(0L, notes[1].Time);
        Assert.Equal(0L, notes[2].Time);
    }

    [Fact]
    public async Task ExportAsync_Rest_ProducesNoNotes()
    {
        // Arrange
        var score = BuildScoreWithRest();

        // Act
        var midiBytes = await ExportToBytes(score);

        // Assert
        var midiFile = MidiFile.Read(new MemoryStream(midiBytes));
        var notes = midiFile.GetTrackChunks().Skip(1).SelectMany(track => track.GetNotes()).OrderBy(n => n.Time).ToList();

        // Should have notes before and after rest, but rest produces no note
        Assert.Equal(2, notes.Count);
        Assert.Equal(60, notes[0].NoteNumber); // C4
        Assert.Equal(62, notes[1].NoteNumber); // D4

        // Second note should start after the rest (quarter + quarter rest = 960 ticks)
        Assert.Equal(960L, notes[1].Time);
    }

    [Fact]
    public async Task ExportAsync_CustomTempo_SetsCorrectTempoEvent()
    {
        // Arrange
        var score = BuildScoreWithTempo(90);

        // Act
        var midiBytes = await ExportToBytes(score);

        // Assert
        var midiFile = MidiFile.Read(new MemoryStream(midiBytes));
        var tempoMap = midiFile.GetTempoMap();
        var tempo = tempoMap.GetTempoAtTime(new MetricTimeSpan(0));

        // 90 BPM = 666,667 microseconds per quarter note
        Assert.Equal(90, Math.Round(tempo.BeatsPerMinute));
    }

    [Fact]
    public async Task ExportAsync_TimeSignature_SetsCorrectTimeSignatureEvent()
    {
        // Arrange
        var score = BuildScoreWithTimeSignature(new Notation.TimeSignature(3, 4));

        // Act
        var midiBytes = await ExportToBytes(score);

        // Assert
        var midiFile = MidiFile.Read(new MemoryStream(midiBytes));
        var tempoMap = midiFile.GetTempoMap();
        var timeSignature = tempoMap.GetTimeSignatureAtTime(new MetricTimeSpan(0));

        Assert.Equal(3, ((MidiTimeSignature)timeSignature).Numerator);
        Assert.Equal(4, ((MidiTimeSignature)timeSignature).Denominator); // DryWetMidi converts back from log2
    }

    [Fact]
    public async Task ExportAsync_KeySignature_SetsCorrectKeySignatureEvent()
    {
        // Arrange
        var score = BuildScoreWithKeySignature(KeySignature.G);

        // Act
        var midiBytes = await ExportToBytes(score);

        // Assert
        var midiFile = MidiFile.Read(new MemoryStream(midiBytes));
        var keySignatureEvent = midiFile.GetTrackChunks().First()
            .Events.OfType<KeySignatureEvent>().FirstOrDefault();

        Assert.NotNull(keySignatureEvent);
        Assert.Equal(1, keySignatureEvent.Key); // 1 sharp
    }

    [Fact]
    public async Task ExportAsync_CustomTicksPerQuarterNote_UsesCorrectTimeDivision()
    {
        // Arrange
        var score = BuildCMajorScale();
        var options = new MidiExportOptions { TicksPerQuarterNote = 960 };

        // Act
        var midiBytes = await ExportToBytes(score, options);

        // Assert
        var midiFile = MidiFile.Read(new MemoryStream(midiBytes));
        var timeDivision = midiFile.TimeDivision as TicksPerQuarterNoteTimeDivision;

        Assert.NotNull(timeDivision);
        Assert.Equal(960, timeDivision.TicksPerQuarterNote);
    }

    // Helper methods

    private static async Task<byte[]> ExportToBytes(NotationScore score, MidiExportOptions? options = null)
    {
        using var stream = new MemoryStream();
        await MidiExporter.ExportAsync(score, stream, options).ConfigureAwait(false);
        stream.Position = 0; // Reset position for reading
        return stream.ToArray();
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
        var part = new Part("Melody", Clef.Treble, [voice]);

        return new NotationScore(metadata, [part]);
    }

    private static NotationScore BuildScoreWithTiedNotes()
    {
        var notes = NotationEventBuilder.Create()
            .C(tieMarker: TieMarkerType.Start)
            .C(tieMarker: TieMarkerType.Stop)
            .Build();

        var metadata = new ScoreMetadata("Tied Notes", "Test", KeySignature.C, Notation.TimeSignature.CommonTime, 120);
        var measure = new Measure(1, notes);
        var voice = new Voice(1, [measure]);
        var part = new Part("Melody", Clef.Treble, [voice]);

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
        var part = new Part("Melody", Clef.Treble, [voice]);

        return new NotationScore(metadata, [part]);
    }

    private static NotationScore BuildScoreWithRest()
    {
        var events = NotationEventBuilder.Create()
            .C().Rest().D()
            .Build();

        var metadata = new ScoreMetadata("Rest Test", "Test", KeySignature.C, Notation.TimeSignature.CommonTime, 120);
        var measure = new Measure(1, events);
        var voice = new Voice(1, [measure]);
        var part = new Part("Melody", Clef.Treble, [voice]);

        return new NotationScore(metadata, [part]);
    }

    private static NotationScore BuildScoreWithTempo(int tempo)
    {
        var events = NotationEventBuilder.Create().C().Build();
        var metadata = new ScoreMetadata("Tempo Test", "Test", KeySignature.C, Notation.TimeSignature.CommonTime, tempo);
        var measure = new Measure(1, events);
        var voice = new Voice(1, [measure]);
        var part = new Part("Melody", Clef.Treble, [voice]);

        return new NotationScore(metadata, [part]);
    }

    private static NotationScore BuildScoreWithTimeSignature(Notation.TimeSignature timeSignature)
    {
        var events = NotationEventBuilder.Create().C().Build();
        var metadata = new ScoreMetadata("Time Signature Test", "Test", KeySignature.C, timeSignature, 120);
        var measure = new Measure(1, events);
        var voice = new Voice(1, [measure]);
        var part = new Part("Melody", Clef.Treble, [voice]);

        return new NotationScore(metadata, [part]);
    }

    private static NotationScore BuildScoreWithKeySignature(KeySignature keySignature)
    {
        var events = NotationEventBuilder.Create().C().Build();
        var metadata = new ScoreMetadata("Key Signature Test", "Test", keySignature, Notation.TimeSignature.CommonTime, 120);
        var measure = new Measure(1, events);
        var voice = new Voice(1, [measure]);
        var part = new Part("Melody", Clef.Treble, [voice]);

        return new NotationScore(metadata, [part]);
    }
}
