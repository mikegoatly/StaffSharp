using StaffSharp.Notation;
using StaffSharp.Performance;

namespace StaffSharp.Tests.Performance;

public class PerformanceTimelineTests
{
    private static TempoMap CreateSimpleTempoMap()
    {
        var tempos = new[] { new TempoChange(Rational.Zero, 120) };
        var timeSigs = new[] { new TimeSignatureChange(Rational.Zero, new TimeSignature(4, 4)) };
        return new TempoMap(tempos, timeSigs);
    }

    [Fact]
    public void PerformanceTimeline_SortsEventsByOnset()
    {
        var tempoMap = CreateSimpleTempoMap();

        // Create events out of order
        var events = new IPerformanceEvent[]
        {
            new SymbolicNoteEvent(MidiNote.Create(64), Rational.Create(4, 1), Rational.Create(1, 1), Velocity.Create(0.8f)),
            new SymbolicNoteEvent(MidiNote.Create(60), Rational.Create(0, 1), Rational.Create(1, 1), Velocity.Create(0.8f)),
            new SymbolicNoteEvent(MidiNote.Create(62), Rational.Create(2, 1), Rational.Create(1, 1), Velocity.Create(0.8f))
        };

        var timeline = new PerformanceTimeline(tempoMap, events);

        Assert.Equal(Rational.Zero, timeline.Events[0].OnsetBeats);
        Assert.Equal(Rational.Create(2, 1), timeline.Events[1].OnsetBeats);
        Assert.Equal(Rational.Create(4, 1), timeline.Events[2].OnsetBeats);
    }

    [Fact]
    public void EventsInRange_ReturnsEventsWithinRange()
    {
        var tempoMap = CreateSimpleTempoMap();

        var events = new IPerformanceEvent[]
        {
            new SymbolicNoteEvent(MidiNote.Create(60), Rational.Create(0, 1), Rational.Create(1, 1), Velocity.Create(0.8f)),
            new SymbolicNoteEvent(MidiNote.Create(62), Rational.Create(2, 1), Rational.Create(1, 1), Velocity.Create(0.8f)),
            new SymbolicNoteEvent(MidiNote.Create(64), Rational.Create(4, 1), Rational.Create(1, 1), Velocity.Create(0.8f)),
            new SymbolicNoteEvent(MidiNote.Create(65), Rational.Create(6, 1), Rational.Create(1, 1), Velocity.Create(0.8f))
        };

        var timeline = new PerformanceTimeline(tempoMap, events);

        var inRange = timeline.EventsInRange(Rational.Create(2, 1), Rational.Create(5, 1)).ToList();

        Assert.Equal(2, inRange.Count);
        Assert.Equal(Rational.Create(2, 1), inRange[0].OnsetBeats);
        Assert.Equal(Rational.Create(4, 1), inRange[1].OnsetBeats);
    }

    [Fact]
    public void EventsAt_ReturnsActiveEvents()
    {
        var tempoMap = CreateSimpleTempoMap();

        var events = new IPerformanceEvent[]
        {
            // Event from beat 0 to 2
            new SymbolicNoteEvent(MidiNote.Create(60), Rational.Create(0, 1), Rational.Create(2, 1), Velocity.Create(0.8f)),
            // Event from beat 1 to 3
            new SymbolicNoteEvent(MidiNote.Create(62), Rational.Create(1, 1), Rational.Create(2, 1), Velocity.Create(0.8f)),
            // Event from beat 4 to 5
            new SymbolicNoteEvent(MidiNote.Create(64), Rational.Create(4, 1), Rational.Create(1, 1), Velocity.Create(0.8f))
        };

        var timeline = new PerformanceTimeline(tempoMap, events);

        // At beat 1.5, first two events should be active
        var activeAt1_5 = timeline.EventsAt(Rational.Create(3, 2)).ToList();
        Assert.Equal(2, activeAt1_5.Count);

        // At beat 4.5, only third event should be active
        var activeAt4_5 = timeline.EventsAt(Rational.Create(9, 2)).ToList();
        Assert.Single(activeAt4_5);
        Assert.Equal(Rational.Create(4, 1), activeAt4_5[0].OnsetBeats);
    }

    [Fact]
    public void TotalDurationBeats_ReturnsLatestOffset()
    {
        var tempoMap = CreateSimpleTempoMap();

        var events = new IPerformanceEvent[]
        {
            new SymbolicNoteEvent(MidiNote.Create(60), Rational.Create(0, 1), Rational.Create(2, 1), Velocity.Create(0.8f)),
            new SymbolicNoteEvent(MidiNote.Create(62), Rational.Create(2, 1), Rational.Create(3, 1), Velocity.Create(0.8f))  // Ends at beat 5
        };

        var timeline = new PerformanceTimeline(tempoMap, events);

        Assert.Equal(Rational.Create(5, 1), timeline.TotalDurationBeats);
    }

    [Fact]
    public void TotalDurationSeconds_ConvertsCorrectly()
    {
        var tempoMap = CreateSimpleTempoMap(); // 120 BPM = 2 beats per second

        var events = new IPerformanceEvent[]
        {
            new SymbolicNoteEvent(MidiNote.Create(60), Rational.Create(0, 1), Rational.Create(4, 1), Velocity.Create(0.8f))  // 4 beats
        };

        var timeline = new PerformanceTimeline(tempoMap, events);

        // 4 beats at 120 BPM = 2 seconds
        Assert.Equal(2.0, timeline.TotalDurationSeconds, precision: 5);
    }

    [Fact]
    public void PerformanceTimeline_EmptyEvents_HasZeroDuration()
    {
        var tempoMap = CreateSimpleTempoMap();
        var timeline = new PerformanceTimeline(tempoMap, Array.Empty<IPerformanceEvent>());

        Assert.Equal(Rational.Zero, timeline.TotalDurationBeats);
        Assert.Equal(0.0, timeline.TotalDurationSeconds);
    }

    [Fact]
    public void PerformanceTimeline_PreservesMetadata()
    {
        var tempoMap = CreateSimpleTempoMap();
        var metadata = new PerformanceMetadata(
            Title: "Test Piece",
            Composer: "Test Composer",
            SourceFile: "test.wav");

        var timeline = new PerformanceTimeline(tempoMap, Array.Empty<IPerformanceEvent>(), metadata);

        Assert.Equal("Test Piece", timeline.Metadata.Title);
        Assert.Equal("Test Composer", timeline.Metadata.Composer);
        Assert.Equal("test.wav", timeline.Metadata.SourceFile);
    }
}
