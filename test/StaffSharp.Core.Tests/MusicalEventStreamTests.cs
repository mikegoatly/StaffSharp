namespace StaffSharp.Core.Tests;

public class MusicalEventStreamTests
{
    [Fact]
    public void MusicalEventStream_ShouldStoreVoicesAndTempo()
    {
        // Arrange
        var events = new[]
        {
            new NoteEvent(MidiNote.C4, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), Velocity.Forte)
        };
        var voice = new Voice(0, events);
        var tempo = Tempo.Create(120);

        // Act
        var stream = new MusicalEventStream(new[] { voice }, tempo);

        // Assert
        Assert.Single(stream.Voices);
        Assert.Equal(tempo, stream.Tempo);
    }

    [Fact]
    public void MusicalEventStream_TotalDuration_ShouldReturnLongestVoice()
    {
        // Arrange
        var voice1 = new Voice(0, new[]
        {
            new NoteEvent(MidiNote.C4, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), Velocity.Forte)
        });
        var voice2 = new Voice(1, new[]
        {
            new NoteEvent(MidiNote.D4, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), Velocity.Forte)
        });

        // Act
        var stream = new MusicalEventStream(new[] { voice1, voice2 });

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), stream.TotalDuration);
    }

    [Fact]
    public void MusicalEventStream_TotalEventCount_ShouldSumAllVoices()
    {
        // Arrange
        var voice1 = new Voice(0, new[]
        {
            new NoteEvent(MidiNote.C4, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), Velocity.Forte),
            new NoteEvent(MidiNote.D4, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), Velocity.Forte)
        });
        var voice2 = new Voice(1, new[]
        {
            new NoteEvent(MidiNote.E4, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), Velocity.Forte)
        });

        // Act
        var stream = new MusicalEventStream(new[] { voice1, voice2 });

        // Assert
        Assert.Equal(3, stream.TotalEventCount);
    }

    [Fact]
    public void MusicalEventStream_GetAllEvents_ShouldReturnOrderedEvents()
    {
        // Arrange
        var voice1 = new Voice(0, new[]
        {
            new NoteEvent(MidiNote.C4, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), Velocity.Forte)
        });
        var voice2 = new Voice(1, new[]
        {
            new NoteEvent(MidiNote.D4, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), Velocity.Forte),
            new NoteEvent(MidiNote.E4, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), Velocity.Forte)
        });

        // Act
        var stream = new MusicalEventStream(new[] { voice1, voice2 });
        var allEvents = stream.GetAllEvents().ToList();

        // Assert
        Assert.Equal(3, allEvents.Count);
        Assert.Equal(TimeSpan.FromSeconds(0), allEvents[0].Onset);
        Assert.Equal(TimeSpan.FromSeconds(1), allEvents[1].Onset);
        Assert.Equal(TimeSpan.FromSeconds(2), allEvents[2].Onset);
    }

    [Fact]
    public void MusicalEventStream_GetVoice_ShouldReturnCorrectVoice()
    {
        // Arrange
        var voice1 = new Voice(0, Array.Empty<NoteEvent>(), "Piano");
        var voice2 = new Voice(1, Array.Empty<NoteEvent>(), "Violin");
        var stream = new MusicalEventStream(new[] { voice1, voice2 });

        // Act
        var retrievedVoice = stream.GetVoice(1);

        // Assert
        Assert.Equal("Violin", retrievedVoice.Name);
        Assert.Equal(1, retrievedVoice.Id);
    }

    [Fact]
    public void MusicalEventStream_GetVoice_ShouldThrowForInvalidId()
    {
        // Arrange
        var voice = new Voice(0, Array.Empty<NoteEvent>());
        var stream = new MusicalEventStream(new[] { voice });

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => stream.GetVoice(99));
    }

    [Fact]
    public void MusicalEventStream_CreateMonophonic_ShouldCreateSingleVoice()
    {
        // Arrange
        var events = new[]
        {
            new NoteEvent(MidiNote.C4, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), Velocity.Forte)
        };
        var tempo = Tempo.Create(120);

        // Act
        var stream = MusicalEventStream.CreateMonophonic(events, tempo, "Melody");

        // Assert
        Assert.Single(stream.Voices);
        Assert.Equal(0, stream.Voices[0].Id);
        Assert.Equal("Melody", stream.Voices[0].Name);
        Assert.Equal(tempo, stream.Tempo);
    }

    [Fact]
    public void MusicalEventStream_CreateEmpty_ShouldCreateEmptyStream()
    {
        // Act
        var stream = MusicalEventStream.CreateEmpty();

        // Assert
        Assert.Single(stream.Voices);
        Assert.Equal(0, stream.TotalEventCount);
        Assert.Equal(TimeSpan.Zero, stream.TotalDuration);
    }

    [Fact]
    public void MusicalEventStream_ShouldThrowForEmptyVoiceList()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new MusicalEventStream(Array.Empty<Voice>()));
    }

    [Fact]
    public void MusicalEventStream_ShouldThrowForDuplicateVoiceIds()
    {
        // Arrange
        var voice1 = new Voice(0, Array.Empty<NoteEvent>());
        var voice2 = new Voice(0, Array.Empty<NoteEvent>()); // Same ID

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new MusicalEventStream(new[] { voice1, voice2 }));
    }

    [Fact]
    public void MusicalEventStream_ShouldAllowNullTempo()
    {
        // Arrange
        var voice = new Voice(0, Array.Empty<NoteEvent>());

        // Act
        var stream = new MusicalEventStream(new[] { voice }, tempo: null);

        // Assert
        Assert.Null(stream.Tempo);
    }
}
