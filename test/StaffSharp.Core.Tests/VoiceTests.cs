namespace StaffSharp.Core.Tests;

public class VoiceTests
{
    [Fact]
    public void Voice_ShouldOrderEventsByOnset()
    {
        // Arrange
        var events = new[]
        {
            new NoteEvent(MidiNote.C4, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), Velocity.Forte),
            new NoteEvent(MidiNote.D4, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), Velocity.Forte),
            new NoteEvent(MidiNote.E4, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), Velocity.Forte)
        };

        // Act
        var voice = new Voice(0, events);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(0), voice.Events[0].Onset);
        Assert.Equal(TimeSpan.FromSeconds(1), voice.Events[1].Onset);
        Assert.Equal(TimeSpan.FromSeconds(2), voice.Events[2].Onset);
    }

    [Fact]
    public void Voice_Duration_ShouldReturnEndOfLastNote()
    {
        // Arrange
        var events = new[]
        {
            new NoteEvent(MidiNote.C4, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), Velocity.Forte),
            new NoteEvent(MidiNote.D4, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), Velocity.Forte) // Ends at 3s
        };

        // Act
        var voice = new Voice(0, events);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(3), voice.Duration);
    }

    [Fact]
    public void Voice_Empty_ShouldHaveZeroDuration()
    {
        // Arrange & Act
        var voice = Voice.Empty(0);

        // Assert
        Assert.Equal(TimeSpan.Zero, voice.Duration);
        Assert.Equal(0, voice.EventCount);
    }

    [Fact]
    public void Voice_ShouldSupportOptionalName()
    {
        // Arrange & Act
        var voice = new Voice(1, Array.Empty<NoteEvent>(), "Piano");

        // Assert
        Assert.Equal("Piano", voice.Name);
        Assert.Equal(1, voice.Id);
    }

    [Fact]
    public void Voice_ShouldThrowForNegativeId()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Voice(-1, Array.Empty<NoteEvent>()));
    }

    [Fact]
    public void Voice_ShouldThrowForNullEvents()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Voice(0, null!));
    }
}
