namespace StaffSharp.Core.Tests;

public class NoteTests
{
    [Fact]
    public void Create_ValidNote_CreatesInstance()
    {
        var note = new Note(
            Pitch: MidiNote.C4,
            Onset: TimeSpan.FromSeconds(1),
            Duration: TimeSpan.FromSeconds(0.5),
            Velocity: Velocity.Forte
        );

        Assert.Equal(60f, note.Pitch.Value);
        Assert.Equal(TimeSpan.FromSeconds(1), note.Onset);
        Assert.Equal(TimeSpan.FromSeconds(0.5), note.Duration);
        Assert.Equal(0.8f, note.Velocity.Value);
    }

    [Fact]
    public void Offset_CalculatesCorrectly()
    {
        var note = new Note(
            Pitch: MidiNote.C4,
            Onset: TimeSpan.FromSeconds(2),
            Duration: TimeSpan.FromSeconds(1.5),
            Velocity: Velocity.MezzoForte
        );

        Assert.Equal(TimeSpan.FromSeconds(3.5), note.Offset);
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        var note1 = new Note(
            MidiNote.C4,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(0.5),
            Velocity.Forte
        );

        var note2 = new Note(
            MidiNote.C4,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(0.5),
            Velocity.Forte
        );

        var note3 = new Note(
            MidiNote.D4,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(0.5),
            Velocity.Forte
        );

        Assert.Equal(note1, note2);
        Assert.NotEqual(note1, note3);
    }

    [Fact]
    public void With_ModifiesProperties()
    {
        var note = new Note(
            MidiNote.C4,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(0.5),
            Velocity.Forte
        );

        var modifiedNote = note with { Pitch = MidiNote.D4 };

        Assert.Equal(62f, modifiedNote.Pitch.Value);
        Assert.Equal(note.Onset, modifiedNote.Onset);
        Assert.Equal(note.Duration, modifiedNote.Duration);
        Assert.Equal(note.Velocity, modifiedNote.Velocity);
    }
}
